# PLAN-112: Assignment detail — Overview มี visualization + ย้าย courses ของ learner ไป popup

- **Status:** READY
- **Assigned:** Antigravity Gemini (React ล้วน ไฟล์เดียวเป็นหลัก)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้รีวิวหน้า `/assignments/:id` บน QA แล้วขอ 2 อย่าง: (1) Overview ควรมี visualize เห็นสถานะภาพรวมของ batch (2) แถบ Learners เลิกคอลัมน์ "Assigned Courses & Progress" (expand/collapse ในตาราง) เปลี่ยนเป็น popup
- **อ่าน `iLearn.Admin.React/README.md` (UI Conventions) ก่อนเริ่ม**

---

## สภาพปัจจุบัน (ยืนยันจากโค้ด — `AssignmentDetailPage.tsx`)

- **Overview card** (บรรทัด ~688-719): `FactGrid` ตัวเลขล้วน (Learners / Completed / Completion Rate / Status / dates) — ไม่มี chart
- **ข้อมูลมีครบแล้ว ไม่ต้องแตะ backend:** `AssignmentDashboardDto` ให้ `learners[]` ที่แต่ละ row = learner×course พร้อม `status` (Completed | InProgress | NotStarted | Overdue | Upcoming = `LEARNER_STATUS_KEYS`), `chartData`, `completionRate`, `courses[]` (completedLearners/totalLearners)
- **มี chart component พร้อม reuse:** `AssignmentReportCharts.tsx` export `StatusDonut` (donut สถานะ + center label completion % + legend + **คลิก segment เพื่อ select/toggle status ได้ผ่าน prop `onSelectStatus`**) ใช้ `recharts` + `src/lib/chartTheme.ts` (`STATUS_COLORS`) อยู่แล้วในหน้า Report
- **Learners tab** (บรรทัด ~859-1007): ตาราง 5 คอลัมน์ — คอลัมน์ "Assigned Courses & Progress" มีกลไก expand/collapse ต่อแถว (`expandedCodes` state, ปุ่ม Show/Hide courses, ปุ่ม "Expand all" ใน toolbar บรรทัด ~795-816) ข้างในแสดง course ราย row + `ProgressBar` + `StatusBadge` + **ปุ่ม reset รายคอร์ส (`handleResetLearnerCourse`)**
- Modal pattern ในไฟล์เดียวกันมีอยู่แล้ว 3 ตัว (Extend Due Date / Add Courses / Add Learners) — ทั้งหมด `fixed inset-0 z-50` ตาม z-ladder

## Scope

### §1 Overview — เพิ่ม status visualization (ไม่แตะ backend)

1. ปรับ layout ใน `<Card title="Overview">` เป็น 2 ส่วน responsive (`grid lg:grid-cols-[1fr_auto]` หรือเทียบเท่า): ซ้าย = `FactGrid` เดิมครบทุก Fact, ขวา = donut
2. **Reuse `StatusDonut` จาก `AssignmentReportCharts.tsx`** — ห้ามเขียน chart ใหม่:
   - data = aggregate `assignment.learners` rows ตาม `status` (enrollment-level: 1 row = learner×course) map ด้วย `LEARNER_STATUS_KEYS` + `learnerStatusLabel` — นับตรง ๆ ไม่ต้อง derive
   - `completionRate` = `assignment.completionRate` (ค่าที่ backend คิดแล้ว)
   - **คลิก segment ⇒ สลับไป tab Learners + ตั้ง `learnerStatusFilter` เป็น status นั้น** (`setActiveDetailTab('learners')` + `setLearnerStatusFilter(...)`; คลิกซ้ำ = กลับ 'All' ตาม behavior เดิมของ StatusDonut) — ผูก `activeStatus={learnerStatusFilter}` ให้ donut highlight sync กับ filter ปัจจุบัน
   - batch ที่ไม่มี enrollment → `StatusDonut` มี `EmptyChart` ภายในแล้ว ไม่ต้องกันเอง
3. ขนาด: กว้างฝั่ง donut ~260-300px บน desktop; จอแคบให้ donut ตกลงมาอยู่ใต้ FactGrid (stack)

### §2 Learners tab — ตัดคอลัมน์ courses ออก เปลี่ยนเป็น popup

