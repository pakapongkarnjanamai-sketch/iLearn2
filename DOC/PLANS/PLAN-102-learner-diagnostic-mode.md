# PLAN-102: Learner diagnostic mode — log client↔server (req/resp) + SCORM + ปุ่ม copy (gated `?debug`)

- **Status:** DONE → REVIEWED (code ผ่าน — รอ manual smoke ก่อน VERIFIED)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ที่มา:** ระหว่าง debug incident SCORM ผู้ใช้ต้องการเก็บ log การทำงาน UI ที่มีผลกับ backend แล้วส่งให้ Claude วิเคราะห์ — ปัจจุบัน `?scormDebug` (PLAN-097) เห็นแค่ค่าที่ **ส่งออก** แต่ไม่เห็น **response ที่ server ตอบ** ซึ่งคือชิ้นที่ตอบ "คะแนนถูกบันทึกไหม"

> **กติกาสำคัญ:** ต้อง gated 100% ด้วย URL flag — ปิดแล้ว overhead 0, ไม่มีปุ่ม, ไม่มี log. **ห้าม log แบบถาวร** (PLAN-097 เพิ่ง gate SCORM log ทิ้งเพราะกิน CPU บน iPad + ข้อมูลผู้เรียนไหลลง console — งานนี้ห้าม regress ข้อนั้น)

## บริบท (โค้ดปัจจุบัน)

- `Player.cshtml` มี `const SCORM_DEBUG = new URLSearchParams(location.search).has('scormDebug')` + `scormLog(...)` แล้ว (097)
- AJAX ทั้งหมดของ learner ใช้ `$.ajax` (jQuery) → ดักด้วย **jQuery global ajax events** ได้ครบในที่เดียว: `loadCourseData` (GetPlayerInfo), `commitRuntimeContentItems` (CommitRuntime non-beacon), `Ping`
- **ยกเว้น:** flush ผ่าน `navigator.sendBeacon` (pagehide/visibilitychange) **ไม่ผ่าน jQuery** → ต้อง log แยกในกิ่ง beacon
- Dashboard (`MyLearning/Index.cshtml`) ใช้ `$.ajax` เช่นกัน (GetMyCourses/GetCourseCatalog) — read-only, secondary

## Scope

### 1. Flag รวม + buffer (`Player.cshtml`)

```js
const params = new URLSearchParams(location.search);
const LEARNER_DEBUG = params.has('debug');
// ?debug เปิด scormLog ด้วย (backward compat: ?scormDebug ยังใช้ได้)
const SCORM_DEBUG = params.has('scormDebug') || LEARNER_DEBUG;

window.__ilearnDiag = [];
function diag(event, data) {
    if (!LEARNER_DEBUG) return;
    const entry = { t: new Date().toISOString(), event, data };
    window.__ilearnDiag.push(entry);
    console.log(`[DIAG] ${event}`, data ?? '');
}
```

- `diag()` no-op สนิทเมื่อปิด (return ก่อนทุกอย่าง)

### 2. ดัก AJAX req/resp ด้วย jQuery global events (เปิดเฉพาะ debug)

ใน `$(function(){...})` — ลงทะเบียน**เฉพาะเมื่อ `LEARNER_DEBUG`**:

```js
if (LEARNER_DEBUG) {
    $(document).ajaxSend(function (e, xhr, settings) {
        diag("ajax→", { method: settings.type, url: settings.url,
            body: safeTruncate(settings.data) });
    });
    $(document).ajaxComplete(function (e, xhr, settings) {
        diag("ajax←", { url: settings.url, status: xhr.status,
            resp: safeTruncate(xhr.responseText) });
    });
}
```

- `safeTruncate(v)`: ตัด string ยาว (เช่น > 2000 ตัว) + คืน '' ถ้า null — กัน log บวมจาก suspend_data/cmiSnapshot
- ครอบ GetPlayerInfo / CommitRuntime / Ping อัตโนมัติ (ทุก `$.ajax`)

### 3. Log กิ่ง sendBeacon แยก (ใน `commitRuntimeContentItems`)

ตรงที่ใช้ `navigator.sendBeacon(commitRuntimeUrl, body)` เพิ่ม:
```js
diag("beacon→CommitRuntime", { reason: options.reason, accepted, itemCount: payload.ContentItems.length });
```
(beacon ไม่มี response — log แค่ว่า accepted ไหม + payload สรุป)

### 4. Log จุด state สำคัญ (เสริม ให้เห็น chain content→client)

`diag()` เพิ่มที่: `updateContentItemData` (key,value,ผลลัพธ์ clientStatus/clientProgress/clientScore), `recalcTotalProgress` (สรุป passedCount/total/progress), `startCourse` (launchUrl), session-expired 440. **เป็น diag() ไม่ใช่ console.log ดิบ** (กติกา gated)

### 5. ปุ่ม "คัดลอก diagnostics" (เฉพาะ debug)

- render เฉพาะ `LEARNER_DEBUG` — ปุ่มลอยเล็ก มุมล่างซ้าย (z สูงกว่า content, ต่ำกว่า modal — ใช้ ladder ในระบบ), `position: fixed`
- กด → `navigator.clipboard.writeText(JSON.stringify(window.__ilearnDiag, null, 2))` → toast "คัดลอก log แล้ว (N รายการ)"
- ปิด debug = ไม่มีปุ่มนี้เลย

### 6. (secondary) Dashboard `MyLearning/Index.cshtml`

