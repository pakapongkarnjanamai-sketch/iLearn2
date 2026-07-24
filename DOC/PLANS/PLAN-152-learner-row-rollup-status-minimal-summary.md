# PLAN-152 — Learner row: roll-up status 5 สถานะ + ย่อ Summary ให้มินิมอล

- **Status:** DONE
- **Assigned:** Antigravity (Gemini) — implementer
- **Reviewer:** Claude Code
- **Author:** Claude Code (planner)
- **Priority:** Medium (แก้ข้อมูลขัดกันเองในหน้า Assignment Detail — UI correctness)
- **สร้างเมื่อ:** 2026-07-24
- **ที่มา:** ผู้ใช้สังเกตว่าในหน้า Assignment Detail แท็บ Learners โดนัทบอก `Not Started 6 (100%)` แต่ทุกแถวขึ้นป้าย `In Progress` ทั้งหมด → ขัดกันเอง สับสน. ต้องการให้ป้ายระดับแถวสะท้อนสถานะจริง และให้คอลัมน์ Summary มินิมอล (ตัด `2 course(s)` ทิ้ง, ย้าย `View courses` ไป Actions)

---

## Root cause / gap

ป้ายสถานะระดับ **แถว (per-learner)** ในตาราง Learners ถูก hardcode เป็น 2 ค่าเท่านั้น — [AssignmentDetailPage.tsx:987-988](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L987):

```tsx
<StatusBadge size="xxs" tone={allCompleted ? 'success' : 'neutral'}>
  {learnerStatusLabel(allCompleted ? 'Completed' : 'InProgress')}
</StatusBadge>
```

