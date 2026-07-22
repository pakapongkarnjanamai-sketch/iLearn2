# PLAN-124: LearnerGroupListPage folder edit action (เพิ่มปุ่มแก้ไขโฟลเดอร์ในไดเรกทอรี Learner Groups)

- **Status:** DONE — Implement สำเร็จแล้ว (2026-07-22)
- **Assigned:** Antigravity Gemini
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้รีวิวหน้า `/admin-react/learner-groups` (Learner Group Explorer) แล้วพบว่ารายการประเภท `FOLDER` ในตารางมีเพียงปุ่มลบ (`Trash2`) และปุ่มเปิดโฟลเดอร์ (`ArrowUpRight`) แต่ขาดปุ่มแก้ไข (`Edit3`) ต่างจากรายการประเภท `GROUP` ที่มีปุ่มจัดการครบถ้วน
- **อ่าน `iLearn.Admin.React/README.md` ก่อนเริ่ม**

---

## วินิจฉัย

1. **`LearnerGroupListPage.tsx`**: ในคอลัมน์ `Actions` ของ `tableColumns` (L611-649) มีเงื่อนไขเช็ค `item.isFolder`:
   - ถ้าเป็น `FOLDER` (หมวดหมู่/โฟลเดอร์): ปัจจุบันแสดงเฉพาะปุ่มลบ `Trash2` (ถ้าเป็นโฟลเดอร์ว่าง) และปุ่ม `ArrowUpRight` สำหรับเปิดเข้าโฟลเดอร์
   - ขาดปุ่ม `IconButton` สำหรับแก้ไขชื่อ/คำอธิบายโฟลเดอร์
2. **Backend API รองรับอยู่แล้ว**: `PUT api/LearnerGroupCategories/{id}` (`UpdateLearnerGroupCategoryDto`) รองรับการอัปเดต `name`, `description`, `divisionId`, `parentId`

---

## Scope

### §1. เพิ่มปุ่มแก้ไขโฟลเดอร์ (`Edit3`) ในคอลัมน์ Actions ของ `LearnerGroupListPage.tsx`
- ไฟล์: `src/pages/learner-groups/LearnerGroupListPage.tsx`
- เพิ่ม `Edit3` ในการ import จาก `lucide-react`
- เพิ่ม state สำหรับ Edit Folder Modal:
  - `editingFolder`: `CategoryLookup | null`
  - `editFolderName`: `string`
  - `editFolderDesc`: `string`
  - `editFolderDivisionId`: `number | ''`
  - `updatingFolder`: `boolean`
- เพิ่มฟังก์ชัน `handleOpenEditFolder(folder: CategoryLookup)` สำหรับเตรียมค่าตั้งต้นและเปิด modal
- เพิ่มฟังก์ชัน `handleUpdateFolder(event: FormEvent)` ยิง `PUT LearnerGroupCategories/{editingFolder.id}`
- ใน `tableColumns` คอลัมน์ `Actions`:
  - สำหรับ `item.isFolder` เพิ่ม `<IconButton icon={Edit3} tone="primary" size="sm" title="Edit Folder" onClick={() => handleOpenEditFolder(item.original as CategoryLookup)} />` ด้านหน้าปุ่มลบ `Trash2`
- เพิ่ม Edit Folder Modal ใน JSX เพื่อรองรับการแก้ไขชื่อ (Folder Name), คำอธิบาย (Description) และ Division (กรณี SuperAdmin บน root directory)

---

## Verification Plan

### Automated Tests
1. `npm run lint` — ผ่าน 0 errors
2. `npm run build` — ผ่าน 0 errors

### Manual Verification
1. เปิดหน้า `/admin-react/learner-groups`
2. สังเกตแถวของโฟลเดอร์ (ประเภท `FOLDER` เช่น `CAS`) จะต้องมีปุ่มดินสอแก้ไขสีน้ำเงิน (`Edit Folder`) แสดงในคอลัมน์ `ACTIONS`
3. คลิกปุ่ม Edit Folder:
   - ต้องมี Modal ขึ้นมาพร้อมแสดงชื่อและคำอธิบายเดิม
   - ลองเปลี่ยนชื่อโฟลเดอร์แล้วกด Save
   - ตรวจสอบว่าระบบบันทึกสำเร็จ, Modal ปิดลง, และตารางอัปเดตชื่อใหม่ทันที

---

## Implementer Notes

- **`LearnerGroupListPage.tsx`**:
  - เพิ่ม `Edit3` ใน icon imports และเพิ่ม `divisionId?: number | null` ใน `CategoryLookup`
  - เพิ่ม `handleOpenEditFolder` และ `handleUpdateFolder` สำหรับจัดการยิง `PUT api/LearnerGroupCategories/{editingFolder.id}`
  - ใน `tableColumns` เพิ่ม `<IconButton icon={Edit3} tone="primary" size="sm" title="Edit Folder">` สำหรับรายการ `item.isFolder`
  - เพิ่ม Edit Folder Modal ใน JSX เพื่อรองรับการแก้ไขชื่อ, คำอธิบาย และ Division
- **Verification**: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.31s)

