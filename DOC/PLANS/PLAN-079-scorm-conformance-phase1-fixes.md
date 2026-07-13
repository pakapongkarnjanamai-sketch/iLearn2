# PLAN-079: SCORM Conformance Phase 1 — แก้บั๊กแท้ 5 ตัว (F1–F5 จาก PLAN-078)

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [PLAN-078](PLAN-078-scorm-rte-conformance-hardening-assessment.md) (assessment ต้นทาง — อ่าน §2 กลุ่ม A ก่อนเริ่ม), `DOC/SCORM-RUNTIME-LIFECYCLE-RULES.md`

> Decision จากผู้ใช้ (2026-07-13): เริ่ม Phase 1 เลย / เนื้อหาองค์กรสร้างจาก **iSpring** / assign **Copilot**
> บริบท iSpring: publish เป็น **single-SCO เสมอ** (Phase 3 ไม่เร่ง) และ iSpring ใช้ resume ผ่าน suspend_data หนัก + ฝั่ง SCORM 2004 รายงาน `session_time` เป็น **ISO8601** → F1/F2 กระทบ content จริงขององค์กรโดยตรง

---

## Scope: 5 fixes — ทั้งหมด backward-compatible (content เดิมต้องเล่นได้เหมือนเดิมทุกอย่าง)

### F1 — เพิ่ม SCORM 1.2 identity keys ที่หายไป

**ไฟล์:** `iLearn.User/Views/MyLearning/Player.cshtml`

1. หัวไฟล์ (~บรรทัด 6) ดึงชื่อจริงเพิ่ม: `var learnerName = User.FindFirst(ClaimTypes.Name)?.Value;` แล้วส่งเข้า JS เป็น `currentLearnerName` (fallback = learnerCode)
2. ใน `resetScormModel` (~บรรทัด 904) เพิ่ม key ตามสเปค 1.2:
   ```js
   "cmi.core.student_id": currentLearnerCode,
   "cmi.core.student_name": currentLearnerName,
   ```
   และปรับ `cmi.learner_name` (2004) ให้ใช้ `currentLearnerName` ด้วย (ตอนนี้ใส่รหัสพนักงานแทนชื่อ)
3. **คง** `cmi.core.learner_id/learner_name` เดิมไว้ (key นอกสเปคแต่อาจมี content เผลออ่าน — ห้ามลบใน phase นี้)

**Acceptance:** SCO 1.2 เรียก `LMSGetValue("cmi.core.student_id")` ได้รหัสพนักงาน, `student_name` ได้ชื่อ; SCO 2004 เรียก `GetValue("cmi.learner_name")` ได้ชื่อจริง

### F2 — Duration parser รองรับ ISO8601 (หัวใจของแผนนี้)

**ปัญหา:** SCORM 2004 ส่ง `session_time` แบบ `PT1H5M30S` → client/server parse ไม่ได้ → บันทึก 0 วินาที

1. **Server:** สร้าง `iLearn.Application/Common/ScormDurationParser.cs`:
   ```csharp
   public static class ScormDurationParser
   {
       /// <summary>คืนวินาที (ปัดเศษ) — รองรับ SCORM 1.2 timespan "HHHH:MM:SS(.cc)" และ SCORM 2004 ISO8601 "P[nD]T[nH][nM][n(.n)S]"; ค่า parse ไม่ได้ = 0</summary>
       public static int ToSeconds(string? value) { ... }
       /// <summary>วินาที → string ตาม format ของเวอร์ชัน ("1.2" → "HHHH:MM:SS", อื่น → "PTnHnMnS")</summary>
       public static string FromSeconds(int seconds, string scormVersion) { ... }
   }
   ```
   - 1.2 timespan: ชั่วโมงเกิน 2 หลักได้ (`0000:00:00.00` สูงสุด 4 หลัก) — split ':' แบบเดิมแต่อย่า reject 4 หลัก; รองรับเศษวินาที `.cc`
   - ISO8601: รองรับอย่างน้อย `PnDTnHnMn.nS` (D/H/M/S ตัวใดตัวหนึ่งหายได้, มี fractional seconds) — ใช้ `System.Xml.XmlConvert.ToTimeSpan` ได้ (รองรับ ISO8601 duration ครบ) ครอบ try/catch
   - แทนที่ `ParseSessionTime` ใน [LearningLogsController.cs:267-275](../../iLearn.API/Controllers/LearningLogsController.cs) ด้วย `ScormDurationParser.ToSeconds`
