# PLAN-153 — Learners tab: ทำให้ป้าย roll-up / ตัวกรอง / โดนัท พูดหน่วยเดียวกัน

- **Status:** VERIFIED
- **Assigned:** Antigravity (Gemini) — implementer *(ทำจริงโดย GitHub Copilot)*
- **Reviewer:** Claude Code
- **Author:** Claude Code (planner)
- **Priority:** Medium (UI correctness ต่อจาก PLAN-152 — ไม่กระทบข้อมูล/backend)
- **สร้างเมื่อ:** 2026-07-27
- **ที่มา:** Finding 1 จากการรีวิว [PLAN-152](./PLAN-152-learner-row-rollup-status-minimal-summary.md)

---

## Problem

PLAN-152 แก้ป้ายสถานะระดับแถวให้เป็น **roll-up ระดับคน** (5 สถานะ) สำเร็จแล้ว — เคสที่ผู้ใช้แจ้ง (โดนัทบอก Not Started แต่ทุกแถวขึ้น In Progress) หายไปจริง

แต่ในหน้าเดียวกันยังมี **2 หน่วยปนกันอยู่**:

| องค์ประกอบ | หน่วย | โค้ด |
|---|---|---|
| ป้ายสถานะในแถว | **ต่อคน** (roll-up) | `deriveLearnerRollupStatus` [:120](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L120) |
| ตัวกรองสถานะ | **ต่อคอร์ส** (match คอร์สใดคอร์สหนึ่ง) | `l.courses.some(c => c.status === filter)` [:241](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L241) |
| โดนัท | **ต่อคอร์ส** (นับ learner×course rows) | `buildStatusData(assignment.learners)` [:823](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L823) |

**เคสพิสูจน์** — learner ที่มีคอร์ส `[Completed, NotStarted]`:
- ป้ายแถวขึ้น `In Progress`
- โดนัทช่อง `In Progress` = **0**
- กดกรอง `In Progress` → **แถวนั้นหายไป** (เพราะไม่มีคอร์สไหน status = InProgress)

⇒ ผู้ใช้เห็นป้าย `In Progress` กับตา แต่กรอง `In Progress` แล้วหาไม่เจอ

---

## Decision — เคาะแล้ว: **ตัวเลือก A** (ผู้ใช้ยืนยัน 2026-07-27)

**ทุกอย่างในแท็บ Learners นับเป็น "ต่อคน"** — โดนัท + ตัวกรอง ใช้ roll-up ตัวเดียวกับป้ายในแถว

ผลข้างเคียงที่ผู้ใช้รับทราบและยอมรับแล้ว: **ตัวเลขในโดนัทจะเปลี่ยนจากเดิม** (เคสในภาพ `Not Started 6` → `Not Started 3`) เพราะเลิกนับ learner×course แล้วเปลี่ยนมานับหัวคน

*(ตัวเลือกที่ตัดทิ้ง: B = ถอยป้าย roll-up ออกไปโชว์ breakdown รายคอร์ส — เสียของที่เพิ่งทำใน PLAN-152; C = กรองด้วย roll-up แต่โดนัทคงเป็นต่อคอร์ส — ยังขัดกันเอง)*

---

## ⚠️ สิ่งที่ต้องรู้ก่อนลงมือ (planner สำรวจมาให้แล้ว — อย่าข้าม)

ในระบบมี roll-up **3 ชุดที่ไม่ตรงกัน** อยู่แล้ว ถ้าแก้เฉพาะวงแหวนโดนัทจะสร้างบั๊กแบบเดิมซ้ำ:

| # | ตัวไหน | หน่วย | สถานะ | ใครใช้ |
|---|---|---|---|---|
| 1 | `deriveLearnerRollupStatus` | ต่อคน | **5 สถานะ** | ป้ายในแถว (PLAN-152) → **แผนนี้จะขยายให้โดนัท+ตัวกรองใช้ด้วย** |
| 2 | `completionRate` ([AssignmentService.cs:1011](../../iLearn.Application/Services/AssignmentService.cs#L1011)) | **ต่อ enrollment** (`completedEnrollments / totalEnrollments`) | % | **เลขกลางโดนัท** ([AssignmentReportCharts.tsx:65](../../iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx#L65)) |
| 3 | `ChartData` ([AssignmentService.cs:1097-1100](../../iLearn.Application/Services/AssignmentService.cs#L1097)) | ต่อคน | **3 สถานะ** (ไม่มี Overdue/Upcoming) | หน้า `AssignmentReportPage.tsx:338,353-354` — **คนละหน้า ห้ามแตะในแผนนี้** |

**กับดักหลัก:** ถ้าเปลี่ยนแค่วงแหวนให้เป็น "ต่อคน" แต่ปล่อยเลขกลาง (`completionRate`) ไว้เป็น "ต่อ enrollment" → **วิดเจ็ตเดียวมีสองหน่วย** เช่น 3 คน × 2 คอร์ส จบไป 3 จาก 6 enrollment → ตรงกลางขึ้น `50%` แต่วงแหวนขึ้น `Completed 0 · In Progress 3` = บั๊กประเภทเดียวกับที่แผนนี้กำลังแก้ ⇒ **ข้อ 4 ใน Scope บังคับทำ ไม่ใช่ optional**

---

## Scope (ถ้าเลือก A — frontend-only, ไฟล์เดียว + helper)

`iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`

1. **ย้าย `deriveLearnerRollupStatus` ออกมาใช้ร่วมกัน** — ปัจจุบันเป็น local const ในไฟล์เดียว ให้ export จากไฟล์นี้ (หรือย้ายไป `AssignmentReportCharts.tsx` ที่มี `buildStatusData` อยู่แล้ว) เพื่อให้โดนัท/ตัวกรองเรียกตัวเดียวกัน — **ห้าม copy logic ไปวางซ้ำ** (เป็นต้นเหตุของบั๊กนี้ตั้งแต่แรก)
2. **แก้ตัวกรอง** [:241](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L241):
   `!l.courses.some(c => c.status === learnerStatusFilter)` → `deriveLearnerRollupStatus(l.courses) !== learnerStatusFilter`
3. **แก้แหล่งข้อมูลโดนัท** [:823](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L823):
   ส่ง roll-up ต่อคนเข้า `buildStatusData` แทน rows ดิบ เช่น
   `buildStatusData(groupedLearners.map(l => ({ status: deriveLearnerRollupStatus(l.courses) ?? 'NotStarted' })))`
   — ตรวจให้แน่ใจว่า learner ที่ไม่มีคอร์สเลย (roll-up = `null`) ถูกจัดการชัดเจน (ไม่นับ หรือ นับเป็น NotStarted — เลือกแล้วเขียนคอมเมนต์กำกับ)
4. **เลขกลางโดนัทต้องเปลี่ยนเป็นต่อคนด้วย (บังคับ)** — เลิกส่ง `assignment.completionRate` จาก backend เข้า `StatusDonut` ([:824](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L824)) แล้วคำนวณฝั่ง React จาก roll-up ชุดเดียวกัน:

   ```ts
   // % ของ "คน" ที่จบครบทุกคอร์ส — ต้องเป็นหน่วยเดียวกับวงแหวน (ต่อคน) ไม่ใช่ completionRate ของ backend ที่นับต่อ enrollment
   const learnerCompletionRate = useMemo(() => {
     const withCourses = groupedLearners.filter(l => l.courses.length > 0)
     if (withCourses.length === 0) return 0
     const done = withCourses.filter(l => deriveLearnerRollupStatus(l.courses) === 'Completed').length
     return Math.round((done / withCourses.length) * 100)   // สเกล 0-100 เท่าเดิม (formatPercent คาดหวังแบบนี้)
   }, [groupedLearners])
   ```
   - **ห้ามแก้ `AssignmentService.CompletionRate` ฝั่ง backend** — ค่านั้นมีหน้าอื่นใช้อยู่ (ดูตาราง #3 ด้านบน) เปลี่ยนแล้วพังที่อื่น
   - `deriveAssignmentStatus` ([:111](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L111)) ยังใช้ `a.completionRate` ของ backend อยู่ — **ปล่อยไว้ตามเดิม** (เป็นสถานะของ assignment ทั้งก้อน คนละเรื่องกับโดนัท) แค่อย่าเผลอลบ field ทิ้ง

## Out of scope (ห้ามแตะ)
- backend / `AssignmentStatusKeys` / DTO / API contract ทุกตัว — ข้อมูลครบฝั่ง frontend แล้ว
- `ChartData` (roll-up 3 สถานะฝั่ง backend) และหน้า `AssignmentReportPage.tsx` ที่ใช้มันอยู่ — **คนละหน้า** ถ้าจะทำให้ตรงกันค่อยเป็นแผนแยก (ดู Known gap)
- ป้ายรายคอร์สใน modal View courses ([:1449](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L1449)) — เป็นระดับคอร์ส **ถูกต้องแล้ว** ต้องคงไว้
- หน้า report อื่น ๆ ที่ใช้ status ต่อคอร์ส

## Known gap (รับรู้ไว้ ไม่ต้องแก้ในแผนนี้)
หลังแผนนี้เสร็จ หน้า `AssignmentDetailPage` (5 สถานะ ต่อคน) กับ `AssignmentReportPage` (`ChartData` 3 สถานะ ต่อคน) จะยัง**ให้ตัวเลขไม่ตรงกันสำหรับ assignment เดียวกัน** — เช่นคนที่เลยกำหนดจะนับเป็น `In Progress` ในหน้า report แต่เป็น `Overdue` ในหน้า detail. ถ้าผู้ใช้เจอแล้วติดใจ ค่อยเปิดแผนใหม่ย้าย `ChartData` ไปใช้ 5 สถานะทั้งระบบ — **อย่าลากเข้ามาทำในแผนนี้**

## Follow-up ที่รวมมาได้ในรอบเดียว (จาก Reviewer Notes PLAN-152)
- **Finding 2:** เปลี่ยน return type ของ `deriveLearnerRollupStatus` เป็น `LearnerStatusKey | null` (ตอนนี้เป็น `string | null` — พิมพ์ key ผิดจะหลุด compile แล้วไปโผล่เป็นข้อความดิบบน badge)
- **Finding 3:** ลบ label `ASSIGNMENT_LABELS.courseCount` ที่ไม่มีผู้ใช้แล้ว ([labels.ts:890](../../iLearn.Admin.React/src/lib/labels.ts#L890)) — ตรวจด้วย grep ก่อนลบ

## Acceptance criteria
1. learner ที่มีคอร์ส `[Completed, NotStarted]` → ป้ายแถว `In Progress` **และ** กดกรอง `In Progress` แล้วต้องเจอแถวนั้น
2. ผลรวมทุกช่องในโดนัท = จำนวน learner ทั้งหมดในแท็บ (ไม่ใช่จำนวน learner×course)
3. กดทุกช่อง/ทุกปุ่มกรองแล้ว จำนวนแถวที่ได้ = ตัวเลขในช่องโดนัทของสถานะนั้นเสมอ
4. เคสเดิมจาก PLAN-152 (3 คน × 2 คอร์ส ยังไม่เริ่ม) ยังขึ้น `Not Started` ทุกแถวเหมือนเดิม — โดนัทเปลี่ยนจาก `6` เป็น `3` ตามหน่วยใหม่
5. learner ที่ไม่มีคอร์สเลย ไม่ทำให้ยอดรวมเพี้ยนหรือ crash
6. **เลขกลางโดนัทเป็นหน่วยเดียวกับวงแหวน** — เคส 3 คน × 2 คอร์ส ที่คนหนึ่งจบครบ อีกสองคนจบคนละคอร์ส: ตรงกลางต้องขึ้น `33%` (1 ใน 3 คน) ไม่ใช่ `50%` (3 ใน 6 enrollment)
7. ไม่มี logic roll-up ถูก copy ไปวางซ้ำ — `deriveLearnerRollupStatus` ต้องมีนิยามเดียวในโค้ดเบส (ตรวจด้วย grep)

## Verification
```powershell
# จากโฟลเดอร์ iLearn.Admin.React
npm run lint
npm run build
```
บวก manual smoke บนหน้า `/assignments/:id` แท็บ Learners — ต้องมี assignment ที่มีสถานะผสมจริง (ถ้าไม่มีใน QA ให้ระบุใน Implementer Notes ว่าทดสอบเคสผสมไม่ได้)

## Implementer Notes
- Implemented in `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx` (frontend-only):
   - `deriveLearnerRollupStatus` return type tightened to `LearnerStatusKey | null`.
   - Learners-tab status filter now matches per-learner roll-up status (same unit as row badge).
   - Donut dataset now built from per-learner roll-up statuses (not raw learner×course rows).
   - Learners with zero courses are normalized to `NotStarted` for donut/filter counting so donut totals stay aligned with learner-row totals.
   - Donut center percentage now uses per-learner completion (`Completed learners / learners with courses`) instead of backend `completionRate` (per-enrollment).
- Follow-up cleanup from PLAN-152 reviewer notes:
   - Removed unused `ASSIGNMENT_LABELS.courseCount` from `iLearn.Admin.React/src/lib/labels.ts`.
- Verification:
   - `npm run lint` ✅
   - `npm run build` ✅
- Manual smoke:
   - Not executed in this run (no live assignment interaction performed from this environment).

---

## Reviewer Notes (Claude Code — 2026-07-27)

**ผลรีวิว: VERIFIED** — ตรงตาม Scope ตัวเลือก A ครบ และเก็บ Finding 2+3 ของ PLAN-152 ให้ด้วย

**หมายเหตุกระบวนการ:** แผน Assigned ให้ Antigravity (Gemini) แต่ผู้ที่ implement จริงคือ **GitHub Copilot** — งานถูกต้องจึงไม่ตีกลับ แต่ครั้งหน้าถ้ารับงานข้ามคนที่ assign ให้จดเหตุผลใน Implementer Notes ด้วย (กัน agent สองตัวหยิบแผนเดียวกันพร้อมกัน)

ตรวจแล้วผ่าน:
1. `npm run lint` ✓ · `npm run build` ✓ (รันเองอิสระ; bundle hash เปลี่ยนเป็น `index-DkBMPY-5.js` = โค้ดใหม่เข้าจริง)
2. **Smoke test ตรรกะจริง** — ดึงฟังก์ชัน `deriveLearnerRollupStatus` ออกจากไฟล์ source มารันตรง ๆ (ไม่ใช่เขียน mock เลียนแบบ) + จำลอง donut/filter/% ตามที่ไฟล์เขียน: roll-up ถูกทั้ง 7 เคส และ **AC1-AC7 ผ่านหมด**
   - AC1: `[Completed, NotStarted]` → ป้าย `In Progress` **และ** กรอง `In Progress` เจอแถวนั้นแล้ว (Finding 1 ปิดจริง)
   - AC3: จำนวนแถวหลังกรอง = ตัวเลขในโดนัท ครบทั้ง 5 สถานะ
   - AC4: เคสเดิม 3 คน × 2 คอร์ส → `Not Started 3` (เดิม 6) ตามที่ตกลง
   - AC6: เลขกลาง `33%` (ต่อคน) ไม่ใช่ `67%` (ต่อ enrollment) ⇒ วงแหวนกับเลขกลางหน่วยเดียวกันแล้ว
3. wiring ตรวจด้วย regex บนไฟล์จริง: filter ใช้ roll-up ✓ · ไม่เหลือ `courses.some(...)` แบบเดิม ✓ · โดนัทไม่ใช้ `assignment.completionRate` แล้ว ✓ · นิยาม roll-up มีที่เดียว (AC7) ✓
4. ไม่แตะ backend เลย — `AssignmentService.CompletionRate` และ `ChartData` เดิมครบ ตาม Out of scope ✓

### Observation (ไม่ใช่บั๊ก — ไม่ต้องแก้)
ตัวหารของเลขกลาง (`คนที่มีคอร์ส`) กับประชากรของวงแหวน (`คนทั้งหมด`) เป็นคนละชุด ⇒ **ในทางทฤษฎี** ถ้ามี learner ที่ไม่มีคอร์สเลย จะได้ `100%` พร้อมวงแหวนที่มี `Not Started 1` ซึ่งขัดกันเอง

แต่ตรวจ backend แล้ว **เคสนี้เกิดไม่ได้จริง**: `learners` สร้างจาก `learnerRows` ทุกแถว ([AssignmentService.cs:1046](../../iLearn.Application/Services/AssignmentService.cs#L1046)) และ `CourseCode` มี fallback `"-"` ซึ่ง truthy เสมอ ⇒ เงื่อนไข `if (l.courseCode || l.courseTitle)` ([:227](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L227)) ผ่านทุกแถว ⇒ ทุก learner มีคอร์ส ≥ 1 เสมอ

⇒ โค้ดจัดการ zero-course (`?? 'NotStarted'`, `withCourses`) เป็น defensive ล้วน **เก็บไว้ได้ ไม่ต้องแก้** — บันทึกไว้เผื่อวันหลัง backend เปลี่ยนให้ส่ง learner ที่ไม่มี enrollment มา ต้องกลับมาทำตัวหารสองฝั่งให้ตรงกันก่อน

### หนี้ที่ยังเหลือ
- **ยังไม่ได้ manual smoke บนของจริง** (API ไม่ได้รันใน session นี้) — ควรกดดูหน้า `/assignments/:id` แท็บ Learners บน QA หลัง deploy โดยเลือก assignment ที่มีสถานะผสม แล้วเช็คว่ากดกรองแต่ละสถานะได้แถวตรงกับเลขในโดนัท
- **ยังไม่ deploy** QA/PROD
