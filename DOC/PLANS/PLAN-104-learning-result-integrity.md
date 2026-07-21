# PLAN-104: HOTFIX — ผลการเรียนถูกเขียนทับหาย + เวลาเรียนนับเวลานั่งเฉย + IsCompleted ค้าง

- **Status:** DONE → REVIEWED (QA deploy complete — รอ manual smoke ก่อน VERIFIED)
- **Assigned:** GitHub Copilot (§A, §B, §D — backend) + Antigravity (Gemini) (§C — client)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ความรุนแรง:** 🔴 CRITICAL — **ผู้เรียนสอบผ่านแล้วผลหาย** และเวลาเรียนทั้งระบบเชื่อถือไม่ได้
- **อ่าน CLAUDE.md หัวข้อ Backend ก่อนเริ่ม**
- **ห้าม deploy PROD** จนกว่าแผนนี้จะ VERIFIED

---

## หลักฐาน (QA DB — enrollment 18214, courseId 540, learner 360007)

Timeline ที่พิสูจน์แล้ว:

| เวลา | เหตุการณ์ | ผลใน DB |
| --- | --- | --- |
| 11:41 | ทำ exam ผ่านจริง | exam(397) `passed`, RawScore **100**; enrollment Progress **100**, IsCompleted=1 (Claude verify แล้ว) |
| 11:41–12:50 | **เปิดแท็บ Player ทิ้งไว้ ~2.5 ชม.** | — |
| 12:50:28 | commit ตอนปิด/สลับแท็บ ส่ง `SessionTime=02:33:55`, `lessonStatus=incomplete`, `RawScore=0` | exam ถูกทับเป็น `incomplete` score **0**, `TotalSecondsPlayed=**9235**`; enrollment Progress ตกเป็น **50** แต่ **IsCompleted ยังค้าง 1** |

สภาพปัจจุบัน (ขัดแย้งกันเอง):

```
Enrollment 18214: Progress=50.0  IsCompleted=1  TotalTimeSpent=9261s (2.5 ชม.)
  log 366 (Learn): passed      score 100  TotSec=26
  log 397 (Exam) : incomplete  score   0  TotSec=9235   ← ผลสอบที่ผ่านแล้วหายไป
  state 397      : Lesson=incomplete Comp=incomplete Succ=passed RawScore=0
```

UI: การ์ดขึ้น **"เรียนจบ" คู่กับ 50%**

## Root cause (ไล่แล้วทีละชั้น)

### A — placeholder commit เขียนทับผลที่สำเร็จแล้ว

1. เปิด item ซ้ำ/reload → content re-init แล้วรายงาน `lesson_status=incomplete` ก่อนผู้เรียนทำอะไร
2. `ScormRuntimeStateService.ApplyCommit` → `PreferStatus(..., isPlaceholderProgressCommit)` — guard ทำงาน**เฉพาะเมื่อ `isPlaceholderProgressCommit=true`** ซึ่งบังคับว่าต้อง**ไม่มี** sessionTime/suspendData/lessonLocation. commit นี้**มี** sessionTime ⇒ ไม่เข้าเงื่อนไข ⇒ `completed` → `incomplete`
3. `PreferRawScore` ตกเงื่อนไขเดียวกัน ⇒ **100 → 0**
4. `MapRuntimeCommitToProgress`: SCORM 1.2 ใช้ `NormalizeScorm12SuccessStatus(lessonStatus)` (ทิ้ง successStatus ที่ส่งมา) ⇒ `unknown` ⇒ `ResolveStatus` = `incomplete`
5. `UpsertLearningLogsAsync` เขียน log เป็น `incomplete/0/score 0`

**หมายเหตุ:** `SuccessStatus=passed` ใน state รอด (PreferSuccessStatus ปกป้อง) — เหลือ state ที่ขัดกันเอง (succ=passed แต่ lesson=incomplete)

### B — `IsCompleted` ไม่เคยถูกปลด

`LearningLogsController.UpdateEnrollmentRollupAsync` สาขา else ตั้งแค่ `Progress` — **ไม่ set `IsCompleted=false`/`CompletedDate=null`** ⇒ ค้างเป็น "เรียนจบ" ทั้งที่ 50%

