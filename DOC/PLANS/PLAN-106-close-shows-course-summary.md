# PLAN-106: กด Close ในเนื้อหาแล้วเรียนครบ → เปิดสรุปผลของเราพร้อมปุ่มกลับหน้าหลัก

- **Status:** VERIFIED (QA deploy + Gate 0/manual smoke ผ่าน)
- **Assigned:** GitHub Copilot (took over from Gemini by user request)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ที่มา:** ผู้ใช้ขอ "กด Close แล้วให้กลับไปหน้าหลัก" — เสนอ 2 ทาง ผู้ใช้**เลือกแบบ B** (เปิด modal สรุปผลของเราซึ่งมีปุ่มกลับอยู่แล้ว แทนการเด้งออกทันที)
- **ลำดับงาน:** ทำ **หลัง [PLAN-105](PLAN-105-commit-runtime-race-500.md)** — ทั้งคู่แตะ `Player.cshtml` และเป็นของ Gemini เหมือนกัน **อย่าทำพร้อมกัน**

---

## บริบท (ยืนยันจากโค้ด/หน้าจอจริง)

- ปุ่ม **Close** อยู่**ในตัว SCORM content** (หน้าจอ "Congratulations, you passed!" สไตล์ปุ่มน้ำเงิน ไม่ใช่ teal ของระบบ) ⇒ **แก้ปุ่มนั้นตรง ๆ ไม่ได้** แต่ package แบบนี้ปกติเรียก SCORM `LMSFinish()`/`Terminate()` ซึ่ง adapter ของเรารับอยู่แล้ว
- ตัวรับปัจจุบัน ([Player.cshtml](../../iLearn.User/Views/MyLearning/Player.cshtml) ~2006 / ~2027): flush + `stopSessionTimer()` + `return "true"` — **ยังไม่ทำอะไรต่อ**
- **มี modal สรุปผลพร้อมใช้อยู่แล้ว:** `showLearningResult()` (~1895) → flush แล้วเรียก `renderLearningResultModal()` (~1917); modal มีปุ่ม `closeSummary()` และ **`goBackToMyLearning()`** (~1938) ที่พาไปหน้า "หลักสูตรของฉัน" ⇒ **งานนี้แค่ทำให้มันเปิดเองตอนเรียนครบ ไม่ต้องสร้าง UI ใหม่**
- `isReadOnly` ถูกตั้ง true เมื่อ `isReadOnly || isCompleted` จาก API (~1420) ⇒ **การเข้ามา "ทบทวน" คอร์สที่จบแล้วจะเป็น read-only** และ commit ถูกข้ามอยู่แล้ว (~1312)

## Scope (แก้เฉพาะ `Player.cshtml`)

### 1. Helper: เช็คว่าเรียนครบทั้งคอร์ส

```js
function isCourseFullyPassed() {
    if (!currentData || !Array.isArray(currentData.contentItems) || currentData.contentItems.length === 0) return false;
    return currentData.contentItems.every(r => r.clientStatus === "passed" || r.clientStatus === "completed");
}
```
- ใช้ `clientStatus` ตัวเดียวกับที่ `recalcTotalProgress` ใช้ตัดสิน `allPassed` ⇒ ตรรกะตรงกัน ไม่ต้องผูกกับ DOM/ปุ่ม

### 2. เปิดสรุปผลอัตโนมัติเมื่อ content จบ + เรียนครบ

```js
let courseSummaryAutoShown = false;   // กันเปิดซ้ำใน page session เดียว

function maybeShowCourseCompleteSummary() {
    if (isReadOnly) return;                 // เข้ามาทบทวนคอร์สที่จบแล้ว — ไม่ต้องเด้ง
    if (courseSummaryAutoShown) return;      // เปิดไปแล้วรอบนี้
    if (!isCourseFullyPassed()) return;      // ยังไม่ครบ → อยู่หน้าเดิมให้เลือก item ถัดไป
    courseSummaryAutoShown = true;
    renderLearningResultModal();
}
```

เรียกใน `LMSFinish` และ `Terminate` **หลัง flush สำเร็จ** (ห้ามเปลี่ยนหน้า/เปิด modal ก่อนบันทึกเสร็จ):

```js
LMSFinish: function(p) {
    scormLog("🏁 SCORM 1.2: LMSFinish");
    const flush = flushSelectedContentItemRuntime({ includeSessionTime: true, reason: "scorm12-finish" });
    stopSessionTimer();
    Promise.resolve(flush)
        .then(maybeShowCourseCompleteSummary)
        .catch(function (err) {
            console.error("❌ Finish sync failed:", err);
            showToast("ไม่สามารถซิงก์ผลล่าสุดได้ กรุณาตรวจสอบผลการเรียนอีกครั้ง", "warning");
            maybeShowCourseCompleteSummary();
        });
    return "true";                           // ⚠️ ต้อง return ทันที (SCORM API เป็น sync) ห้าม await
}
```
- `Terminate` (SCORM 2004) ทำแบบเดียวกัน (reason `scorm2004-terminate` เดิม)
- **ห้ามเรียก `showLearningResult()`** ตรง ๆ เพราะมันจะ flush ซ้ำอีกรอบ — ใช้ `renderLearningResultModal()` ผ่าน helper ข้างบน