1. **ตัดออก:** คอลัมน์ `Assigned Courses & Progress` (th + td ทั้งบล็อก ~บรรทัด 873, 912-966), state `expandedCodes` + effect ที่ reset มัน, ปุ่ม "Expand all/Collapse all" ใน toolbar (~795-816), import `ChevronDown`/`ChevronUp` ถ้าไม่เหลือที่ใช้
2. **เพิ่มใน cell Summary** (หรือคอลัมน์ Learner — เลือกที่อ่านง่าย): `Badge` `N courses` เดิม + `AppButton variant="ghost"` `"View courses"` เปิด popup ของ learner นั้น (learner ที่ `courses.length === 0` ไม่ต้องมีปุ่ม แสดง italic "No courses assigned" แทนที่เดิมเคยอยู่คอลัมน์ courses)
3. **Popup ใหม่ (modal z-50 ตาม pattern เดิมในไฟล์):**
   - state ตัวเดียว: `courseModalCode: string | null` — **render เนื้อหาจาก `groupedLearners` ล่าสุดด้วย code** (อย่า snapshot ข้อมูลตอนเปิด เพื่อให้ reset แล้ว reload ข้อมูลใน popup refresh ตาม; ถ้า code หายไปหลัง reload เช่นโดนลบ ให้ปิด popup เอง)
   - header: ชื่อ + code learner; body: รายการ course — layout ราย row เดิมย้ายมาได้เลย (courseTitle/courseCode + `ProgressBar` + `StatusBadge` + `IconButton` reset รายคอร์ส → `handleResetLearnerCourse` เดิม)
   - **ฟังก์ชัน reset รายคอร์สต้องยังใช้ได้จาก popup** — useConfirm ซ้อน modal ได้ (ตรวจ z: ConfirmDialog ใช้ z-60 ทับ z-50 ตาม ladder)
   - footer: ปุ่ม Close (`AppButton ghost`)
4. **คงเดิมห้ามพัง:** search + `SegmentedToggle` status filter (filter logic อ่าน `l.courses.some(...)` — ไม่เกี่ยวกับคอลัมน์ที่ตัด), bulk select/reset/remove, per-learner reset/remove ใน Actions, Load more/`DETAIL_TABLE_CHUNK_SIZE`, ลิงก์โปรไฟล์ learner

### นอก Scope (ห้ามทำ)

- ห้ามแตะ backend/`AssignmentDashboardDto` — ข้อมูลครบแล้ว
- ห้ามแตะหน้า Report (`AssignmentReportPage`/`AssignmentReportCharts`) นอกจาก **import** — ถ้า `StatusDonut` ต้อง generalize เล็กน้อย (เช่น prop optional) แก้แบบ additive ห้ามเปลี่ยน behavior หน้า Report
- ห้ามเพิ่ม chart library อื่น (recharts เท่านั้น) / ห้าม hand-roll pill/ปุ่ม (ใช้ Badge/AppButton/IconButton)
- ห้ามแตะ dead code อื่นในไฟล์นอกเหนือจากที่ §2 สั่งตัด

## Contract ที่เปลี่ยน

ไม่มี — React display เท่านั้น, ไม่มี API/DB change

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Manual (QA — ใช้ AS-20260713-002 /assignments/288 ที่ผู้ใช้เปิดอยู่ได้):
1. Overview มี donut สถานะ + completion % ตรงกับ Fact เดิม; จอแคบ donut stack ใต้ facts ไม่ล้น
2. คลิก segment donut → เด้งไป tab Learners + filter ตรง status; คลิกซ้ำ → filter กลับ All; เลือก filter จาก SegmentedToggle → donut highlight ตาม
3. ตาราง Learners ไม่มีคอลัมน์ Assigned Courses & Progress แล้ว; ปุ่ม View courses เปิด popup รายการ course ครบ (ชื่อ/code/progress/status)
4. กด reset รายคอร์สใน popup → confirm (z ทับ popup ถูกต้อง) → toast + ข้อมูลใน popup refresh; ปิด popup ตาราง summary อัปเดต
5. bulk select / reset / remove / search / Load more ทำงานเหมือนเดิม
6. console 0 error

## Implementer Notes

_(เติมโดย implementer)_
