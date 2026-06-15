# PLAN-006: Admin Users — เพิ่มหน้า Detail (`/users/:id`) และฟังก์ชันลบ user

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: commit 1b7b8c0; build/test ผ่าน)
- **Assigned:** Gemini
- **Priority:** High
- **Estimated scope:** 1 ไฟล์ใหม่ (`UserDetailPage.tsx`) + แก้ 2 ไฟล์ (`AdminUsersPage.tsx`, `App.tsx`)

## Problem

ต่อเนื่องจาก PLAN-004 (Users new/edit เป็น wizard แล้ว) — module Admin Users ยังขาด 2 อย่างเมื่อเทียบกับ module มาตรฐานอื่น (Content Library, Learner Groups, Courses):

1. **ไม่มีหน้า Detail** (`/users/:id`) — module อื่นทุกตัวมี detail page เป็นจุดศูนย์กลางก่อนเข้า edit (pattern: เนื้อหาในการ์ด + `ControlsSidebar` ด้านขวา ดู `ContentItemDetailPage.tsx` เป็นตัวอย่าง)
2. **ไม่มีฟังก์ชันลบ user** — ทั้งที่ backend มี endpoint อยู่แล้ว

## Backend contract (มีครบแล้ว ห้ามแก้ backend)

- `DELETE admin/UsersCRUD/Delete` — FormData field `key` = user id (มาจาก `GenericController.Delete`, `iLearn.API/Controllers/Base/GenericController.cs` บรรทัด ~70-78) คืน `Ok()` / `NotFound`
- โหลด user เดี่ยวแบบ enriched (มี fullName/email/division/department/section/position): ใช้ `GET admin/UsersCRUD/Get?filter=[["id","=",<id>]]` แล้วอ่าน `data[0]` — **วิธีเดียวกับที่ `UserEditorPage.tsx` (จาก PLAN-004) ใช้อยู่ ลอก/แชร์ helper ได้เลย**
  - หมายเหตุ: base มี `GET admin/UsersCRUD/Get/{id}` ด้วย แต่คืน entity ดิบ **ไม่ enriched + ไม่มี roles** — อย่าใช้
- Type `AdminUser` / `UserRoleInfo` / `RoleInfo` ถูก export ไว้แล้วจากงาน PLAN-004 — import มาใช้ อย่าประกาศซ้ำ

## Scope (ทำแค่นี้)

### 1. ไฟล์ใหม่ `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`

ตาม pattern `ContentItemDetailPage.tsx` (การ์ด `rounded-lg border border-slate-200 bg-white` + `ControlsSidebar` ขวา):

- **เนื้อหา:** ข้อมูล user — NID, Full Name, Email, Division, Department, Section, Position, Last Login (`formatDateTime` จาก `src/lib/format.ts`), สถานะ Active (`StatusBadge`), รายการ Roles เป็น badge (reuse สไตล์ badge สีจาก grid เดิมใน `AdminUsersPage.tsx`: SuperAdmin = purple, อื่น = indigo)
- **ControlsSidebar** (`backTo="/users"`):
  - `ControlAction` Edit Roles → `/users/${id}/edit`
  - `ControlAction` Delete (variant="danger", icon Trash2) → `useConfirm` (title "Delete Admin User", message ระบุชื่อ/NID, danger) → `DELETE admin/UsersCRUD/Delete` ด้วย FormData `key` → toast success → `navigate('/users')`
- ใช้ `LoadingState` ระหว่างโหลด / `NotFoundState` เมื่อ id ไม่มีจริง
- Breadcrumb: ใช้ `useBreadcrumbs().setLabel(id, user.fullName || user.nid)` เพื่อให้ breadcrumb แสดงชื่อแทน "Details" (pattern เดียวกับ `MasterDataDetailPage.tsx` บรรทัด ~72-74)

### 2. `AdminUsersPage.tsx` — ผูก grid เข้าหน้า detail

- double-click row → navigate `/users/${id}` (เปลี่ยนจากเดิมที่ไป edit — ให้ตรง convention ของ module อื่นที่ dblclick = detail)
- action buttons ในแถว: เพิ่ม icon ดูรายละเอียด (เช่น Eye) → `/users/${id}`; **คง icon Shield → `/users/${id}/edit` ไว้ตามเดิม**
- ห้ามเพิ่มปุ่มลบในแถว grid — การลบให้ทำจากหน้า detail เท่านั้น (ตาม pattern Content Library)

