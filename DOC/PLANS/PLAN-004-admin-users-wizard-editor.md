# PLAN-004: หน้า Admin Users — เปลี่ยน new/edit จาก slide-over panel เป็น Wizard ตามมาตรฐานระบบ

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: commit 54e4a8e; lint 0err / build / dotnet test 115 ผ่าน)
- **Assigned:** Gemini
- **Priority:** High
- **Estimated scope:** 1 ไฟล์ใหม่ (`UserEditorPage.tsx`) + แก้ 3 ไฟล์ (`AdminUsersPage.tsx`, `App.tsx`, `Breadcrumbs.tsx`)

## Problem

หน้า `/users` (`iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`) ตอนนี้ทำ create/edit ผ่าน **slide-over panel 2 อันในหน้าเดียว** (panel "Add Admin User" บรรทัด ~272-317 และ panel "Roles — …" บรรทัด ~195-269) ซึ่งไม่ตรงมาตรฐานของระบบ — editor ทุกตัว (Courses, Content Library, Learner Groups, SCORM Version) ใช้ **หน้าแยกแบบ Wizard** (`AppWizard` จาก `src/components/ui/AppWizard.tsx`) ที่ route `/<module>/new` และ `/<module>/:id/edit` ครอบด้วย `<Remount>`

## Backend contract (ห้ามแก้ backend — ลอก shape ตามนี้)

Controller: `iLearn.API/Controllers/Base/UsersCRUDController.cs` (policy SuperAdminOnly)

- `GET admin/UsersCRUD/Get` — DevExtreme DataSourceLoader, คืน `{ totalCount, groupCount, summary, data: [...] }` แต่ละ row enriched ด้วยข้อมูล employee: `id, nid, lastLogin, createdAt, isActive, userRoles[], employeeId, fullName, email, division, department, section, position` (camelCase) — `userRoles[]` แต่ละตัวมี `UserId, RoleId, Role { Id, Name, RoleType, DivisionId }` (โค้ดหน้าเดิมรองรับ shape นี้แล้ว ดู type `AdminUser`/`UserRoleInfo`/`RoleInfo` ใน `AdminUsersPage.tsx` — ย้ายไปใช้ต่อได้)
  - **ไม่มี endpoint GetById** — หน้า edit ให้โหลด user เดี่ยวผ่าน DevExtreme filter: `admin/UsersCRUD/Get?filter=[["id","=",<id>]]` (encode ด้วย `encodeURIComponent`) แล้วใช้ `data[0]`; ถ้าไม่เจอ → แสดง `NotFoundState`
- `POST admin/UsersCRUD/Post` — FormData `values` = JSON เช่น `{"nid":"N1234"}` (มาจาก `GenericController.Post`) คืน `Ok(newEntity)` = user ที่สร้างพร้อม `id` (camelCase)
- `PUT admin/UsersCRUD/Put` — FormData `key` = user id, `values` = JSON; ถ้ามี `roleIds: number[]` controller จะ sync UserRole ให้ (ลบที่หาย เพิ่มที่ใหม่)
- Roles lookup: `GET admin/RolesCRUD/Get?requireTotalCount=false` (โค้ดเดิมโหลดอยู่แล้ว บรรทัด ~47-54)

## Scope (ทำแค่นี้)

### 1. ไฟล์ใหม่ `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`

ใช้ `AppWizard` ตาม pattern ของ `LearnerGroupEditorPage.tsx` / `ContentItemEditorPage.tsx` (state `currentStep`, `steps: WizardStep[]` ใน `useMemo`, `onSubmit` → toast → `navigate('/users')`)

**โหมด create (`/users/new`):** 3 steps
1. **User** — input Employee NID (uppercase อัตโนมัติเหมือนเดิม, placeholder "e.g. N4734", helper text เดิม) — `validate`: NID ไม่ว่าง
2. **Roles** — checkbox list ของ roles ทั้งหมด (ย้าย UI checkbox จาก panel เดิมบรรทัด ~224-249 มาใช้) — เลือกได้ 0 ตัวขึ้นไป (optional)
3. **Review** — สรุป NID + รายชื่อ roles ที่เลือก
- submit: `POST admin/UsersCRUD/Post` ด้วย `{ nid }` → ถ้าเลือก roles ไว้ ต่อด้วย `PUT admin/UsersCRUD/Put` ด้วย `key` = id จาก response และ `values` = `{ roleIds }` → toast success → กลับ `/users`
- ถ้า POST สำเร็จแต่ PUT ล้ม: toast error บอกว่า "user ถูกสร้างแล้วแต่ assign roles ไม่สำเร็จ ไปแก้ที่หน้า edit" แล้วกลับ `/users` (อย่าทิ้งผู้ใช้ค้างใน wizard)

**โหมด edit (`/users/:id/edit`):** 2 steps
1. **Roles** — แสดงข้อมูล user แบบอ่านอย่างเดียว (NID, Name, Division — เหมือน panel เดิมบรรทัด ~204-219) + checkbox roles ติ๊กตามที่ user มีอยู่
2. **Review** — สรุปการเปลี่ยนแปลง roles
- submit: `PUT admin/UsersCRUD/Put` ด้วย `{ roleIds }` (logic เดิมจาก `handleSaveRoles` บรรทัด ~76-95) → toast → กลับ `/users`
- ระหว่างโหลด user ใช้ `LoadingState`, ไม่เจอใช้ `NotFoundState` (shared components ใน `src/components/ui`)