2. **Client:** ใน `Player.cshtml` แก้ `parseClockTimeToSeconds` (~บรรทัด 827) ให้รองรับทั้งสอง format (regex ISO8601: `/^P(?:(\d+)D)?T?(?:(\d+(?:\.\d+)?)H)?(?:(\d+(?:\.\d+)?)M)?(?:(\d+(?:\.\d+)?)S)?$/i` + fallback split ':' เดิม) — ฟังก์ชันนี้ถูกใช้เทียบ max ใน `captureSessionTime` ถ้า parse ไม่ได้ SCO 2004 จะแพ้ค่า LMS เสมอ
3. **Unit tests (บังคับ — นี่คือแก่นของงาน):** ใน `iLearn.Tests` เพิ่ม `ScormDurationParserTests`: `"00:05:30"`→330, `"0001:00:00"`→3600, `"00:00:30.55"`→31 หรือ 30 (ระบุ rounding ที่เลือกใน test), `"PT5M30S"`→330, `"PT1H"`→3600, `"PT30.5S"`→31/30, `"P1DT2H"`→93600, `""`/null/`"garbage"`/`"PT"`→0; `FromSeconds(3661,"1.2")`→`"0001:01:01"`, `FromSeconds(3661,"2004")`→`"PT1H1M1S"`

**Acceptance:** SCO 2004 (iSpring) เรียนจริงแล้ว `LearningLog.TotalSecondsPlayed` > 0

### F3 — Initialize `_count`/`_children` ให้ครบ

**ไฟล์:** `Player.cshtml` — ใน `resetScormModel` เพิ่ม:

```js
// counts
"cmi.objectives._count": "0",
// SCORM 1.2 _children
"cmi.core._children": "student_id,student_name,lesson_location,credit,lesson_status,entry,score,total_time,lesson_mode,exit,session_time",
"cmi.core.score._children": "raw,min,max",
"cmi.objectives._children": "id,score,status",
"cmi.student_data._children": "mastery_score,max_time_allowed,time_limit_action",
"cmi.interactions._children": "id,objectives,time,type,correct_responses,weighting,student_response,result,latency",
// SCORM 2004
"cmi._version": "1.0",
"cmi.score._children": "scaled,raw,min,max",
"cmi.comments_from_learner._children": "comment,location,timestamp",
"cmi.comments_from_learner._count": "0",
"cmi.comments_from_lms._count": "0"
```

และแก้ `setCmiModelValue` (~บรรทัด 950) ให้ auto-increment `_count` ของ **objectives ด้วย** (ตอนนี้ทำเฉพาะ interactions): เปลี่ยน regex เป็น `/^cmi\.(interactions|objectives)\.(\d+)\./`

**Acceptance:** `GetValue("cmi.objectives._count")` = "0" ตอนเริ่ม; SCO เขียน `cmi.objectives.0.id` แล้ว `_count` = "1"

### F4 — Persist `cmi.score.scaled` (SCORM 2004)

1. `iLearn.Domain/Entities/ScormRuntimeState.cs`: เพิ่ม `public decimal? ScaledScore { get; set; }` + migration `AddScaledScoreToScormRuntimeState`
2. `ScormRuntimeDtos.cs`: เพิ่ม `ScaledScore` ใน `ScormRuntimeContentItemCommitDto` + `ScormRuntimeStateDto`
3. `ScormRuntimeStateService.ApplyCommit`: เก็บ `ScaledScore` ด้วย guard แบบเดียวกับ `PreferRawScore` (ห้าม placeholder commit ทับค่า non-zero ด้วย 0)
4. `Player.cshtml` — `buildContentItemRuntimeState` (~บรรทัด 1019+): อ่าน `cmi.score.scaled` จาก cmiModel ใส่ payload; และใน `resetScormModel` คืนค่า `cmi.score.scaled` จาก runtimeState ถ้ามี
5. **Fallback คะแนนรายงาน:** ใน `MapRuntimeCommitToProgress` ([LearningLogsController.cs:440+]) ถ้า `RawScore` ไม่มีแต่ `ScaledScore` มี → ใช้ `Math.Round(scaled * 100)` เป็น Score ของ LearningLog (scaled ตามสเปคคือ -1..1)
6. **Contract sync:** ตรวจแล้ว React ไม่มี type mirror `ScormRuntimeStateDto` (grep `ScormRuntimeState|rawScore` ใน `iLearn.Admin.React/src` = 0) → ไม่มีงานฝั่ง React แต่ implementer ต้อง grep ซ้ำยืนยันก่อนปิดงาน