### 3. ปุ่มใน modal ให้สื่อว่า "จบแล้ว"

ปุ่มขวาใน `.summary-footer` เป็น `goBackToMyLearning()` อยู่แล้ว (ข้อความ "กลับหน้าหลักสูตร") ⇒ **ไม่ต้องแก้ logic** ตรวจแค่ว่าข้อความ/ไอคอนสื่อชัดว่ากดแล้วออกไปหน้าหลักสูตรของฉัน ปรับถ้อยคำได้ถ้าจำเป็น (ไม่บังคับ)

## Gate 0 — ต้องยืนยันก่อนลงมือ (ถูกกว่าสร้างของที่ไม่ทำงาน)

**เรายังไม่เคยพิสูจน์ว่าปุ่ม Close ของ package นี้เรียก `LMSFinish` จริง**

1. เปิด `...Player?courseId=<คอร์สที่มี exam>&debug`
2. ทำ exam ให้จบ แล้วกด **Close**
3. ดู console: ต้องเห็น `🏁 SCORM 1.2: LMSFinish` (หรือ `🏁 SCORM 2004: Terminate`)

- **เห็น** → ทำตามแผนนี้ได้เลย
- **ไม่เห็น** → **หยุด อย่าเดา** จดใน Implementer Notes แล้วแจ้ง Claude — จะออกแบบ trigger ทางอื่นให้ (เช่น จับ transition ตอนคอร์สครบใน `recalcTotalProgress`) เพราะ hook ที่ผิดจะไม่ทำงานเลย

## Contract ที่เปลี่ยน

- API / DB / migration: **ไม่มี**
- พฤติกรรม: เมื่อ content แจ้งจบ **และ**ทุก item ผ่านครบ **และ**ไม่ใช่ read-only → modal สรุปผลเปิดเอง 1 ครั้งต่อการเข้าหน้า

## นอก Scope (ห้ามทำ)

- **ห้ามเด้งออกไปหน้าหลักอัตโนมัติ** — ผู้ใช้เลือกแบบ B (ให้เห็นสรุปก่อน แล้วกดเอง)
- ห้ามเปิด modal เมื่อยังเรียนไม่ครบ (คอร์สหลาย item — จะเตะผู้เรียนออกก่อนทำ item ถัดไป)
- ห้ามแก้ปุ่ม Close ใน content / ห้ามยุ่งกับ DOM ใน iframe
- ห้ามแตะ flush lifecycle ของ 097, session timer ของ 104 §C, การ serialize ของ 105 §1
- ห้าม `await` ใน `LMSFinish`/`Terminate` (SCORM API ต้องคืนค่าแบบ synchronous)

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (QA):
1. **Gate 0 ผ่านก่อน** (เห็น LMSFinish/Terminate ตอนกด Close)
2. คอร์ส 2 items: ทำ **item แรก** จบแล้วกด Close → **modal ต้องไม่เปิด** ยังอยู่หน้าเดิมเลือก item ถัดไปได้
3. ทำ **item สุดท้าย** จบแล้วกด Close → modal สรุปผลเปิดเอง แสดงเวลาเรียนแต่ละบท → กด "กลับหน้าหลักสูตร" → ไปหน้า `MyLearning` ถูกต้อง
4. ปิด modal (ปุ่ม "กลับ") แล้วกด Close ในเนื้อหาอีกครั้ง → **ไม่เด้งซ้ำ** (guard ทำงาน)
5. เข้าคอร์สที่ **จบแล้ว** (ทบทวน) → เล่นแล้วกด Close → **modal ไม่เด้ง** (read-only)
6. ตรวจ DB: ผลการเรียนถูกบันทึกก่อน modal เปิด (ไม่มีคะแนน/เวลาหาย)
7. console 0 error

## Deploy note

แตะเฉพาะ **iLearn.User** → deploy learner อย่างเดียว ไม่มี migration

## QA Deploy & Smoke (GitHub Copilot, 2026-07-21 14:42)

- Deploy QA learner สำเร็จใน bundle 105+106: `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721142236`, root `web.config` → `.\_user_deploy_20260721142236\iLearn.User.dll`, previous `_user_deploy_20260721133726`; script health check `https://ap-ntc2138-qawb/iLearn/` ได้ `200`, `AutoRolledBack=False`.
- Gate 0 ผ่านบน QA: เปิด `https://ap-ntc2138-qawb/iLearn/MyLearning/Player?courseId=540&debug` ด้วย learner `430339`, กดปุ่ม **Close** ใน SCORM package แล้ว console แสดง `🏁 SCORM 1.2: LMSFinish` จริง.
- Manual completion smoke ผ่านแบบ end-to-end: reset QA enrollment `18217`, เล่น PDF item 366 จนครบ 7/7 หน้า (`completed` → `passed`, progress 50), ทำ exam item 397 ได้ `100% (5 points)`, กด **Close** แล้ว iLearn เปิด modal `ผลการเรียน` เอง มีตารางเวลาเรียน 2 บทและปุ่ม `กลับหน้าหลักสูตร`.
- Finish commit หลัง Close ผ่าน: console มี `Runtime commit (scorm12-finish)` และ `CommitRuntime status: 200`; ไม่พบ console error, `Runtime commit failed`, หรือ `500`.
- Persisted verification หลัง modal: `GetPlayerInfo?courseId=540` คืน `progress=100`, `isCompleted=True`; runtime state item 397 เป็น `lessonStatus=passed`, `completionStatus=completed`, `successStatus=passed`, `rawScore=100`, `sessionTime=00:03:13`.