### 3. `App.tsx` — เพิ่ม route ใต้กลุ่ม `{/* Admin Users */}`

```tsx
<Route path="users/:id" element={<RequireRole superAdminOnly><Remount><UserDetailPage /></Remount></RequireRole>} />
```
**ต้องวางหลัง `users/new`** (static ก่อน dynamic — React Router จัด specificity ให้อยู่แล้ว แต่เรียงไว้ให้อ่านง่าย: `users` → `users/new` → `users/:id` → `users/:id/edit`)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend ทุกไฟล์
- ห้ามแก้ `UserEditorPage.tsx` นอกจากกรณีเดียว: ถ้าจะ extract helper โหลด user ร่วมกัน (เช่น `fetchAdminUser(id)`) ให้แยกเป็นไฟล์ utility ใน `src/pages/users/` ได้ แต่ห้ามเปลี่ยนพฤติกรรม editor
- ห้ามเพิ่ม guard กันลบตัวเอง (backend ไม่มี — ถ้าเห็นว่าควรมี จดใน Implementer Notes แล้วข้าม)
- ห้ามแตะ grid columns / search / ปุ่ม Add Admin User

## Acceptance criteria

- [x] `/users/:id` แสดงข้อมูล user ครบ (NID, ชื่อ, email, division, department, section, position, last login, active, roles)
- [x] ปุ่ม Delete ใน ControlsSidebar → confirm dialog → ลบสำเร็จ → toast → กลับ `/users` แล้ว user หายจาก grid
- [x] ปุ่ม Edit Roles ใน detail → ไปหน้า wizard edit ได้
- [x] double-click row ใน grid → detail; icon Shield → edit เหมือนเดิม
- [x] เปิด `/users/999999` → `NotFoundState`
- [x] Breadcrumb แสดง "Admin Users / <ชื่อหรือ NID>" ไม่ใช่ "Details"
- [x] ใช้ `useConfirm` (ห้าม `window.confirm`), `formatDateTime` จาก `src/lib/format.ts` ตามกติกา

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด `/users` → dblclick user → เห็น detail ครบ → Edit Roles ไป wizard ได้ → กลับมา detail → Delete → confirm → กลับ `/users` user หายจริง; เปิด `/users/999999` → NotFoundState

## Implementer Notes

- **สร้างหน้าใหม่:** [UserDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserDetailPage.tsx) เพื่อแสดงข้อมูลรายละเอียดครบถ้วนของ Admin User (NID, ชื่อ, Email, แผนก, ต่ำแหน่ง, วันเวลาล็อคอินล่าสุด, สถานะการใช้งานด้วย StatusBadge, และรายการ Roles ที่มี) พร้อมเชื่อมโยงกับ `ControlsSidebar` ด้านขวา
- **ปุ่มลบแอดมิน:** เพิ่มปุ่มลบในแถบเครื่องมือด้านข้าง ทำงานร่วมกับ `useConfirm` ยืนยันก่อนยิง `DELETE admin/UsersCRUD/Delete` โดยส่ง FormData `key` เป็น ID ไปยังเซิร์ฟเวอร์
- **ปรับการนำทางหน้าหลัก:** ใน [AdminUsersPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx) ได้ปรับปรุง `onRowDblClick` ให้ชี้ไปหน้ารายละเอียด `/users/${id}` แทน และเพิ่มปุ่มรูปตา (`Eye`) สำหรับเปิดหน้ารายละเอียดคู่กับปุ่มโล่ (`Shield`) สำหรับแก้ไขบทบาทหน้าที่
- **แก้ไข `App.tsx`:** เพิ่มเส้นทางรูท `/users/:id` ครอบด้วย `<Remount>` และ `<RequireRole superAdminOnly>` สำหรับแสดงหน้ารายละเอียดแอดมินรายบุคคล
- **ผลตรวจสอบความถูกต้อง:** การคอมไพล์ build และทดสอบ lint, backend unit tests ทั้งหมดผ่านการทำงานสมบูรณ์ 100% ไม่มี warning ใหม่เกิดขึ้น