**Acceptance:** SCO ที่ set เฉพาะ `cmi.score.scaled=0.8` → `ScormRuntimeState.ScaledScore=0.8` + `LearningLog.Score=80`

### F5 — total_time สะสมจริง (LMS-maintained ตามสเปค)

1. **API:** endpoint player-info (`EnrollmentsController`) — เพิ่ม `TotalSecondsPlayed` (int) ใน content item ของ `PlayerInfoDto` (ดึงจาก `LearningLog.TotalSecondsPlayed` ของ enrollment+contentItem ที่ active หลัง `ResetAt` — ใช้ query ที่มีอยู่ใน rollup เป็นแบบ)
2. **Player:** ใน `resetScormModel` คำนวณ `totalTime = ScormDurationParser-equivalent ฝั่ง JS` จาก `contentItem.totalSecondsPlayed` → format ตามเวอร์ชัน (1.2 = `HHHH:MM:SS`, 2004 = `PTnHnMnS`) แทนการ echo `runtimeState.totalTime` ตรง ๆ; ถ้า `totalSecondsPlayed` ไม่มี (ข้อมูลเก่า) fallback พฤติกรรมเดิม
3. **ห้าม** total_time ลดลง: ใช้ `Math.max(totalSecondsPlayed, parse(runtimeState.totalTime))`
4. หมายเหตุ front-page: หน้า summary modal ใช้ `clientTime` (session) อยู่แล้ว — อย่าเปลี่ยน display logic

**Acceptance:** เรียน 2 รอบ (ปิด/เปิดใหม่) → SCO เรียก `GetValue("cmi.core.total_time")` ได้ค่าสะสมของทั้งสองรอบ ไม่ใช่รอบสุดท้าย

---

## Constraints

- ❌ ห้ามแตะ guard เดิม: placeholder-commit protection, terminal-status protection (`PreferStatus`/`HasTerminalProgress`), path/zip security ใน `ScormService`
- ❌ ห้ามลบ key เดิมใน cmiModel (รวม `cmi.core.learner_id` นอกสเปค) — เพิ่มเท่านั้น
- ❌ ห้ามเปลี่ยน rollup/completion logic (`UpdateEnrollmentRollupAsync`) นอกจาก fallback score ใน F4 ข้อ 5
- ❌ Phase 2/3 (error state machine, vocab validation, masteryscore/dataFromLMS, multi-SCO warning) — **นอก scope** ห้ามแถม
- ⚠️ ข้อมูล `TotalSecondsPlayed` เก่าที่เคยเป็น 0 จาก F2 กู้ไม่ได้ — known data gap ไม่ต้อง backfill

## Verification (รันก่อนปิดงาน)

