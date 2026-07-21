# PLAN-103: SCORM launch ต้อง same-origin เสมอ + ห้ามล้มเงียบ (กันเคส "ทำข้อสอบผ่านแล้วไม่บันทึก")

- **Status:** DONE → REVIEWED (code ผ่าน + พิสูจน์บน QA จริงแล้ว — รอ regression smoke ก่อน VERIFIED)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ความรุนแรง:** 🟠 HIGH — ไม่ใช่บั๊กที่เกิดตอนนี้บน PROD (learner เข้าผ่าน public FQDN ตรง config อยู่แล้ว) แต่**โหมดล้มเหลวคือผู้เรียนเสียงานเงียบ ๆ**
- **ที่มา:** ผู้ใช้ทดสอบ QA แล้วพบ "ทำ exam ได้ 100% แต่ไม่บันทึก" — วินิจฉัยแล้วเป็น cross-origin (ยืนยัน + พิสูจน์กลับด้วยการเปิด URL ที่ถูกต้องแล้วบันทึกได้ปกติ)
- **เกี่ยวข้อง:** [PLAN-094](PLAN-094-public-scorm-content-origin.md)/[095](PLAN-095-qa-public-scorm-content-origin.md) — **class เดียวกัน กัดครั้งที่ 2**

---

## หลักฐาน (QA, 2026-07-21)

| | |
| --- | --- |
| เปิด Player ผ่าน | `https://ap-ntc2138-qawb/iLearn/...` (internal host) |
| iframe SCORM โหลดจาก | `https://ap-ntc2138-qawb**.nikonoa.net**/iLearn/Courses/...` (จาก `FileSettings:HostUrl`) |
| ผล | **คนละ origin** → content เรียก `window.parent.API` ไม่ได้ → console: **`Lms initialization failed` ×2** |

CommitRuntime ที่หลุดออกมา (exam ที่หน้าจอโชว์ 100% passed):

```json
{"contentItemId":397,"lessonStatus":"incomplete","successStatus":"unknown","rawScore":0}
```

⇒ `cmiModel` ไม่เคยถูก content อัปเดต → commit ส่งค่า default → **บาร์ไม่เติม + คะแนนไม่บันทึก โดยไม่มี error ให้ผู้เรียนเห็น**

**พิสูจน์กลับ:** เปิดผ่าน `https://ap-ntc2138-qawb.nikonoa.net/...` (same-origin) → ทำ exam เดิม → DB: enrollment 18214 `Progress=100, IsCompleted=1, TotalScore=100`, exam `passed/RawScore=100`, learn `completed/100` ✅ (และยืนยัน delta time ของ 099 ถูกด้วย: session 2:02 → `TotalSecondsPlayed=122` ไม่โป่ง)

## บริบทโค้ด

- `ScormService.GetScormUrl` ([iLearn.Infrastructure](../../iLearn.Infrastructure/Services/ScormService.cs) บรรทัด 20-38) คืน **absolute URL** = `CombineUrlSegments(_settings.FileUrl, folder, href)` โดย `FileUrl` = `FileSettings:HostUrl` + `CourseFolder` (static config — **ไม่รู้ว่า learner เปิดผ่าน host ไหน**)
- API ใส่ค่านี้ลง `PlayerContentItemDto.LaunchUrl` (`EnrollmentsController.GetPlayerInfoByCourse`)
- `MyLearningController.GetPlayerInfo` ([iLearn.User](../../iLearn.User/Controllers/MyLearningController.cs)) proxy JSON ดิบผ่าน `CreateProxyResultAsync` (ไม่แตะ body)
- **เนื้อหา SCORM ถูกเสิร์ฟโดย learner app เอง** (`UseCourseStaticFiles` — `RequestPath=/Courses` ใต้ PathBase `/iLearn`) ⇒ path `/iLearn/Courses/...` ใช้ได้กับทุก host ที่ผูกกับแอปนี้
- Player ใช้ `contentItem.launchUrl` แค่ set `iframe.src` (relative URL ใช้ได้ปกติ; `isIframeActive()` อ่าน `iframe.src` ที่ resolve แล้ว จึงไม่กระทบ)

## Scope

### §1 (หลัก) — learner proxy rewrite `launchUrl` ให้เป็น root-relative

ใน `MyLearningController.GetPlayerInfo` หลังได้ response จาก API และ **status 2xx + parse ได้**:

- parse ด้วย `System.Text.Json.Nodes.JsonNode` (ไม่ต้อง mirror DTO เต็ม)
- วน `data.contentItems[]` → ทุก `launchUrl` ที่เป็น **absolute URI**:
  - **guard:** rewrite เฉพาะเมื่อ host ตรงกับ host ของ `FileSettings:HostUrl` (คือ content host ของเราเอง) — ถ้าเป็น host อื่น **ปล่อยไว้** (ให้ §2 เตือนแทน)
  - rewrite เป็น `uri.PathAndQuery + uri.Fragment` (เช่น `/iLearn/Courses/<folder>/index.html`) ⇒ browser resolve กับ origin ปัจจุบันเสมอ = **same-origin ทุกกรณี ไม่ว่าเข้าผ่าน host ไหน**
- ถ้า parse ไม่สำเร็จ / ไม่มี contentItems → **คืน body เดิมไม่แตะ** (ห้ามทำให้ endpoint พังเพราะ shape เปลี่ยน)
- ต้อง inject `IOptions<FileSettings>` เข้า `MyLearningController` (ยังไม่มี)
- **ห้ามแตะ API/ScormService** — endpoint อื่น (admin preview ฯลฯ) ยังได้ absolute URL เหมือนเดิม

### §2 — ไม่ล้มเงียบ (client, `Player.cshtml`)

**(ก) assert origin ก่อน launch** — ใน `startCourse` ก่อน set `iframe.src`:

```js
const resolved = new URL(contentItem.launchUrl, window.location.href);
if (resolved.origin !== window.location.origin) {
    console.error("SCORM cross-origin launch:", resolved.origin, "≠", window.location.origin);
    showToast("เนื้อหาอยู่คนละโดเมนกับระบบ — ผลการเรียนจะไม่ถูกบันทึก กรุณาแจ้งผู้ดูแลระบบ", "error", 10000);
}
```
- **เตือนแต่ยังให้เล่นต่อได้** (ไม่ block — ผู้เรียนอาจแค่อ่านเนื้อหา) แต่ต้องเห็นชัดว่าจะไม่บันทึก
- หลัง §1 แล้วเงื่อนไขนี้ไม่ควร fire เลย — เป็น safety net กัน config drift

**(ข, optional ถ้าความเสี่ยง false-positive ต่ำ) จับ "content ไม่เคย init LMS"** — ตั้ง flag เมื่อ `LMSInitialize`/`Initialize` ถูกเรียก; หลัง iframe `load` + grace **20 วินาที** ถ้ายังไม่เคย init → toast เตือนแบบเดียวกัน (จับสาเหตุอื่นนอกจาก cross-origin เช่น package เสีย). ถ้าเห็นว่าเสี่ยงเตือนผิดกับ content ที่ init ช้า **ให้ข้ามข้อนี้แล้วจดใน Notes**

### §3 — บันทึกกับดักลงเอกสาร (ให้ agent ทุกตัวรู้)

- **`CLAUDE.md`** เพิ่ม 1 bullet (หัวข้อ backend/หมายเหตุเฉพาะทาง): SCORM iframe ต้อง same-origin กับหน้า Player — `FileSettings:HostUrl` ที่ไม่ตรงกับ host ที่ผู้ใช้เปิด = content เรียก `window.parent.API` ไม่ได้ → **ล้มเงียบ ผู้เรียนทำจนจบแต่ไม่บันทึก** (อาการที่เห็น: console `Lms initialization failed`, commit ส่ง `incomplete/rawScore 0`) — precedent PLAN-094/103
- **`DOC/DEPLOY-CHECKLIST.md`** §5 (Verify) เพิ่มขั้น: เปิด Player ผ่าน **canonical public URL** แล้วยืนยันว่า iframe origin ตรงกับ page origin + เล่น 1 item ให้จบและเช็คว่าบันทึกจริง

## Contract ที่เปลี่ยน

- `GET /MyLearning/GetPlayerInfo` → field `launchUrl` เปลี่ยนจาก absolute เป็น **root-relative path** (consumer เดียวคือ Player ซึ่งใช้ set `iframe.src` — relative ใช้ได้)
- API / ScormService / DB / migration: **ไม่มี**

## นอก Scope (ห้ามทำ)

