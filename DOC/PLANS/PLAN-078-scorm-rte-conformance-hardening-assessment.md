# PLAN-078: ประเมิน SCORM 1.2/2004 RTE Conformance เชิงลึก + แผนยกระดับให้ "รองรับดีที่สุด"

- **Status:** DECIDED — ผู้ใช้ตอบ decision ครบ (2026-07-13): ✅ เริ่ม Phase 1 ทันที → แตกเป็น [PLAN-079](PLAN-079-scorm-conformance-phase1-fixes.md) (Assigned: GitHub Copilot) / เนื้อหาองค์กรใช้ **iSpring** (publish single-SCO เสมอ → Phase 3 ใช้ทาง ก เป็น default, ไม่เร่ง) / Phase 2 รอผลของ Phase 1 ก่อน
- **Assigned:** — (ตัว assessment ปิดแล้ว; งาน implement อยู่ที่ PLAN-079)
- **Author:** Claude Code (planner)
- **Reviewer:** —
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [lms-standard-conformance-assessment.md](../lms-standard-conformance-assessment.md) (ประเมินภาพรวม — ให้ SCORM engine 🟢), [PLAN-077](PLAN-077-lms-level-completion-enforcement-assessment.md) (F2 ในเอกสารนี้เป็น prerequisite ของ time-gate), `DOC/SCORM-RUNTIME-LIFECYCLE-RULES.md`

> มาจากทิศทางผู้ใช้ (2026-07-13): "เน้น SCORM engine 1.2/2004 เป็นหลักก่อน เพราะใช้เป็นมาตรฐาน จุดนี้ต้องรองรับให้ดีที่สุด" — เอกสารนี้เจาะ conformance เทียบสเปค SCORM RTE (Run-Time Environment) ทีละหมวด จากโค้ดจริงทั้งฝั่ง player ([Player.cshtml](../../iLearn.User/Views/MyLearning/Player.cshtml) — API adapter) และฝั่ง server (`ScormRuntimeStateService`, `LearningLogsController`, `ScormService`)

---

## 1. ภาพรวมสถาปัตยกรรม SCORM runtime ปัจจุบัน

```
SCO (iframe, same-origin จาก /Courses/{guid}/...) 
  → window.API (SCORM 1.2) / window.API_1484_11 (SCORM 2004)   [Player.cshtml:1704/1768]
  → cmiModel (JS dict ต่อ content item, reset ตอนเปิด)          [resetScormModel :891]
  → LMSCommit/Commit/Finish/Terminate → flushSelectedContentItemRuntime
  → POST MyLearning/CommitRuntime (proxy) → API LearningLogs/commit-runtime
  → ScormRuntimeStateService (raw state) + LearningLog (rollup) → Enrollment.IsCompleted
```

**จุดแข็งที่มีอยู่แล้ว (ควรคงไว้ — อย่าทำ regression):**
- Alias sync สองเวอร์ชันในโมเดลเดียว (`cmi.core.*` ↔ `cmi.*`) + `getPreferredCmiModelValue` เลือก key ตามเวอร์ชัน
- Placeholder-commit guard ฝั่ง server กัน status/score ถอยหลังจาก commit ว่อง ๆ ตอนปิด ([ScormRuntimeStateService:162-235](../../iLearn.Infrastructure/Services/ScormRuntimeStateService.cs))
- Resume ครบ: suspend_data + lesson_location + entry `resume`/`ab-initio` heuristic
- `cmi.interactions.N.*` เก็บลง dict + auto-increment `_count` + snapshot ทั้งก้อนใน `CmiSnapshotJson`
- Exam vs Learn policy centralize ที่ `ScormContentStatusPolicy` (exam ต้อง passed ไม่ใช่แค่ completed)
- Initial `lesson_status = "not attempted"` ถูกต้องตามสเปค ([deriveLessonStatus:773](../../iLearn.User/Views/MyLearning/Player.cshtml))

---

## 2. Findings — เรียงตามผลกระทบจริง

### 🔴 กลุ่ม A: บั๊กแท้ (เนื้อหาที่ทำตามสเปคจะทำงานผิด/ข้อมูลหาย — ควรแก้ก่อน)

