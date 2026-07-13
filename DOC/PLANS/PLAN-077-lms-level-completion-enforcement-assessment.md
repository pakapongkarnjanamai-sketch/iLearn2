# PLAN-077: ประเมินการบังคับ completion ระดับ LMS (ไม่พึ่ง SCORM package) — บังคับ "ดูจนครบ" ฝั่ง iLearn เอง

- **Status:** ASSESSMENT (เอกสารตัดสินใจ — ยังไม่แก้โค้ด; รอ decision จากผู้ใช้ก่อนแตกเป็นแผน implement)
- **Assigned:** — (ยังไม่มอบ implementer)
- **Author:** Claude Code (planner)
- **Reviewer:** —
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [PLAN-076](PLAN-076-large-scorm-file-support-assessment.md) (SCORM ไฟล์ใหญ่ — คุยกันต่อเนื่องเรื่องวิดีโอ)

> มาจากคำถามผู้ใช้ (2026-07-13): อยากให้ iLearn บังคับ completion เอง (ไม่พึ่งให้ SCORM package เป็นคนตัดสิน) เช่น กันเคสเนื้อหาที่ mark completed ทันทีตอนเปิด เอกสารนี้ประเมินว่าทำได้แค่ไหน จุดไหน และข้อจำกัดสำคัญ

---

## 1. Completion pipeline ปัจจุบัน (ยืนยันจากโค้ดจริง)

```
SCORM content (JS) เล่น → commit ทุกช่วง → POST /api/LearningLogs/commit-runtime  (หรือ update-progress)
   │
   ├─ (1) MapRuntimeCommitToProgress → ScormContentStatusPolicy.ResolveStatus       // ตัดสิน passed/completed/incomplete ต่อ content item
   │        [LearningLogsController.cs:440] + [ScormContentStatusPolicy.cs:8]         // ← เชื่อ lesson_status/completion_status ที่ package ส่งล้วน
   │
   ├─ (2) ScormRuntimeStateService.UpsertAsync → ScormRuntimeState                    // เก็บ raw runtime: SessionTime, TotalTime, RawScore, status
   │        [ScormRuntimeStateService.cs:40]
   │
   ├─ (3) UpsertLearningLogsAsync → LearningLog                                        // Status, Progress, TotalSecondsPlayed (สะสม), Score, AttemptCount
   │        [LearningLogsController.cs:323]
   │
   └─ (4) UpdateEnrollmentRollupAsync → Enrollment.IsCompleted                         // ← จุดตัดสิน completion ของทั้งคอร์ส
            [LearningLogsController.cs:388]
```

**จุดตัดสิน completion ทั้งคอร์ส** ([LearningLogsController.cs:400-409](../iLearn.API/Controllers/LearningLogsController.cs)):
```csharp
int passedCount = updatedLogs.Count(log => ...(log.Status == "passed" || log.Status == "completed"));
if (passedCount >= allContentItemIds.Count && allContentItemIds.Count > 0) {
    enrollment.IsCompleted = true; enrollment.Progress = 100;
}
```
⇒ ปัจจุบัน "จบ" = **ทุก content item มี status passed/completed** โดย status นั้น**มาจาก package** ไม่มีเงื่อนไขเวลา/พฤติกรรมเพิ่ม

## 2. Signal ที่ LMS "มี" vs "ไม่มี" (หัวใจของการประเมิน)

| Signal | มีไหม | ที่เก็บ | ใช้ enforce ได้ไหม |
|---|---|---|---|
| เวลาเล่นสะสมต่อ content | ✅ มี | `LearningLog.TotalSecondsPlayed` ([LearningLog.cs:26](../iLearn.Domain/Entities/LearningLog.cs)) | ✅ ทำ time-gate ได้ |
| session_time / total_time (SCORM) | ✅ มี | `ScormRuntimeState.SessionTime/TotalTime` ([ScormRuntimeState.cs:39](../iLearn.Domain/Entities/ScormRuntimeState.cs)) | ✅ |
| คะแนน / จำนวน attempt | ✅ มี | `LearningLog.Score` / `AttemptCount` | ✅ |
| เวลา commit จริง (server clock) | ⚠️ มีบางส่วน | `ScormRuntimeState.LastCommittedAtUtc` + `LearningLog.CreatedAt` | ✅ ถ้าเพิ่ม logic นับ elapsed ฝั่ง server |
| **% ของวิดีโอที่ดูจริง / played ranges** | ❌ **ไม่มี** | — | ❌ SCORM ไม่รายงานให้ LMS |
| จำนวน/ความยาววิดีโอในแต่ละ SCO | ❌ ไม่มี | — (อยู่ใน package) | ❌ |