### C — เวลาเรียนนับเวลาที่แท็บเปิดทิ้งไว้

`Player.cshtml` `captureSessionTime` คิดจาก `now − sessionStartTime` (นาฬิกาแขวน) — ไม่หยุดตอนแท็บ hidden. PLAN-097 เพิ่ม flush ตอน pagehide/visibilitychange ⇒ ค่ามหาศาลนั้นถูก commit จริง
⇒ exam ทำจริง ~2 นาที บันทึก **9,235 วินาที**. PLAN-099 §2 แก้แค่ "บวกซ้ำ" ไม่ได้แก้ "นับเวลานั่งเฉย"

## Scope

### §A (CRITICAL, backend) — ห้าม downgrade ผลที่สำเร็จแล้ว

**A1. `ScormRuntimeStateService`** — เปลี่ยนกติกา: **ค่า placeholder ห้ามทับค่า terminal เสมอ** ไม่ใช่เฉพาะตอน `isPlaceholderProgressCommit`
- `PreferStatus`: ถ้า existing เป็น terminal (`passed/completed/failed/browsed`) และ incoming เป็น placeholder (`incomplete/not attempted/unknown`) → **คง existing** (ตัดเงื่อนไข `isPlaceholderProgressCommit` ออกจากการตัดสินใจนี้)
- terminal → terminal **ยังทับได้** (เช่น passed→failed จากการสอบใหม่จริง) — ห้ามบล็อก
- `PreferRawScore`: ถ้า incoming = 0 และ **incoming status เป็น placeholder** → คงคะแนนเดิม; ถ้า incoming เป็น terminal ให้รับค่าใหม่ (แม้เป็น 0 — สอบใหม่ได้ 0 จริง)

**A2. `LearningLogsController.UpsertLearningLogsAsync`** (กันชั้นสอง) — ห้ามเขียน `log.Status` จาก terminal (`passed`/`completed`) กลับเป็น placeholder ภายใน attempt เดิม; ถ้า status ใหม่เป็น placeholder แต่ log เดิม terminal → **คง status/progress/score เดิม** (ยังอัปเดตเวลาได้ตามปกติ)
- การ reset จริง (PLAN-099/101) ล้าง state + กรอง log ด้วย `ResetAt` อยู่แล้ว ⇒ หลัง reset จะเริ่มใหม่สะอาด ไม่ถูกกติกานี้บล็อก

### §B (backend) — rollup ต้องปลด IsCompleted เมื่อยังไม่ครบ

ใน `UpdateEnrollmentRollupAsync` สาขา else:
```csharp
enrollment.IsCompleted = false;
enrollment.CompletedDate = null;
enrollment.Progress = ...;   // เดิม
```
- link snapshot (`SnapshotCompleted/SnapshotCompletedDate`) จะตามค่าใหม่อยู่แล้ว (โค้ดเดิม assign จาก enrollment)
- **⚠️ ต้องทำคู่กับ §A เท่านั้น** — ถ้าปล่อย §A ไว้ §B จะขยายความเสียหาย (ปลด "เรียนจบ" ของคนที่ผ่านจริง). ห้าม deploy §B เดี่ยว ๆ

### §C (client, `Player.cshtml`) — นับเฉพาะเวลาที่ active

แทน `sessionStartTime` เดี่ยว ๆ ด้วยตัวสะสมเวลา active:
- `sessionActiveMs` (สะสม) + `sessionResumedAt` (เวลาที่เริ่มนับรอบปัจจุบัน)
- `startCourse`: `sessionActiveMs = 0; sessionResumedAt = Date.now()`
- `visibilitychange`: hidden → `sessionActiveMs += Date.now() - sessionResumedAt; sessionResumedAt = null` · visible → `sessionResumedAt = Date.now()`
- `captureSessionTime`: ใช้ `sessionActiveMs + (sessionResumedAt ? Date.now() - sessionResumedAt : 0)` แทนการลบ `sessionStartTime`
- **คงพฤติกรรม max() เดิม** (เวลา monotonic ไม่ถอยหลัง) และคง flush ของ 097 ทุกจุด (ต้องยัง save ตอน hidden — แค่ค่าที่ส่งเป็นเวลา active)
- ตอน hidden ลำดับสำคัญ: **หยุดนับก่อน แล้วค่อย flush** เพื่อให้ค่าที่ส่งไม่รวมเวลาที่กำลังจะ idle