| # | Finding | หลักฐาน | ผลกระทบ |
|---|---|---|---|
| **F1** | **SCORM 1.2 ใช้ key ผิด: ไม่มี `cmi.core.student_id` / `cmi.core.student_name`** — โมเดลใส่เป็น `cmi.core.learner_id/learner_name` (ชื่อของ 2004) | [Player.cshtml:906-907](../../iLearn.User/Views/MyLearning/Player.cshtml) | SCO 1.2 ที่เรียก `LMSGetValue("cmi.core.student_id")` ได้ `""` — package จำนวนมาก (Storyline/Captivate) ใช้ค่านี้ผูก resume state ภายใน/แสดงชื่อผู้เรียน → resume เพี้ยน/ชื่อว่าง |
| **F2** | **ไม่รองรับ ISO8601 duration ของ SCORM 2004** — `session_time` ของ 2004 ตามสเปคคือ `PT1H5M30S` แต่ (ก) client `parseClockTimeToSeconds` รับเฉพาะ `HH:MM:SS` ([Player.cshtml:827-839]) (ข) server `ParseSessionTime` ใช้ `TimeSpan.TryParse` ซึ่ง parse ISO8601 ไม่ได้ ([LearningLogsController.cs:267-275](../../iLearn.API/Controllers/LearningLogsController.cs)) | ทั้งสองจุดคืน **0 วินาที** | เวลาเรียนของ SCO 2004 ที่รายงานตามสเปค **หายทั้งหมด** (`TotalSecondsPlayed=0`) → รายงานเวลาเรียนผิด และเป็น **blocker ของ PLAN-077 time-gate** |
| **F3** | **`cmi.objectives._count` ไม่ถูก initialize** (มีแค่ `interactions._count`) และไม่มี `_children` เลย (`cmi.core._children`, `cmi.score._children`, ฯลฯ) | [Player.cshtml:945] | SCO อ่าน `_count` ได้ `""` → `parseInt` เป็น NaN ใน content บางตัว; SCO ที่ probe `_children` เพื่อตรวจ capability ตีความว่า LMS ไม่รองรับ |
| **F4** | **`cmi.score.scaled` (2004) ไม่ถูก persist** — เก็บใน dict/snapshot เท่านั้น server มีแต่ `RawScore` | `ScormRuntimeState` ไม่มี field; commit DTO ไม่ส่ง | SCO 2004 ที่รายงานเฉพาะ scaled (พบบ่อยใน quiz สมัยใหม่) → **คะแนนหายจากรายงาน** |
| **F5** | **`total_time` ไม่ accumulate** — สเปคกำหนดให้ LMS สะสม session_time ทุก attempt เข้า total_time (read-only ฝั่ง SCO) แต่ปัจจุบัน echo ค่าล่าสุดกลับไป | [Player.cshtml:899,935] + `PreferDuration` ฝั่ง server แค่กันค่า 0 ทับ | SCO ที่แสดง "เวลาเรียนสะสม" ได้ค่าผิด — ทั้งที่ server มี `LearningLog.TotalSecondsPlayed` สะสมถูกอยู่แล้ว แค่ไม่ถูกแปลงกลับเป็น `cmi.core.total_time` ตอน relaunch |

### 🟡 กลุ่ม B: ไม่ตรงสเปค RTE (content ส่วนใหญ่ทนได้ แต่ strict content / ADL test suite ไม่ผ่าน)