**ข้อจำกัดที่ต้องพูดตรง ๆ:** LMS มองเห็นแค่ "SCO เปิดอยู่กี่วินาที" (session_time) + "status ที่ package บอก" — **ไม่เห็นว่าในหน้านั้นมีวิดีโอกี่คลิป ยาวเท่าไหร่ ผู้เรียนลากไปถึงไหน** ข้อมูลระดับวิดีโออยู่ใน package เท่านั้น

⇒ **"บังคับดูวิดีโอจนครบจริง ๆ" แบบพิสูจน์ได้ ทำที่ LMS ล้วนไม่ได้** สิ่งที่ LMS ทำได้คือ **proxy เชิงเวลา/พฤติกรรม** ซึ่งเป็น deterrent ที่ดี แต่ไม่ใช่ proof (เปิดค้างไว้เฉย ๆ ก็ผ่าน time-gate)

## 3. ตัวเลือก (พร้อม trade-off + effort)

### Option 1 — Minimum time-on-content gate (เบาสุด, ทำได้จริงเร็ว)
- **ทำ:** เพิ่มเงื่อนไขก่อนยอมรับ `passed/completed` ต่อ content item — ต้อง `TotalSecondsPlayed >= MinimumSeconds` ที่ตั้งไว้ ไม่งั้น cap เป็น `incomplete` ที่ [MapRuntimeCommitToProgress](../iLearn.API/Controllers/LearningLogsController.cs:440) / rollup
- **ต้องมี config:** field ใหม่ระดับ `ContentItem` (เช่น `MinimumSeconds`) หรือระดับ course/version → **schema + migration + admin UI ให้ตั้งค่า**
- **ข้อดี:** ใช้ signal ที่มีอยู่แล้ว, กันเคส "mark completed ทันที" ได้ทันที
- **ข้อเสีย:** `session_time` เชื่อค่าที่ **client/package รายงาน** — ผู้ใช้ที่ตั้งใจโกงแก้ค่าได้ (ดู Option 2 กัน)
- **Effort:** S–M (gate logic + 1 migration + admin UI 1 จุด)

### Option 2 — Server-side elapsed time (แข็งกว่า, กัน client โกงเวลา)
- **ทำ:** นับเวลาจาก **timestamp ฝั่ง server** (`LastCommittedAtUtc`/`CreatedAt` ต่อ commit) แทนการเชื่อ `session_time` จาก client + อาจใช้ heartbeat commit เป็นช่วง ๆ เพื่อยืนยันว่าเปิดจริงต่อเนื่อง
- **ข้อดี:** ทนการปลอม `session_time`, สะท้อนเวลาเปิดจริง
- **ข้อเสีย:** ยังเป็น "เวลาเปิด" ไม่ใช่ "ดูวิดีโอจริง" (เปิดค้าง tab ก็นับ), ต้องคิด idle/tab-hidden detection ถ้าจะแม่น
- **Effort:** M (เพิ่ม accumulation logic + อาจแก้ commit cadence ฝั่ง player JS)

### Option 3 — เสริม gate ระดับ enrollment / ordering (มีฐานอยู่แล้วบางส่วน)
- **ทำ:** ต่อยอด rollup — เช่น บังคับเรียงลำดับ (content ก่อนหน้าต้องจบก่อนปลดล็อกถัดไป), หรือบังคับ minimum total time ทั้งคอร์ส
- **ข้อดี:** ระบบมี `Order` + all-content-passed อยู่แล้ว ต่อยอดไม่ไกล
- **Effort:** S–M

### Option 4 — บังคับวิดีโอจริง (ต้องมี video signal — นอกขอบ "LMS ล้วน")
- **ทาง 4a (มาตรฐาน):** author ใน SCORM ให้ package เป็นคนบังคับ (ที่คุยใน turn ก่อน) — LMS รับผลถูกต้องอยู่แล้ว **ไม่ต้องพัฒนา LMS**
- **ทาง 4b (LMS ควบคุมเต็ม):** ทำ **native video content type** ใน iLearn (ไม่ใช่ SCORM) ที่ player รายงาน played-ranges/percent กลับมา → LMS enforce ได้เต็มและพิสูจน์ได้
  - **ข้อเสีย:** ฟีเจอร์ใหญ่มาก — content type ใหม่, player ใหม่, storage/streaming (โยงกับ [PLAN-076](PLAN-076-large-scorm-file-support-assessment.md)), tracking schema ใหม่ ทั้งหมดนอกเหนือ SCORM
  - **Effort:** L (โครงการแยก)