- [x] `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (รวม `ScormDurationParserTests` ใหม่ + test เดิม 136+ ตัวไม่แตก) แล้วลบ artifacts _(implementer ผ่าน; reviewer รันซ้ำอิสระ 2026-07-13: **178/178 passed**)_
- [x] Migration สร้าง/apply ได้ (`dotnet ef migrations script` ตรวจ SQL มีแค่ ADD COLUMN ScaledScore) _(reviewer ตรวจไฟล์ migration + model snapshot: มีแค่ `ScaledScore decimal(18,2) NULL` จริง)_
- [ ] **E2E กับ iSpring golden packages** — ทดสอบด้วย **learner code `610034`** (ตามที่ผู้ใช้ระบุ 2026-07-13) — ผู้ใช้เตรียม package ไว้แล้วที่ `SampleSCORM\USECASE\` (ตรวจ manifest ยืนยันแล้ว 2026-07-13; ทุกตัว single-SCO, manifest อยู่ใน subfolder ของ zip — `FindManifestPath` รองรับอยู่แล้ว):

  > **หมายเหตุสำคัญ:** `610034` เคยใช้ทดสอบ E2E บน **PROD** มาก่อน (course 507, ดู AGENT_LOG 2026-07-03/PLAN-047) — รอบนี้ทดสอบบน **QA** ซึ่งเป็นคนละฐานข้อมูล enrollment เดิมบน PROD **ไม่ข้ามมาที่ QA ให้อัตโนมัติ** ตัวรหัสพนักงานเองผูกกับ EmployeeHub/Legacy provider (ไม่ผูก environment) จึง lookup ผ่านได้ปกติ แต่ต้อง **สร้าง course + content item (จาก 4 golden packages) + enroll 610034 ใหม่บน QA** ก่อนทดสอบ — ไม่ใช่ enrollment เดิมจาก PROD

  | ไฟล์ | SCORM | ชนิด | ใช้ทดสอบ |
  |---|---|---|---|
  | `NTC-WI-PD2-050_12_Learn.zip` | 1.2 | Learn | F1 resume, F5 total_time สะสม |
  | `NTC-WI-PD2-035_12_Exam.zip` | 1.2 | Exam | F4 score + exam policy เดิมไม่แตก |
  | `NTC-WI-PD2-711_2004_Learn.zip` | 2004 4th Ed. | Learn | **F2 (ตัวชี้ขาด):** `TotalSecondsPlayed` > 0, F5 |
  | `NTC-WI-PD2-334_2004_Exam.zip` | 2004 4th Ed. | Exam | F4 `ScaledScore` + exam ต้อง passed ไม่ใช่แค่ completed |

  เช็คลิสต์ต่อ package:
  - เปิดเรียน → ปิดกลางคัน → เปิดใหม่ **resume ตรงตำแหน่งเดิม** (กติกา regression สำคัญสุด — F1 แตะ identity ที่ iSpring ใช้ผูก resume)
  - SCORM 2004: เรียนจบแล้ว `LearningLog.TotalSecondsPlayed` > 0 (F2) และ `cmi.core.total_time` สะสมข้ามรอบ (F5)
  - Exam: ทำ quiz ให้คะแนน → Score ขึ้นรายงาน (F4); เคส exam ได้ completed แต่ไม่ passed ต้องยัง incomplete (policy เดิม)
- [ ] Console log ฝั่ง player ไม่มี error ใหม่ระหว่างเล่น
- [ ] Regression: content เดิมบน dev/QA ที่เคยเล่นได้ ยังเล่น/resume/จบได้ปกติ

## Implementer Notes

- **F1:** Added `learnerName` from `ClaimTypes.Name` in Razor header + `currentLearnerName` JS variable. Added `cmi.core.student_id`/`cmi.core.student_name` (SCORM 1.2 spec keys) and changed all `learner_name` values to use real name. Kept legacy `cmi.core.learner_id`/`cmi.core.learner_name` keys intact.
- **F2:** Created `ScormDurationParser.cs` using `XmlConvert.ToTimeSpan` for ISO8601 and manual parse for 1.2 timespan (supports up to 4-digit hours + centiseconds). Replaced `ParseSessionTime` in LearningLogsController. Updated `parseClockTimeToSeconds` JS with ISO8601 regex. Also added `formatSecondsToScormDuration` JS helper. **22 unit tests** covering both formats + edge cases + roundtrips — all pass.
- **F3:** Added all `_children`/`_count` keys for both SCORM versions in `resetScormModel`. Changed `setCmiModelValue` regex from `interactions` only to `interactions|objectives` for auto-increment.
- **F4:** Added `ScaledScore` to entity + both DTOs + `ApplyCommit` (reuses `PreferRawScore` guard) + `MapToDto`. Added to player payload/commit. Fallback in `MapRuntimeCommitToProgress`: if RawScore is null but ScaledScore present, uses `scaled * 100`. Migration: single `ADD COLUMN ScaledScore decimal(18,2) nullable`.
- **F5:** Added `TotalSecondsPlayed` to `PlayerContentItemDto` + populated from `LearningLog` in EnrollmentsController. Added `computeTotalTime` JS that uses `Math.max(totalSecondsPlayed, parsed runtimeState.totalTime)` → formats to correct SCORM version string. Total_time never decreases.
- **React contract sync:** Verified — grep confirms no React type mirrors for ScormRuntimeStateDto or PlayerContentItemDto. No frontend changes needed.
- **Verification:** Build 0 errors, 178 tests pass (was 156 before previous plans + 22 new ScormDurationParser tests), migration clean.
- **E2E with golden packages:** Pending user manual verification with iSpring content from `SampleSCORM\USECASE\`, using learner code `610034` on QA.

## Reviewer Sign-off (Claude Code — 2026-07-13)

**Code review: ผ่านครบทั้ง 5 fixes** — ตรวจ diff ทุกไฟล์ + รัน verification ซ้ำอิสระ:

- **F1:** `student_id/student_name` เพิ่มถูกตำแหน่ง, `learner_name` ใช้ชื่อจริงจาก `ClaimTypes.Name` (fallback = code), legacy keys คงไว้ครบตาม constraint ✅
- **F2 (แก่นของงาน):** `ScormDurationParser` ถูกต้อง — 1.2 timespan validate นาที/วินาที ≤59 + centiseconds + 4-digit hours, ISO8601 ผ่าน `XmlConvert.ToTimeSpan` ครอบ try/catch; `ParseSessionTime` ถูกแทนที่จริง; JS regex ISO8601 ตรงกับฝั่ง server; **tests 33 cases (22 theories) ครอบทั้งสอง format + edge + roundtrip — reviewer รันซ้ำผ่าน 178/178** ✅
- **F3:** `_children`/`_count` ครบสองเวอร์ชันตามสเปคในแผนเป๊ะ; regex auto-increment ครอบ objectives แล้ว ✅
- **F4:** `ScaledScore` ครบทั้ง entity/DTO×2/ApplyCommit (ใช้ `PreferRawScore` guard เดิม — placeholder protection ไม่ถูกลดทอน)/MapToDto/player payload/fallback scaled×100; migration สะอาด single ADD COLUMN ✅
- **F5:** `TotalSecondsPlayed` ดึงจาก log ตัวเดียวกับที่ DTO ใช้อยู่ (เคารพ ResetAt window เดิม); `computeTotalTime` มี max-guard ไม่ให้ total_time ลดลง; format ตามเวอร์ชัน ✅
- **Constraints:** ไม่แตะ guard เดิม, ไม่มี scope creep (diff เฉพาะไฟล์ในแผน + snapshot มีแค่ ScaledScore), React ไม่มี contract ต้อง sync (ยืนยันซ้ำ) ✅

ข้อสังเกต minor (ไม่ blocking — เก็บเป็น follow-up ได้):
1. `computeTotalTime` เคส 0 วินาที คืน `"00:00:00"` ให้ SCO 2004 (ควรเป็น `"PT0S"`) — พฤติกรรมเท่าของเดิมก่อนแก้ ไม่ใช่ regression; แก้ได้ด้วยการเรียก `formatSecondsToScormDuration` เสมอไม่ต้องเช็ค `> 0`
2. `ScaledScore decimal(18,2)` เก็บทศนิยม 2 ตำแหน่ง — scaled ละเอียดกว่านั้น (เช่น 0.8333) จะถูกปัด แต่ผลต่อ Score (int) ≤ 1 คะแนน — ยอมรับได้
3. Razor `'@learnerName'` HTML-encode ในบริบท JS string (ชื่อมี apostrophe จะกลายเป็น `&#x27;`) — pattern เดียวกับ `@learnerCode` เดิม และชื่อจาก EmployeeHub เป็นอังกฤษล้วน — รับได้