## Implementer Notes

### GitHub Copilot — 2026-07-21 14:13

- รับงานแทน Gemini ตามคำสั่งผู้ใช้ หลัง PLAN-105 ปิดแล้ว
- เพิ่ม `courseSummaryAutoShown`, `isCourseFullyPassed()`, และ `maybeShowCourseCompleteSummary()` ใน `Player.cshtml`
- Hook เฉพาะ `LMSFinish` (SCORM 1.2) และ `Terminate` (SCORM 2004): สร้าง `flushSelectedContentItemRuntime({ includeSessionTime: true, ... })`, `stopSessionTimer()`, แล้ว `Promise.resolve(flush).then(maybeShowCourseCompleteSummary).catch(...)` โดยยัง `return "true"` ทันทีตามกติกา SCORM sync API
- Helper เปิด modal ผ่าน `renderLearningResultModal()` เท่านั้น ไม่เรียก `showLearningResult()` เพื่อไม่ flush ซ้ำ, ไม่ redirect อัตโนมัติ, ไม่แตะ DOM ใน iframe, ไม่แตะ PLAN-105 runtime commit queue
- Gate 0/manual QA ยังไม่ได้รันใน coding environment เพราะต้องใช้ learner session + SCORM package จริงบน QA; ต้องยืนยันใน `?debug` ว่าปุ่ม Close ยิง `LMSFinish`/`Terminate` จริงก่อน deploy/acceptance
- Verified: `dotnet build iLearn.User/iLearn.User.csproj -o artifacts\verify-user` ผ่าน; ลบ `artifacts\verify-user` แล้ว

## Reviewer Sign-off (Claude Code, 2026-07-21)

- **ตรงสเปคทุกข้อ:** `isCourseFullyPassed()` ใช้ `clientStatus` ตัวเดียวกับ `recalcTotalProgress` (ตรรกะตรงกัน ไม่ผูก DOM) ✅ `maybeShowCourseCompleteSummary()` guard ครบ 3 ชั้น — `isReadOnly` (ทบทวนคอร์สที่จบแล้วไม่เด้ง) / `courseSummaryAutoShown` (ครั้งเดียวต่อ page) / `isCourseFullyPassed()` (คอร์สหลาย item ไม่เตะออกกลางคัน) ✅
- **เรียก `renderLearningResultModal()` ไม่ใช่ `showLearningResult()`** ⇒ ไม่ flush ซ้ำ ตามที่กำชับ ✅
- **hook ถูกต้อง:** `LMSFinish`/`Terminate` เก็บ promise ของ flush → `.then(maybeShowCourseCompleteSummary)` → **`return "true"` ทันที ไม่มี `await`** (SCORM API เป็น sync) ✅ modal เปิด**หลัง**บันทึกเสร็จ ⇒ ไม่มีคะแนน/เวลาหาย ✅ flush พัง → toast เตือนแล้วยังเปิดสรุปให้ดู (ตรงกับ pattern เดิมของ `showLearningResult`) ✅
- **เข้ากับ 105:** flush ของ LMSFinish วิ่งผ่านคิวของ 105 ⇒ modal เปิดหลังคิวเคลียร์จริง ลำดับถูกต้อง ✅ ไม่แตะ session timer ของ 104 §C / flush lifecycle ของ 097 ✅
- **Verify อิสระ:** build learner 0 errors; `node --check` ผ่าน; `dotnet test` 217/217 (ไม่กระทบ)
- **⚠️ ยังไม่ผ่าน Gate 0 — บล็อกการ VERIFIED:** implementer จดเองว่ารันไม่ได้ใน coding environment ⇒ **ยังไม่มีใครพิสูจน์ว่าปุ่ม Close ของ package นี้เรียก `LMSFinish`/`Terminate` จริง** ถ้าไม่เรียก ฟีเจอร์นี้จะไม่ทำงานเลย (โค้ดถูกแต่ไม่มีอะไรมา trigger) — **ต้องทำ Gate 0 บน QA ด้วย `?debug` ก่อนถือว่าใช้ได้** ถ้าไม่ยิงให้แจ้ง Claude ออกแบบ trigger ทางอื่น

**สรุป: โค้ดผ่านรีวิว ไม่มี finding — แต่ VERIFIED ไม่ได้จนกว่า Gate 0 จะผ่านบน QA**
