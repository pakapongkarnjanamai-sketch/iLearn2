# PLAN-116: ยกเลิกคลิก filter บน chart (ต่อจาก PLAN-114) + โชว์เลขลำดับหน้าชื่อหมวดใน learner sidebar

- **Status:** READY
- **Assigned:** GitHub Copilot (React charts + learner view — งานเล็ก 2 ส่วน)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้สั่ง 2 ข้อ (2026-07-22): (1) PLAN-114 คงการคลิก filter ไว้ — ตอนนี้ให้**ยกเลิกการคลิก filter** บน chart (2) หน้า learner `MyLearning` ให้แสดง **ลำดับ** ของ Categories ในกล่องหมวดหมู่ — นี่คือ option §4 ของ PLAN-111 ที่ผู้ใช้ยืนยันแล้ว
- **หมายเหตุ:** ข้อ 2 ต่อยอด PLAN-113 (`categorySortOrder` ถูกส่งถึง client แล้ว — อยู่บน QA แต่ **ยังไม่ขึ้น PROD**) ⇒ deploy งานนี้ขึ้น PROD ต้องพา PLAN-113 ไปด้วย (อยู่ HEAD เดียวกันอยู่แล้ว)

---

## วินิจฉัย (ยืนยันจากโค้ด)

- **จุดคลิกมี 2 จุดเท่านั้น** ใน `AssignmentReportCharts.tsx`: `Pie.onClick` (`StatusDonut` ~54-58) และ `Bar.onClick` (`CourseCompletionBars` ~149-153) — legend เป็น `<li>` ธรรมดาไม่ clickable
- Callers: `AssignmentDetailPage.tsx` (donut — `onSelectStatus` สลับ tab+filter จาก PLAN-112) และ `AssignmentReportPage.tsx` (donut + bars — `onSelectStatus`/`onSelectCourse` ตั้ง filter ของตาราง)
- ทั้งสองหน้ามีตัวกรองปกติของตัวเองอยู่แล้ว (`SegmentedToggle` / dropdown) — ผู้ใช้ยังกรองได้เหมือนเดิมโดยไม่ต้องคลิก chart
- **Learner sidebar:** `renderCategorySidebar` (`MyLearning/Index.cshtml` ~1472) render `${cat.name}` — `cat.sortOrder` มีอยู่แล้วใน entry จาก PLAN-113

## Scope

### §1 ยกเลิกคลิก filter (React — `AssignmentReportCharts.tsx` + 2 callers)

1. `StatusDonut`: ตัด prop `onSelectStatus` ออกจาก type + ลบ `onClick`/`cursor="pointer"` ที่ `<Pie>`
2. `CourseCompletionBars`: ตัด prop `onSelectCourse` + ลบ `onClick`/`cursor="pointer"` ที่ `<Bar>`
3. **คง prop `activeStatus`/`activeCourse` + logic dim opacity ไว้** — chart ยังสะท้อน filter ที่เลือกจาก toolbar (ยังมีประโยชน์และไม่ใช่ "การคลิก")
4. Callers: `AssignmentDetailPage.tsx` ลบ `onSelectStatus` (รวม logic สลับ tab), `AssignmentReportPage.tsx` ลบ `onSelectStatus`/`onSelectCourse` — **ห้ามแตะ filter toolbar เดิมของทั้งสองหน้า** (SegmentedToggle/dropdown ยังทำงานปกติ)
5. Tooltip hover ของ chart คงเดิม

### §2 เลขลำดับหน้าชื่อหมวด (learner — `MyLearning/Index.cshtml`)

- `renderCategorySidebar`: แสดงชื่อเป็น `${cat.sortOrder}. ${cat.name}` **เฉพาะเมื่อ `cat.sortOrder > 0`** — ถ้า 0/undefined (deploy skew หรือข้อมูลไม่มีลำดับ) แสดงชื่อเดิมไม่มีเลข
- แถว "ทั้งหมด" ไม่มีเลข
- แสดงเลขเฉพาะ **sidebar หมวดหมู่** — ไม่แตะชื่อหมวดที่โผล่ที่อื่น (badge/หัวข้อ course list) จนกว่าผู้ใช้จะสั่งเพิ่ม
- escape ไม่ต้องกังวลเพิ่ม — sortOrder เป็น number จาก `Number`/int DTO

### นอก Scope (ห้ามทำ)

- ห้ามลบ prop `activeStatus`/`activeCourse` หรือ dim logic
- ห้ามแตะ filter controls เดิมของหน้า detail/report
- ห้ามแตะ backend ใด ๆ (ข้อมูลครบแล้วจาก PLAN-113/114)
- ห้ามใส่เลขใน category name ฝั่ง admin (มีคอลัมน์ ลำดับ แยกอยู่แล้ว)

## Contract ที่เปลี่ยน

ไม่มี API/DB — แต่ **props ของ `StatusDonut`/`CourseCompletionBars` เปลี่ยน (ตัด callback)** — เป็น internal component contract, grep ยืนยันมี caller แค่ 2 ไฟล์ที่แก้ในแผนนี้

## Verification

```powershell
cd iLearn.Admin.React; npm run lint; npm run build
# learner view เป็น cshtml — build iLearn.User
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (QA):
1. Assignment detail + report: คลิก segment donut / bar → **ไม่มีอะไรเกิดขึ้น** (ไม่สลับ tab ไม่ตั้ง filter), cursor ไม่เป็น pointer; hover tooltip ยังแสดง
2. เลือก filter จาก SegmentedToggle/dropdown → chart ยัง dim segment/bar ที่ไม่ active ตามเดิม; ตาราง filter ปกติ
3. learner MyLearning: sidebar แสดง `1. <ชื่อหมวด>`, `2. <ชื่อหมวด>` … ตามลำดับ admin; "ทั้งหมด" ไม่มีเลข; คลิกหมวด/นับ course ปกติ
4. console 0 error ทั้ง 2 แอป

## Deploy note

- แตะ **Admin React + iLearn.User** (ไม่มี API/migration รอบนี้)
- **iLearn.User ขึ้น PROD ต้องพา PLAN-113 (API+User) ไปด้วย** — อยู่ HEAD เดียวกัน deploy ตามปกติแล้วรัน verification ของ 113 ซ้ำบน PROD
- QA → verify → PROD (รอผู้ใช้ยืนยัน)

## Implementer Notes

_(เติมโดย implementer)_