### §D (backend, กันซ้ำ) — เพดานกัน session time เพี้ยน

ใน `UpsertLearningLogsAsync` ก่อนบวก delta: ถ้า delta > **4 ชั่วโมง** (14400 วิ) ต่อ 1 commit → ไม่บวก + `ILogger.LogWarning` (ค่าผิดปกติชัดเจน) — กันข้อมูลขยะแม้ client เพี้ยน

## Contract ที่เปลี่ยน

- API shape / DB schema / migration: **ไม่มี**
- พฤติกรรมเปลี่ยน: (1) placeholder ไม่ทับ terminal (2) `IsCompleted` ปลดได้เมื่อไม่ครบ (3) session time = เวลา active

## นอก Scope (ห้ามทำ)

- ห้ามแตะ reset paths ของ 099/101 (ถูกแล้ว)
- ห้ามแตะ `NormalizeScorm12SuccessStatus` / `ResolveStatus` (ถูกตามสเปค SCORM 1.2 — แก้ที่ชั้น merge/log แทน)
- ห้ามแตะ launchUrl/103, diagnostic/102
- **ห้ามแก้ข้อมูลที่เพี้ยนอยู่ใน DB โดยไม่ได้รับอนุญาต** (ดู Remediation)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

> **หมายเหตุสำคัญ:** PLAN-101 รายงานว่า test project build ไม่ผ่านเพราะ `EnrollmentsPlayerInfoTests.NullNotificationService` ไม่ implement signature ใหม่ของ `INotificationService` — **ต้องแก้ test double ตัวนั้นให้ suite รันได้ก่อน** แล้วจึงถือว่างานนี้ verify ได้ (ห้ามส่งงานโดยที่ยังรัน test ไม่ได้)

Tests ที่ต้องเพิ่ม:
1. **§A1:** state เดิม `completed/RawScore=100` + commit `incomplete/0` (มี sessionTime) → state **ยังเป็น completed, RawScore 100**
2. **§A1:** state เดิม `passed` + commit `failed` (terminal→terminal) → **ทับได้เป็น failed**
3. **§A2:** log เดิม `passed/100` + update `incomplete/0` → log ยัง `passed/100` (แต่เวลาอัปเดตได้)
4. **§B:** 2 items ผ่าน 1 → `IsCompleted=false`, `CompletedDate=null`, Progress=50; ครบ 2 → `IsCompleted=true`, Progress=100
5. **§D:** delta > 4 ชม. → ไม่ถูกบวกเข้า `TotalSecondsPlayed`
6. **§C:** (ถ้าทำ unit test ฝั่ง client ไม่ได้ ให้ manual ตาม §Manual ข้อ 3)

Manual (QA — deploy API + learner):
1. ทำ exam ให้ผ่าน → **reload หน้า Player** → รอ content รายงาน incomplete → ตรวจว่า **ผลยัง passed / คะแนนไม่หาย / progress ยัง 100%**
2. คอร์ส 2 items ผ่าน 1 → การ์ดต้องขึ้น **ไม่ใช่ "เรียนจบ"** และ progress 50%
3. **เปิด Player ทิ้งไว้ 10 นาทีโดยสลับไปแท็บอื่น** → กลับมาปิด → `TotalSecondsPlayed` ต้องใกล้เวลาที่ดูจริง (ไม่ใช่ 10 นาที)
4. ตรวจ DB ว่าไม่มี enrollment ที่ `IsCompleted=1` แต่ `Progress<100`

## Remediation ข้อมูลที่เพี้ยนอยู่ (ต้องขออนุญาตผู้ใช้ก่อนแตะ DB)