**สถานะ: DONE (code review ผ่าน) — ยังไม่ VERIFIED จนกว่า E2E กับ golden packages จะผ่าน** (ต้อง deploy QA: API + iLearn.User + apply migration บน QA DB ก่อน แล้วเล่น 4 packages ตามตาราง Verification)

## Next Steps — มอบ GitHub Copilot: commit + deploy QA (2026-07-13)

ผู้ใช้ต้องการทดสอบ E2E เองบน QA ด้วย golden packages — ให้ Copilot ทำต่อดังนี้:

1. **Commit** โค้ดทั้งหมดของ PLAN-079 (ดู `git status` — ไฟล์ที่แก้/ใหม่ตรงกับหัวข้อ Implementer Notes + migration 2 ไฟล์ + `ScormDurationParserTests.cs`) พร้อม PLAN docs ที่เกี่ยวข้อง (076/077/078/079 + `lms-standard-conformance-assessment.md` + `AGENT_LOG.md`) — ใช้ commit message อธิบาย PLAN-079 F1–F5
2. **Apply migration บน QA DB โดยตรงด้วย `dotnet ef database update`** (เปลี่ยนจาก idempotent script + sqlcmd ตามคำสั่งผู้ใช้ 2026-07-13) — **ต้องระบุ `--connection` แบบ explicit เสมอ ห้ามพึ่ง `ASPNETCORE_ENVIRONMENT`**:
   ```powershell
   dotnet ef database update `
     --project iLearn.Infrastructure --startup-project iLearn.API `
     --connection "Data Source=AP-NTC2138-QADB;Database=iLearnDB_New;Persist Security Info=True;User ID=sa;Password=<จาก iLearn.API/appsettings.json>;Trust Server Certificate=True"
   ```
   ⚠️ **เหตุผลที่ต้องระบุ `--connection` ตรง ๆ (ห้ามละขั้นตอนนี้):** repo นี้มี connection string **ต่างกัน 3 ชุด** ตาม environment — base `appsettings.json` (ไม่มี suffix) ชี้ QA จริง (`AP-NTC2138-QADB`), `appsettings.Development.json` ชี้เครื่องอื่น (`10.10.143.37`), `appsettings.Production.json` ชี้ PROD (`AP-NTC2139-COSS`) ถ้ารัน `dotnet ef database update` เฉย ๆ โดยไม่ระบุ `--connection` จะได้ connection ตาม `ASPNETCORE_ENVIRONMENT` ของเครื่องที่รัน ณ ขณะนั้น (ค่า default ที่ไม่ตั้งคือ `Production` เมื่อรันนอก IIS) — เสี่ยงไป apply ผิดเครื่อง ตรงกับ class of bug ที่เคยเกิดจริงใน [PLAN-051](PLAN-051-qa-env-contamination-and-prod-student-500.md) (QA อ่าน/เขียน PROD DB โดยไม่ตั้งใจ) ระบุ `--connection` ชัดเจนตัดปัญหานี้ทิ้งไปเลย
   - ยืนยันหลังรัน: query `SELECT COL_LENGTH('ScormRuntimeStates','ScaledScore')` ต้องไม่เป็น NULL และ `__EFMigrationsHistory` มี `AddScaledScoreToScormRuntimeState`