### 2. `AdminUsersPage.tsx` — ตัด panel ออก เปลี่ยนเป็น navigate

- ปุ่ม "Add Admin User" → `navigate('/users/new')` (หรือ `Link`)
- action button รูป Shield + double-click row → `navigate(`/users/${id}/edit`)`
- ลบ state/JSX ของทั้งสอง panel (`selectedUser`, `pendingRoleIds`, `showAddPanel`, `addNid`, `handleSaveRoles`, `handleAddUser`, `hasRoleChanges`, JSX บรรทัด ~194-317) — type `AdminUser`/`RoleInfo`/`UserRoleInfo` ย้าย/export ให้ `UserEditorPage` ใช้ร่วม (อย่าประกาศซ้ำสองที่)
- `refreshKey` ไม่จำเป็นแล้ว (กลับเข้าหน้า list จะ mount ใหม่) — ลบได้

### 3. `App.tsx` — เพิ่ม 2 routes ใต้ comment `{/* Admin Users */}`

```tsx
<Route path="users/new" element={<RequireRole superAdminOnly><Remount><UserEditorPage /></Remount></RequireRole>} />
<Route path="users/:id/edit" element={<RequireRole superAdminOnly><Remount><UserEditorPage /></Remount></RequireRole>} />
```
(**ต้องครอบ `<Remount>`** ตามกติกา route editor ใน CLAUDE.md และครอบ `RequireRole superAdminOnly` เหมือน route `/users` เดิม)

### 4. `src/components/layout/Breadcrumbs.tsx` — เติม `'users': 'Admin Users'` ใน `SEGMENT_MAP`

(ตอนนี้ segment `users` ไม่อยู่ใน map — fallback แสดง "Users" เฉย ๆ; `new`/`edit` มี map อยู่แล้ว)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend ทุกไฟล์ (`UsersCRUDController`, `GenericController`)
- ห้ามแก้ `AppWizard.tsx` และ shared components อื่น
- ห้ามเพิ่ม validation NID กับระบบ HR (เกินขอบเขต — ตรวจแค่ไม่ว่างตามเดิม)
- ห้ามแตะ grid columns / search ของ `AdminUsersPage` (เปลี่ยนเฉพาะส่วน create/edit)

## Acceptance criteria

- [x] `/users/new` เป็นหน้า wizard 3 steps (User → Roles → Review), สร้าง user + assign roles จบใน flow เดียว
- [x] `/users/:id/edit` เป็นหน้า wizard 2 steps (Roles → Review), บันทึก roles ได้เหมือน panel เดิม
- [x] หน้า `/users` ไม่เหลือ slide-over panel; ปุ่ม Add / icon Shield / double-click row นำทางไปหน้า wizard
- [x] โหลด user ใน edit ผ่าน filter แล้วเปิด id ที่ไม่มีจริง → `NotFoundState`
- [x] เนื้อหาอยู่ในการ์ด `rounded-lg border border-slate-200 bg-white` (AppWizard จัดให้อยู่แล้ว) + ใช้ `LoadingState`/`NotFoundState`/`toast` ตามมาตรฐาน
- [x] Breadcrumb แสดง "Admin Users / Create" และ "Admin Users / Modify"
- [x] Response types มีคอมเมนต์ `// Mirrors UsersCRUDController.Get (iLearn.API/Controllers/Base/UsersCRUDController.cs)` ตามกติกา API Contract Sync

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: `/users` → Add Admin User → ไล่ครบ 3 steps → user ใหม่โผล่ใน grid พร้อม roles; เปิด edit ผ่าน icon → เปลี่ยน roles → save → grid อัปเดต; เปิด `/users/999999/edit` → NotFoundState

## Implementer Notes

- **สร้างไฟล์ใหม่:** [UserEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserEditorPage.tsx) เพื่อรองรับการจัดการข้อมูล Admin User แบบ Wizard (มีขั้นตอนแยกตามความต้องการทั้งในโหมด Create (3 steps) และโหมด Edit (2 steps) ครบถ้วน)
- **ปรับปรุง `AdminUsersPage.tsx`:** ถอด slide-over panel, state ทั้งหมด, และ dead code อื่น ๆ ออก เปลี่ยนพฤติกรรมเป็นการนำทางผ่าน `useNavigate` ไปยังหน้าแยก และทำการ export ชนิดข้อมูล `RoleInfo`, `UserRoleInfo`, `AdminUser` พร้อมใส่คอมเมนต์อ้างอิง API contract ไปยัง C# controller ตามกติกา
- **แก้ไข `App.tsx`:** เพิ่มเส้นทางกำหนดหน้า `/users/new` และ `/users/:id/edit` ครอบด้วย `<Remount>` และ `<RequireRole superAdminOnly>` เรียบร้อย
- **แก้ไข `Breadcrumbs.tsx`:** เพิ่มคีย์ `'users': 'Admin Users'` ใน `SEGMENT_MAP` เพื่อปรับแถบ Breadcrumbs ให้แสดงผลสวยงามและสื่อความหมาย
- **ผลการทดสอบ:** ทั้ง `npm run lint`, `npm run build`, และ backend `dotnet test` ทำงานผ่านสมบูรณ์ 100% ปราศจาก Error และ Warning ใหม่