## 4. ข้อเสนอของ planner (คร่าว ๆ — รอ decision)

- ถ้าเป้าหมายคือ **"กันเนื้อหาที่ mark completed ทันที / บังคับให้ใช้เวลาขั้นต่ำ"** → **Option 1 (+2 ถ้าต้องกันโกง)** ตอบโจทย์ คุ้มค่าที่สุด เป็น LMS-level จริง
- ถ้าต้องการ **"พิสูจน์ว่าดูวิดีโอครบจริง"** → LMS ล้วนทำไม่ได้ ต้อง **4a (author SCORM)** หรือลงทุน **4b (native video)** — แนะนำ 4a ก่อนเพราะไม่ต้องพัฒนา
- ทาง **pragmatic** ที่นิยม: **Option 1 + 4a** — package คุมวิดีโอ, LMS คุมเวลาขั้นต่ำเป็นตาข่ายกันพลาดอีกชั้น

## 5. Decision points (ผู้ใช้)

1. **เป้าหมายจริงคืออะไร?** — (ก) กัน "completed ทันที" / บังคับเวลาขั้นต่ำ  หรือ (ข) พิสูจน์ว่าดูวิดีโอครบจริง ๆ ? (ตอบข้อนี้ชี้ขาดระหว่าง Option 1–2 กับ Option 4)
2. **ระดับการตั้งค่า** — อยากตั้ง minimum time ที่ระดับไหน: ต่อ content item, ต่อ course, หรือ global default?
3. **ต้องกันการโกง `session_time` จาก client ไหม?** — ถ้าใช่ ต้องรวม Option 2 (server-side time)
4. **ยอมรับ proxy เชิงเวลาได้ไหม** ว่า "อยู่กับเนื้อหาครบเวลา" ≈ "เรียนแล้ว" (รู้ว่าไม่ใช่ proof ว่าดูวิดีโอทุกวินาที)?

## 6. ถ้าตัดสินใจทำ — งานที่ต้องมีในแผน implement

- [ ] เพิ่ม config completion rule (field + migration + admin UI) — ระดับตาม decision #2
- [ ] ใส่ gate ใน commit pipeline โดย**ไม่แตะ security/tracking เดิม** — ตำแหน่งที่เหมาะ: ก่อน resolve status เป็น passed ใน `MapRuntimeCommitToProgress` และ/หรือใน `UpdateEnrollmentRollupAsync`
- [ ] ทดสอบ backward-compat: enrollment/log เก่าที่ completed ไปแล้วต้องไม่ถูก downgrade ย้อนหลัง
- [ ] เคส reset ([reset-progress](../iLearn.API/Controllers/LearningLogsController.cs:151)) ต้องล้าง time สะสมด้วย (ปัจจุบันล้าง `TotalTimeSpent` แล้ว)
- [ ] (ถ้า Option 2) กำหนด commit cadence/heartbeat ฝั่ง player + idle handling

## Constraints (สำหรับแผน implement ในอนาคต)

- ❌ ห้ามแตะ security ของ commit endpoint (`TryResolveTrustedLearnerLearnerCode` / learner proxy) — เป็น trust boundary
- ❌ ห้ามทำให้ enrollment ที่ completed อยู่แล้วกลับไป incomplete โดยไม่ตั้งใจ (rollup ต้อง idempotent)
- ⚠️ อย่าโฆษณาเกินจริงว่า "บังคับดูวิดีโอครบ" ถ้าใช้แค่ time-gate — มันคือ minimum-time enforcement (proxy) ไม่ใช่ video-completion proof

## หมายเหตุ

เอกสารนี้เป็น **assessment** ต่อเนื่องจากคำถามผู้ใช้ — ยังไม่ใช่แผน implement และยังไม่มอบ implementer จนกว่าผู้ใช้จะตอบ §5 (โดยเฉพาะ decision #1: เป้าหมายเป็น "เวลาขั้นต่ำ" หรือ "พิสูจน์วิดีโอ")