- **enrollment 18214** — exam ผ่านจริงเมื่อ 11:41 (มีหลักฐาน) แต่ตอนนี้เป็น incomplete/0 และเวลา 9,235 วิ
  - ทางเลือก 1: หลัง deploy fix → **admin reset แล้วให้ทำ exam ใหม่** (สะอาดที่สุด ไม่แตะ DB มือ)
  - ทางเลือก 2: แก้ค่าคืนใน DB ตามหลักฐาน (ต้องได้รับอนุญาตชัดเจน)
- **ตรวจวงกว้าง:** enrollment อื่นที่ `TotalSecondsPlayed` สูงผิดปกติ (ตั้งแต่ deploy 097 เป็นต้นมา) และที่ `IsCompleted=1 AND Progress<100` — ให้ query รายงานให้ผู้ใช้ตัดสินใจ **อย่าเพิ่งแก้เอง**

## เรื่องที่ยังค้าง (ไม่อยู่ใน scope นี้)

- **CommitRuntime 500 (`DbUpdateException`)** ยังไม่ทราบ inner exception — ตัดสาเหตุไปแล้ว: index/filter, duplicate, truncation, check/trigger, `.Max()` ว่าง, concurrent commit. เปิด stdout log รอจับแล้วแต่ไม่เกิดซ้ำ จึงปิดคืน. **ถ้าเจออีกให้เปิด stdout log ใหม่แล้วเก็บ stack ทันที**

## Deploy note

§A/§B/§D = **API**; §C = **learner**. ไม่มี migration. **§B ห้าม deploy โดยไม่มี §A**

## Implementer Notes