- ห้ามแตะ `ScormService.GetScormUrl` / API / `FileSettings` config values (094/095 ถูกแล้ว — งานนี้ทำให้ทนทานขึ้น ไม่ใช่ย้อนกลับ)
- ห้าม block ผู้เรียนไม่ให้เปิดเนื้อหาเมื่อ origin ไม่ตรง (เตือนพอ)
- ห้ามแตะ backend ของ PLAN-101 (Copilot ทำคู่ขนาน: iLearn.API/Application/Infrastructure)
- **ระวังชนกับ [PLAN-102](PLAN-102-learner-diagnostic-mode.md)** — ทั้งคู่แตะ `Player.cshtml` และเป็นของ Gemini เหมือนกัน ⇒ **ทำทีละแผน อย่าทำพร้อมกัน; แนะนำ 103 ก่อน** (แก้ correctness) แล้วค่อย 102 (tooling)

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (QA):
1. เปิด Player ผ่าน **internal host** `https://ap-ntc2138-qawb/iLearn/MyLearning/Player?courseId=540` → เดิมพัง ตอนนี้ต้อง **เล่นได้ + บันทึกได้** (launchUrl relative → same-origin), console **ไม่มี** `Lms initialization failed`
2. เปิดผ่าน **public FQDN** → ยังทำงานปกติเหมือนเดิม (ไม่ regress)
3. ทำ exam ให้ผ่านจาก **ทั้งสอง host** → ตรวจ DB: `RawScore=100`, `successStatus=passed`, enrollment `Progress=100`
4. ตรวจ response ของ `GetPlayerInfo`: `launchUrl` ขึ้นต้นด้วย `/` (relative) ไม่ใช่ `https://`
5. (ถ้าทำ §2ข) content ปกติ **ไม่**ขึ้น toast เตือน (ไม่ false-positive)

## Deploy note

แตะเฉพาะ **iLearn.User** (learner) → deploy learner อย่างเดียว ไม่ต้อง deploy API, ไม่มี migration

## Implementer Notes

- ลงทะเบียน `FileSettings` ใน DI ของ `iLearn.User` (`ServiceCollectionExtensions.cs`) เรียบร้อยแล้ว
- ปรับปรุง `MyLearningController.GetPlayerInfo` ให้แปลง absolute `launchUrl` จาก API ที่มี host ตรงกับ `FileSettings:HostUrl` ให้เป็น root-relative path (เช่น `/iLearn/Courses/<folder>/index.html`) ก่อนส่งตอบให้ UI
- เพิ่ม client assertion ใน `Player.cshtml` (`startCourse`) เพื่อแจ้งเตือนผู้เรียนผ่าน toast หากพบ cross-origin launch
- อัปเดตเอกสารกติกากลางใน `CLAUDE.md` และ `DOC/DEPLOY-CHECKLIST.md` §5 เรียบร้อยแล้ว
- ผ่านการตรวจสอบ build `dotnet build iLearn.User` สำเร็จ 0 errors, 0 warnings

## Reviewer Sign-off (Claude Code, 2026-07-21)

- **§1 rewrite ปลอดภัยและตรงสเปค:** `rewriteLaunchUrls:true` ส่งเฉพาะ `GetPlayerInfo` — endpoint อื่น (CommitRuntime/Ping/UpdateProgress/ResetProgress) ใช้ default `false` ✅ guard ครบชั้น: `IsSuccessStatusCode` + `mediaType.Contains("json")` + type check (`data is JsonObject && contentItems is JsonArray`) + **host ต้องตรงกับ `FileSettings:HostUrl`** จึงจะ rewrite + `try/catch` fallback คืน content เดิม ⇒ shape แปลก/parse พัง ไม่ทำ endpoint ล่ม ✅ inject `IOptions<FileSettings>` + ลงทะเบียน DI แล้ว ✅
- **§2 assertion:** เช็ค `resolved.origin !== location.origin` ใน `startCourse` → `console.error` + toast 10 วินาที **ไม่บล็อกการเล่น** ตามสเปค; ห่อ try/catch ✅ (§2ข init-timeout ไม่ได้ทำ — เป็น optional ในแผน)
- **§3 เอกสาร:** เพิ่มกับดักลง `CLAUDE.md` (หมายเหตุเฉพาะทาง) + `DEPLOY-CHECKLIST` §5 ✅
- **พิสูจน์บน QA จริงแล้ว (สำคัญที่สุด):** ผู้ใช้เปิด Player ผ่าน **internal host** `ap-ntc2138-qawb` ซึ่งเดิมพัง — ตอนนี้ SCORM ทำงานเต็ม 100%, **ไม่มี `Lms initialization failed`** ในconsole อีก ⇒ §1 แก้ได้จริงในสนาม
- **Verify อิสระ:** build learner 0 errors; `node --check` ผ่าน
- **คงค้าง:** ยืนยันว่าเปิดผ่าน **public FQDN** ยังทำงานปกติ (ไม่ regress) + ตรวจว่า `launchUrl` ใน response ขึ้นต้นด้วย `/`

**สรุป: ผ่านรีวิว ไม่มี finding — และเป็นแผนเดียวในชุดนี้ที่พิสูจน์ผลบน QA แล้ว**