ถ้าเวลาเหลือ: ใส่ flag + `diag()` + ajax global events + ปุ่ม copy แบบเดียวกันสำหรับ GetMyCourses/GetCourseCatalog — **แยกได้ ถ้าไม่ทำจดเป็นหนี้** (Player คือ hot path ของ backend interaction ที่กำลัง debug)

## Contract ที่เปลี่ยน

- ไม่มี (frontend เพิ่ม debug tooling; ไม่แตะ API/payload/response shape)
- พฤติกรรม production (ไม่มี `?debug`): **เหมือนเดิมเป๊ะ 0 overhead**

## นอก Scope (ห้ามทำ)

- ห้าม log ถาวรไม่ผ่าน flag / ห้ามเปิด default
- ห้าม log ค่าเต็มของ suspend_data/cmiSnapshotJson (ตัดด้วย safeTruncate — กัน console บวม + ข้อมูลยาว)
- ห้ามส่ง log ออกนอก (ไม่มี POST log ไป endpoint — copy manual เท่านั้น)
- ห้ามแตะ SCORM adapter logic / lifecycle / commit logic (เพิ่ม diag() เท่านั้น ไม่เปลี่ยนพฤติกรรม)
- ห้ามแตะ backend / Program.cs / layout

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual:
1. `?debug` → console เห็น `[DIAG] ajax→/ajax←` ของ GetPlayerInfo + CommitRuntime พร้อม status+resp; ปุ่ม copy โผล่; กดแล้ว clipboard มี JSON array
2. เล่น item → เห็น `beacon→CommitRuntime` ตอนสลับแท็บ, `updateContentItemData`/`recalcTotalProgress` chain
3. **ไม่ใส่ `?debug`** → console เงียบ (เหลือ init/error เดิม), ไม่มีปุ่ม, `window.__ilearnDiag` ว่าง/ไม่ถูกเขียน
4. `?scormDebug` เดิมยังทำงาน (backward compat)
5. suspend_data ยาว → log ถูกตัด ไม่ทำ console ค้าง

**เป้าใช้งานจริง:** ผู้ใช้เปิด `?debug` เล่น item จนจบ → กดปุ่ม copy → paste log ให้ Claude → เห็นครบว่า content ยิง score/status อะไร (SetValue) → client จับเป็นอะไร (updateContentItemData) → commit ส่งอะไร (ajax→/beacon→) → server ตอบอะไร (ajax←) = ชี้จุดหลุดได้ทันที

## Implementer Notes

- พัฒนาระบบ diagnostic logging gated ด้วย `?debug` URL parameter ใน `Player.cshtml` และ `Index.cshtml` เรียบร้อยแล้ว
- เพิ่ม `safeTruncate` helper เพื่อตัด string / stringified data ที่ยาวเกิน 2,000 ตัวอักษร
- ผูก jQuery global events (`ajaxSend` และ `ajaxComplete`) สำหรับ capturing HTTP req/resp ครบถ้วน
- บันทึก `beacon→CommitRuntime` และจุดเปลี่ยน state ได้แก่ `startCourse`, `updateContentItemData`, `recalcTotalProgress`
- ปุ่มลอย "คัดลอก diagnostics" จะแสดงผลเฉพาะเมื่อมี `?debug` และสามารถคัดลอก `window.__ilearnDiag` เป็น formatted JSON ไปยัง clipboard
- ผ่านการตรวจสอบ build `dotnet build iLearn.User` สำเร็จ 0 errors

## Reviewer Sign-off (Claude Code, 2026-07-21)

- **Gate ถูกต้อง 100%:** `LEARNER_DEBUG` จาก `?debug`; `SCORM_DEBUG = scormDebug || debug` (backward compat ✅); `diag()` `return` ก่อนทำอะไรเมื่อปิด; **ajaxSend/ajaxComplete + ปุ่ม copy ลงทะเบียนเฉพาะใน `if (LEARNER_DEBUG)`** ⇒ ปิดแล้ว overhead 0 ไม่ regress การ gate ของ 097 ✅
- **ครอบ client↔server ครบ:** jQuery global events ดัก `$.ajax` ทุกตัว (GetPlayerInfo/CommitRuntime/Ping/GetMyCourses/GetCourseCatalog) พร้อม **request body + response status + response body** — เติมชิ้นที่ `?scormDebug` ขาดพอดี; กิ่ง `sendBeacon` log แยกตามสเปค ✅
- **safeTruncate 2000 ตัว** กัน suspend_data/cmiSnapshot ทำ console บวม ✅ ไม่มีการส่ง log ออกนอก (copy manual เท่านั้น) ✅
- **state chain:** `diag()` ที่ `startCourse` / `updateContentItemData` / `recalcTotalProgress` ✅
- ทำ §6 (Dashboard) ด้วย — เกินสเปคขั้นต่ำ
- **Verify อิสระ:** `node --check` ทั้ง Player (1,479 บรรทัด) และ Index (694) ผ่าน; build learner 0 errors
- **Observation (LOW, ไม่บล็อก):** (1) บล็อก diagnostic (~30 บรรทัด: `diagParams/safeTruncate/diag/copyDiagnosticsLog`) **ซ้ำกันใน 2 view** — ถ้าจะแตะรอบหน้าควรย้ายไป layout (2) ปุ่ม copy ใช้ `z-index:1000` ซึ่งสูงกว่า `.summary-overlay` (999) — จงใจก็ได้ (copy ตอนเปิด modal ได้) แต่หลุด ladder ใน CLAUDE.md เล็กน้อย

**สรุป: ผ่านรีวิว — observation ไม่บล็อก**