3. **Deploy ขึ้น QA** ทั้ง 2 app ที่มีโค้ดเปลี่ยน: `tools/deploy-api.ps1` (API — LearningLogsController/EnrollmentsController) และ `tools/deploy-user.ps1` (iLearn.User — Player.cshtml) — **ไม่ต้อง** deploy admin/admin-react (ไม่ถูกแตะในแผนนี้)
4. Smoke check เบื้องต้นหลัง deploy: `/iLearn/Service/api/health` (หรือ smoke endpoint ที่มี) = 200 บน QA
5. อัปเดต Implementer Notes ของแผนนี้ด้วยผล commit hash / migration apply log / deploy stamp — **ไม่ต้อง mark VERIFIED เอง** (ผู้ใช้จะทดสอบ E2E ด้วย 4 golden packages ตามตารางในหัวข้อ Verification เอง แล้ว Claude Code จะปิดงานหลังผู้ใช้ยืนยันผล)

### Constraints เพิ่มเติมสำหรับขั้นนี้
- ❌ ห้าม deploy ขึ้น PROD ในรอบนี้ — QA เท่านั้น
- ❌ ห้ามรัน `dotnet ef database update` โดยไม่ระบุ `--connection` — ต้องชี้ QA DB ตรง ๆ ตามที่ระบุในขั้น 2 เท่านั้น (กัน environment ambiguity ตามที่อธิบายไว้)
- ⚠️ migration นี้เป็น `ADD COLUMN` เดี่ยว ๆ ไม่มี data migration ซับซ้อน — รันตรงปลอดภัยกว่ากรณี PLAN-057 ที่มี CREATE VIEW ปนอยู่