- GitHub Copilot ทำครบ §A/§B/§C/§D ในรอบเดียวกัน (รวม client timing แม้แผนเดิม assign §C ให้ Gemini) เพื่อให้ hotfix deploy เป็นชุดเดียว: API + learner, ไม่มี migration/API shape change.
- §A: `ScormRuntimeStateService` เปลี่ยน merge rule ให้ terminal status (`passed/completed/failed/browsed`) ไม่ถูก placeholder (`incomplete/not attempted/unknown`) ทับอีกต่อไป แม้ commit จะมี `SessionTime`/runtime fields; raw/scaled score 0 จาก placeholder ไม่ทับคะแนนเดิม แต่ terminal→terminal ยังทับได้ (เช่น passed→failed พร้อม score 0).
- §A2/§D: `LearningLogsController.UpsertLearningLogsAsync` กันชั้น log ไม่ให้ terminal log ถูก placeholder downgrade ภายใน attempt เดิม และเพิ่ม cap delta ต่อ commit > 14,400s ให้ไม่บวกเข้า `TotalSecondsPlayed` พร้อม `LogWarning`.
- §B: rollup สาขาไม่ครบ set `IsCompleted=false` และ `CompletedDate=null` แล้วจึงคำนวณ progress ทำให้ UI ไม่ค้าง "เรียนจบ" คู่กับ progress ต่ำกว่า 100%.
- §C: `Player.cshtml` เปลี่ยน session timer จาก wall-clock เป็น active-time accumulator (`sessionActiveMs` + `sessionResumedAt`); hidden/pagehide/beforeunload pause ก่อน flush, visible resume เฉพาะ session ที่เริ่มแล้ว, คง monotonic max behavior เดิม.
- แก้ test harness blocker จาก PLAN-101/104: `NullNotificationService` ใน tests เพิ่ม overload `GetForUserAsync(..., skip)` เพื่อให้ suite compile กับ notification contract ที่ test artifact เห็น.
- Regression tests เพิ่ม/ปรับ: state placeholder ไม่ทับ terminal+score, terminal→terminal ทับได้, log passed/100 ไม่ถูก incomplete/0 downgrade แต่เวลาอัปเดต, rollup ปลด/คืน completion ตาม progress, delta >4h ไม่บวกเวลา.
- Verified: `dotnet build iLearn.User/iLearn.User.csproj` ผ่าน; focused `dotnet test iLearn.Tests/iLearn.Tests.csproj --filter 'FullyQualifiedName~LearningLogsRuntimeValidationTests|FullyQualifiedName~ScormRuntimeStateServiceTests'` ผ่าน 28/28; full PLAN verification `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน 214/214 และลบ `artifacts\verify-test` แล้ว. Warnings ที่เหลือเป็น warnings เดิมของ solution.
- QA deployment: API deployed first to `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\_deploy_20260721133451` (previous `20260721121253`), active `web.config` argument `.\_deploy_20260721133451\iLearn.API.dll`, health endpoint returned expected HTTP 401 and `AutoRolledBack=False`. Learner/User deployed second to `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721133726` (previous `20260721121441`), active `web.config` argument `.\_user_deploy_20260721133726\iLearn.User.dll`, root health returned HTTP 200 and `AutoRolledBack=False`. Independent read-back confirmed both active stamps; no migration was run.

## Reviewer Sign-off (Claude Code, 2026-07-21)

ตรวจ diff เต็มทั้ง backend (§A/§B/§D) และ client (§C) + verify อิสระ:

- **§A1 `ScormRuntimeStateService`:** `PreferStatus` **ตัดเงื่อนไข `isPlaceholderProgressCommit` ออกแล้ว** → terminal ถูกปกป้องเสมอ (นี่คือหัวใจของบั๊ก: commit ที่มี sessionTime เคยหลุด guard) ✅ terminal→terminal ยังทับได้ (ไม่บล็อก passed→failed) ✅ `PreferRawScore` ใช้ `isPlaceholderOutcome` จากสถานะของ commit ที่เข้ามา ⇒ 0 ทับ 100 ไม่ได้เมื่อ incoming เป็น placeholder แต่ทับได้เมื่อ terminal ✅ ลบ `IsPlaceholderProgressCommit` ที่ตายแล้ว ✅
- **§A2 `UpsertLearningLogsAsync`:** `shouldPreserveTerminalOutcome` คง status/score เดิมเมื่อ incoming เป็น placeholder; ตั้ง `isInputPassed` ใหม่จาก status ที่คงไว้ ⇒ **`log.Progress` กลับเป็น 100 ถูกต้อง** (จุดที่พลาดง่ายและทำถูก) ✅
- **§B rollup:** สาขา else ปลด `IsCompleted=false` + `CompletedDate=null` ✅ link snapshot ตามค่าใหม่อัตโนมัติ (โค้ดเดิม) ✅ **deploy คู่กับ §A ตามที่กำชับ**
- **§D เพดาน:** 14,400 วิ/commit + `LogWarning` มีบริบทครบ (enrollment/contentItem/ค่าเก่า/ค่าใหม่) — **ครอบทั้ง path update และ insert** (insert เกินสเปค แต่ถูกต้อง เพราะ log แรกก็โป่งได้) ✅
- **§C `Player.cshtml`:** เปลี่ยนเป็น `sessionActiveMs` + `sessionResumedAt` ครบชุด (start/pause/resume/stop/reset/isActive/getActiveMs) ✅ **`pauseSessionTimer()` ถูกเรียกก่อน flush ทุกจุด** (beforeunload/pagehide/visibility-hidden) ตามที่กำชับ ⇒ ค่าที่ส่งไม่รวมเวลาที่กำลังจะ idle ✅ `pause` idempotent (เรียกซ้ำไม่บวกเกิน) ✅ gate เดิม `sessionStartTime !== null` → `isSessionTimerActive()` semantics ตรงกัน ✅ คง max() monotonic เดิม ✅
- **Tests:** 5 เคสตามแผนครบ รวม **เคส regression ตรง ๆ** `UpsertAsync_PreservesTerminalStateWhenIncomingPlaceholderCarriesSessionTime` (จำลอง commit ที่มี sessionTime มาทับ passed) และ `..._ClearsCompletedFlagWhenRollupFallsBelow...AndRestoresWhenCompleteAgain` (ทดสอบทั้งขาลงและขากลับ) ✅
- **Verify อิสระ:** `dotnet test` **214/214**; build API + learner 0 errors; `node --check` ผ่าน

**สรุป: ผ่านรีวิว ไม่มี finding — พร้อม commit + deploy (API + learner)**