| # | Finding | หลักฐาน | หมายเหตุ |
|---|---|---|---|
| **F6** | **ไม่มี error state machine ทั้งระบบ** — `GetLastError` คืน `"0"` เสมอ, `GetErrorString` คืน `""` (สเปคบังคับ non-empty สำหรับ code ที่ valid), ไม่ track สถานะ Not Initialized → Running → Terminated (Initialize ซ้ำต้องคืน false + error 103/101, เรียกหลัง Terminate ต้อง error 123 ฯลฯ) | [Player.cshtml:1760-1762, 1828-1830] | SCO ที่เขียน defensive (เช็ค error ทุกครั้ง) และ ADL Conformance Test จะ fail; `SetValue` ตอน iframe inactive คืน `"true"` ทั้งที่ไม่ได้เก็บ = โกหก SCO |
| **F7** | **ไม่ validate vocabulary + read-only/write-only** — SCO เขียนทับ `learner_id` ได้, เขียน `lesson_status` ค่านอก vocab ได้ (ควร error 405), `GetValue("cmi.core.session_time")` ควร error (write-only) แต่คืนค่า | `LMSSetValue`/`SetValue` รับทุกอย่างเข้า dict | เสี่ยง state เพี้ยนจาก content เขียนผิด และไม่ผ่าน conformance test |
| **F8** | **ไม่ส่งต่อค่าจาก manifest สู่ runtime** — `ScormService` parse แค่ `launchHref` + `schemaversion`; ไม่อ่าน `adlcp:masteryscore` → `cmi.student_data.mastery_score` (สเปค 1.2: LMS ควร derive lesson_status จาก score เทียบ mastery), `adlcp:dataFromLMS` → `cmi.launch_data`, `maxtimeallowed`/`timeLimitAction` | [ScormService.cs:235+](../../iLearn.Infrastructure/Services/ScormService.cs) | Package ที่พึ่ง `launch_data` เพื่อ config ตัวเอง **พังเงียบ**; mastery-based pass/fail ไม่ทำงาน |
| **F9** | **Multi-SCO / organizations ถูกละเลย** — `FindLaunchPage` เลือก SCO แรกตัวเดียว; ไม่ parse `<organizations>/<item>` tree; SCORM 2004 Sequencing & Navigation (imsss, `adl.nav.request`) ไม่มี; **ตอน upload ไม่เตือน** ว่า package มีหลาย SCO | [ScormService.cs:401-442] | Package multi-SCO ถูกยุบเหลือบทแรกโดยผู้ดูแลไม่รู้ตัว (TOC ของ player คือ content items ของ iLearn ไม่ใช่โครงสร้างใน package) |
| **F10** | **ข้อมูล interactions/objectives ไม่ถูกใช้เชิงรายงาน** — อยู่ใน `CmiSnapshotJson` (cap 256KB) เท่านั้น query ไม่ได้ | `ScormRuntimeState.CmiSnapshotJson` | ตอบโจทย์ diagnose แต่ยังไม่ตอบ item-level exam report (โยง P8 ใน assessment ภาพรวม) |

### เรื่องที่ตรวจแล้ว "ผ่าน" (ไม่ต้องแก้)
- suspend_data limit 65535 ฝั่ง transport — ครอบสเปคทั้ง 1.2 (4096) และ 2004 (64000) แบบ generous ✅
- API discovery: SCO ใน iframe same-origin หา `window.API` ผ่าน parent chain ได้ตามสเปค ✅
- Initial entry/exit, not attempted, credit/mode defaults ✅

---

## 3. แผนยกระดับ (เสนอเป็น 3 เฟส — แต่ละเฟสปิดจบในตัว)

### Phase 1 — แก้บั๊กแท้ (กลุ่ม A) — **effort S-M, ความเสี่ยงต่ำ, คุ้มสุด**
1. เพิ่ม `cmi.core.student_id`/`cmi.core.student_name` ใน `resetScormModel` (F1) — 2 บรรทัด + ส่งชื่อจริงของ learner จาก session (ตอนนี้ใช้รหัสพนักงานแทนชื่อ)
2. เขียน parser duration กลาง (รับทั้ง `HH:MM:SS` และ ISO8601 `PT#H#M#S`) ใช้ทั้ง client (`parseClockTimeToSeconds`) และ server (`ParseSessionTime`) + คืน `total_time` เป็น format ตามเวอร์ชัน (F2) — **มี unit test ครอบ format ทั้งสอง**
3. Initialize `cmi.objectives._count = "0"` + ชุด `_children` มาตรฐานของทั้งสองเวอร์ชัน (F3)
4. เพิ่ม `ScaledScore` ใน `ScormRuntimeState` + commit DTO + migration (F4)
5. แปลง `TotalSecondsPlayed` สะสม → `cmi.core.total_time`/`cmi.total_time` ตอน reset model (F5)