= "ถ้ายังไม่จบทุกคอร์ส → In Progress" โดยไม่สนสถานะจริง ทั้งที่:
- **สถานะจริงมี 5 ค่า** (`Completed | InProgress | NotStarted | Overdue | Upcoming`) คำนวณ **รายคอร์ส** ฝั่ง backend ที่ [AssignmentStatusKeys.GetScheduledLearnerStatus](../../iLearn.Application/Common/AssignmentStatusKeys.cs#L58) และส่งมาแล้วใน `l.courses[].status`
- โดนัท + ปุ่มกรอง ([AssignmentReportCharts.buildStatusData](../../iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx#L156), filter ที่ [:229](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L229)) ใช้ 5 สถานะจริง → เลยไม่ตรงกับป้ายแถว

**ข้อมูลครบฝั่ง React แล้ว** (`groupedLearners[].courses[].status` — ยืนยันที่ grouping [:206-214](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L206)) ⇒ ทำ roll-up ฝั่ง frontend ได้เลย **ไม่ต้องแก้ backend / DTO / API contract**

---

## Scope (frontend-only, ไฟล์เดียว: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`)

### 1) เพิ่ม helper roll-up (วางใกล้ ๆ `deriveAssignmentStatus` ~[:110](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L110))

รวมสถานะรายคอร์สของ learner เป็น 1 ป้าย ตาม **ลำดับความสำคัญ** (urgent-first ให้สอดคล้อง semantic โดนัท):

```ts
// รวมสถานะรายคอร์สเป็นสถานะรวมของ learner (mirror 5 keys ของ AssignmentStatusKeys.Learner)
// ลำดับ: จบครบ → มีเกินกำหนด → เริ่มแล้ว/บางส่วน → ยังไม่เริ่ม → ยังไม่ถึงกำหนด
const deriveLearnerRollupStatus = (courses: Array<{ status: string }>): string | null => {
  if (courses.length === 0) return null
  const s = courses.map(c => c.status)
  if (s.every(x => x === 'Completed')) return 'Completed'
  if (s.some(x => x === 'Overdue')) return 'Overdue'
  if (s.some(x => x === 'InProgress' || x === 'Completed')) return 'InProgress' // มี progress/จบบางส่วน
  if (s.some(x => x === 'NotStarted')) return 'NotStarted'
  return 'Upcoming' // เหลือกรณีทุกคอร์ส Upcoming
}
```

หมายเหตุ: เงื่อนไข `x === 'Completed'` ในบรรทัด InProgress คือดัก **จบบางส่วน** (เช่น `[Completed, NotStarted]`) ให้เป็น In Progress อย่างถูกต้อง

### 2) เปลี่ยนป้ายระดับแถว ([:987-989](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L987))

ให้ใช้ค่า roll-up จริง และ **ปล่อยให้ `StatusBadge` derive tone เองจาก label** (StatusBadge เรียก `statusTone()` เมื่อไม่ส่ง `tone` — ดู [StatusBadge.tsx:18](../../iLearn.Admin.React/src/components/ui/StatusBadge.tsx#L18)) ⇒ ได้สีถูกทุกสถานะ (Overdue=danger, NotStarted/Upcoming=warning, InProgress=info, Completed=success):

```tsx
{(() => {
  const rollup = deriveLearnerRollupStatus(l.courses)
  return rollup ? (
    <StatusBadge size="xxs">{learnerStatusLabel(rollup)}</StatusBadge>
  ) : null
})()}
```

- ลบ prop `tone={allCompleted ? 'success' : 'neutral'}` ทิ้ง (ให้ derive เอง)
- คง `0/2 Completed` ([:984-986](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L984)) ไว้เหมือนเดิม — บอกจำนวนครบอยู่แล้ว
- `allCompleted` ยังใช้ที่อื่นไหม? หลังแก้ ถ้าไม่มีที่ใช้แล้วให้ลบตัวแปร (กัน lint no-unused). `completedCount`/`totalCount` ยังใช้อยู่ (บรรทัด `0/{totalCount} Completed` + เงื่อนไข `totalCount === 0`)

### 3) ตัด `2 course(s)` badge ทิ้ง + ย้าย `View courses` ไป Actions

**คอลัมน์ Summary** ([:991-1004](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L991)) — เดิม:
```tsx
{totalCount === 0 ? (
  <span ...>{t(ASSIGNMENT_LABELS.noCoursesAssigned)}</span>
) : (
  <div className="flex items-center gap-2">
    <Badge ...>{tf(ASSIGNMENT_LABELS.courseCount, totalCount)}</Badge>  {/* ← ลบ */}
    <AppButton ... onClick={() => setCourseModalCode(l.learnerCode)}>   {/* ← ย้ายไป Actions */}
      {t(ASSIGNMENT_LABELS.viewCourses)}
    </AppButton>
  </div>
)}
```
แก้เป็น: เหลือแค่ข้อความ `No courses assigned` ตอนไม่มีคอร์ส, กรณีมีคอร์สให้ **ไม่ render อะไรเพิ่ม** ในบล็อกนี้ (Summary เหลือแค่ `0/2 Completed [badge]`):
```tsx
{totalCount === 0 && (
  <span className="text-slate-400 text-xs italic">{t(ASSIGNMENT_LABELS.noCoursesAssigned)}</span>
)}
```

**คอลัมน์ Actions** ([:1007-1024](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L1007)) — เพิ่มปุ่ม View courses เป็น `IconButton` ตัวแรก (ให้เข้าชุด icon-button convention ของ Actions; แสดงเฉพาะเมื่อมีคอร์ส):
```tsx
<div className="inline-flex items-center gap-1.5">
  {totalCount > 0 && (
    <IconButton
      onClick={() => setCourseModalCode(l.learnerCode)}
      icon={Eye}
      tone="neutral"
      size="sm"
      title={t(ASSIGNMENT_LABELS.viewCourses)}
    />
  )}
  <IconButton onClick={() => handleResetLearner(...)} icon={RotateCcw} tone="primary" size="sm" title={...} />
  <IconButton onClick={() => handleRemoveLearner(...)} icon={Trash2} tone="danger" size="sm" title={...} />
</div>
```
- `Eye` import จาก `lucide-react` (เพิ่มใน import block บนสุด ~[:1-9](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L1) ที่มี `RotateCcw, Trash2` อยู่แล้ว). ถ้าเห็นว่า `BookOpen` สื่อความ "คอร์ส" กว่า ใช้ได้เช่นกัน — เลือกตัวใดตัวหนึ่ง
- `IconButton` prop `title` บังคับ (UI convention) — ใช้ `ASSIGNMENT_LABELS.viewCourses` เดิม
- ตรวจ tone ที่ `IconButton` รองรับ (`neutral` มีไหม) — ถ้าไม่มีให้ใช้ `primary`; อย่าเดา ดู `IconButton.tsx`

**ห้ามแตะ:** logic โดนัท/filter (`buildStatusData`, `learnerStatusFilter`), modal `View courses` เดิม (`courseModalCode`/`modalLearner` — แค่เปลี่ยนตัว trigger), backend/DTO ใด ๆ, ป้ายรายคอร์สใน modal ([:1449](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx#L1449) ที่ใช้ `c.status` อยู่แล้ว — ถูกต้องแล้ว)

---

## Out of scope
- ไม่แก้ backend / `AssignmentStatusKeys` / DTO / API contract (ข้อมูลครบฝั่ง frontend แล้ว)
- ไม่แตะ logic โดนัท / ปุ่มกรองสถานะ (ถูกต้องอยู่แล้ว)
- ไม่เพิ่ม tooltip แจกแจงสถานะรายคอร์สบนป้าย roll-up (ถ้าอยากได้ค่อยทำแยก — ดู Follow-up)

## Follow-up (optional)
- ป้าย roll-up ใส่ `title=` สรุปรายคอร์ส (เช่น "1 In Progress · 1 Overdue") ให้ hover เห็นที่มา — เพิ่ม UX แต่ไม่จำเป็นในรอบนี้

---

## Verification

รันจากโฟลเดอร์ `iLearn.Admin.React`:
```powershell
npm run lint
npm run build   # tsc -b && vite build
```
เกณฑ์ผ่าน:
1. Build/lint 0 error (ระวัง unused: `allCompleted` ถ้าไม่ได้ใช้แล้ว, และ label `ASSIGNMENT_LABELS.courseCount` จะไม่ถูกอ้างถึง — ปล่อยไว้ใน labels.ts ได้ ไม่ใช่ error)
2. **ตรงกับเคสในภาพ**: assignment 3 คน × 2 คอร์ส, ทุกคอร์ส progress 0 ในช่วงเปิด → ทุกแถวต้องขึ้น **`Not Started`** (tone warning) ตรงกับโดนัท `Not Started 6 (100%)` — ไม่ใช่ `In Progress` อีกต่อไป
3. Summary แต่ละแถวเหลือแค่ `0/2 Completed` + ป้าย roll-up — ไม่มี `2 course(s)` badge แล้ว
4. Actions มีปุ่ม View courses (icon) นำหน้า reset/delete, กดแล้วเปิด modal รายคอร์สได้เหมือนเดิม; แถวที่ไม่มีคอร์ส (`0` courses) ไม่มีปุ่ม View courses และ Summary โชว์ "No courses assigned"
5. สุ่มตรวจ roll-up หลายเคส (ถ้ามีข้อมูลจริง): จบครบ→Completed(เขียว), มีคอร์สเลยกำหนด→Overdue(แดง), จบบางส่วน→In Progress(น้ำเงิน), ยังไม่ถึง start ทุกคอร์ส→Upcoming(เหลือง)

## Definition of Done
- [x] เพิ่ม `deriveLearnerRollupStatus` + ป้ายแถวใช้ค่า roll-up (derive tone เอง)
- [x] ตัด `2 course(s)` badge; ย้าย `View courses` → Actions (IconButton, แสดงเมื่อมีคอร์ส)
- [x] `npm run lint` + `npm run build` ผ่าน
- [x] ป้ายแถวตรงกับโดนัท/ตัวกรอง (เคสในภาพ = Not Started)
- [x] เปลี่ยน Status ในไฟล์นี้เป็น DONE + เติม Implementer Notes + ลง `DOC/AGENT_LOG.md`

---

## Implementer Notes

- **สิ่งที่ได้ดำเนินการ**:
  1. เพิ่ม helper `deriveLearnerRollupStatus(courses)` ใน `AssignmentDetailPage.tsx` เพื่อคำนวณ rollup status 5 สถานะ (`Completed`, `Overdue`, `InProgress`, `NotStarted`, `Upcoming`) จากสถานะรายคอร์สเรียงลำดับ priority
  2. แก้ไขการแสดงผลป้ายสถานะในตาราง Learners ให้ใช้ `deriveLearnerRollupStatus` ร่วมกับ `learnerStatusLabel` โดยปล่อยให้ `StatusBadge` derive `tone` อัตโนมัติ (`Overdue`=danger/แดง, `NotStarted`/`Upcoming`=warning/ส้ม, `InProgress`=info/น้ำเงิน, `Completed`=success/เขียว)
  3. ลบ `allCompleted` (และ `Badge` import ที่ไม่ใช้อื่นๆ) เพื่อป้องกัน eslint warning
  4. ทำความสะอาดคอลัมน์ Summary โดยถอดป้าย `X course(s)` และป้ายข้อความ `View courses` ออก เหลือเพียง `0/2 Completed` และป้ายสถานะ
  5. ย้ายการดูคอร์สไปไว้ในคอลัมน์ Actions โดยเพิ่ม `IconButton` (ไอคอน `Eye`, `tone="neutral"`, `title={viewCourses}`) นำหน้าปุ่ม Reset และ Delete (แสดงเฉพาะกรณี learner มีคอร์ส)
- **การตรวจสอบ**:
  - `npm run lint` = 0 errors, 0 warnings
  - `npm run build` (tsc -b && vite build) = ผ่านเรียบร้อย
  - `dotnet build iLearn.Tests` + `dotnet test` = Passed (279 passed, 0 failed)