### Phase 2 — RTE compliance (กลุ่ม B: F6-F8) — effort M
1. ทำ error state machine + error codes + `GetErrorString` ตามตาราง spec (แยกเป็น JS module ทดสอบได้)
2. Validate vocabulary + read-only/write-only ต่อ element (ตารางจากสเปค 1.2/2004)
3. `ScormService` parse `masteryscore`/`dataFromLMS` → เก็บใน `ContentItem` (fields ใหม่ + migration) → inject เข้า `cmiModel` + ใช้ mastery override lesson_status ตามสเปค 1.2

### Phase 3 — นโยบาย multi-SCO (F9) — **ต้องตัดสินใจ ไม่ใช่แค่โค้ด**
- **ทาง ก (แนะนำ):** ประกาศ "1 content item = 1 SCO" เป็น design ของ iLearn (สอดคล้องแนวแบ่งตอนใน [PLAN-076](PLAN-076-large-scorm-file-support-assessment.md) Option D) + **เพิ่ม validation ตอน upload**: ถ้า manifest มีหลาย item/SCO → เตือน admin ให้แตกเป็นหลาย content item (ไม่ silent)
- ทาง ข: รองรับ organizations + SN เต็มรูปแบบ — งานใหญ่มาก (effort L+) ไม่คุ้มถ้า content ในองค์กรเป็น single-SCO ทั้งหมด
- F10 (interactions report) แยกไปรวมกับงาน reporting (P8) ไม่ผูกกับเฟสนี้

### Testing strategy (ทุกเฟส)
- **Golden packages:** เก็บ SCORM ตัวอย่างจาก authoring tool ที่องค์กรใช้จริง (1.2 และ 2004 อย่างละตัว) ไว้เป็น regression set ใน repo/test share
- ADL SCORM 1.2 Test Suite + SCORM 2004 4th Ed. Conformance Test Suite รันกับ player (manual รอบใหญ่)
- xUnit ครอบ: duration parser, status normalization (มีอยู่แล้วบางส่วนใน `ScormRuntimeStateServiceTests`), manifest parse fields ใหม่

## 4. Decision points (ผู้ใช้)

1. **เริ่ม Phase 1 เลยไหม?** — เป็นบั๊กแท้ 5 ตัว แก้ได้โดยไม่กระทบ content เดิม (backward-compatible ทั้งหมด) _(แนะนำ: เริ่มเลย — โดยเฉพาะ F1/F2 ที่กระทบข้อมูลจริงวันนี้)_
2. **เนื้อหาในองค์กรสร้างจาก tool อะไรบ้าง?** (Storyline / iSpring / Captivate / จ้างทำ) — ใช้เลือก golden packages และยืนยันว่า F1/F2 กระทบ content ที่มีอยู่แค่ไหน
3. **เคยมี/จะมี package แบบ multi-SCO ไหม?** — ชี้ขาด Phase 3 ทาง ก (validate+เตือน) หรือ ข (รองรับเต็ม)
4. **Assign ใคร?** — Phase 1 เหมาะกับ implementer ตัวใดตัวหนึ่ง (โค้ดกระจาย client JS + server parser + migration เล็ก) — ถ้าอนุมัติ ผมจะแตกเป็น PLAN-079 (implement Phase 1) พร้อมสเปคละเอียดต่อ finding

## Constraints (สำหรับแผน implement)

- ❌ ห้ามทำลาย guard เดิม (placeholder-commit protection, terminal-status protection, path/zip security ใน ScormService)
- ❌ การเพิ่ม validation (F6/F7) ต้องไม่ทำให้ content เดิมที่เคยเล่นได้ **เล่นไม่ได้** — error ต้อง "รายงานตามสเปค" ไม่ใช่ "ปฏิเสธการทำงาน" (log + คืน error code แต่ยังเก็บค่าที่ valid)
- ⚠️ ทุกการแก้ duration/score ต้องมี migration path สำหรับข้อมูลเดิม (TotalSecondsPlayed ที่เคยบันทึกเป็น 0 จาก F2 กู้ไม่ได้ — ยอมรับเป็น known data gap ตั้งแต่วันเริ่มระบบ)
