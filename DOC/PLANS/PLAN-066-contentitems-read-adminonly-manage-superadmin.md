# PLAN-066 — Content Library เข้าถึงไม่ได้สำหรับ Admin: แยก policy อ่าน(AdminOnly)/จัดการ(SuperAdminOnly)

- **Status:** VERIFIED (Claude Code reviewer sign-off — ดูท้ายไฟล์)
- **Assigned:** Antigravity (Gemini)
- **Priority:** High (Admin/NLC เปิด Content Library + preview เนื้อหาใน Course Version ไม่ได้ — 403)
- **Author:** Claude Code (planner)
- **Context:** พบจาก proactive scan หลังเปลี่ยน f6515 SuperAdmin→NLC (ต่อจาก PLAN-065)
- **Execution order:** ทำ **หลัง PLAN-065 เสร็จ** (ทั้งคู่แก้ `EntityListPage.tsx` — 065 แก้บรรทัด 27 division lookup, 066 แก้ ~บรรทัด 212 toolbar content-library; ทำ 065 ให้จบ commit ก่อนกัน merge ชน)
- **Product decision (ผู้ใช้ยืนยัน):** Admin (รวม NLC) ต้อง **อ่าน + preview เนื้อหาได้**; การ **จัดการ (upload/publish/unpublish/delete/bulk/edit)** คง **SuperAdmin เท่านั้น**. ContentItems เป็น resource รวม ไม่ผูก division (Admin เห็นได้ทั้งหมด — ยอมรับได้)

## Root cause

`ContentItemsController` (`api/ContentItems`) มี `[Authorize(Policy="SuperAdminOnly")]` **ระดับ class** ครอบทุก action
(`iLearn.API/Controllers/ContentItemsController.cs:19`) โดยไม่มี method override — แต่หน้า Content Library อยู่ใน
sidebar section "Operations" (Admin เห็น) และ route `content-library/*` ใน `App.tsx` **ไม่ได้ guard** superAdminOnly
→ Admin กดเข้าแล้ว list/preview ยิง endpoint SuperAdminOnly = **403**

จุดที่ 403 สำหรับ Admin:
- List: `GET api/ContentItems` / `api/ContentItems/paged`
- Preview SCORM: `GET api/ContentItems/{id}/content` — ใช้ทั้งใน `ContentItemDetailPage.tsx:132`
  **และ `VersionDetailPage.tsx:199` (โซน Courses ที่ Admin ใช้งานปกติ)**

หมายเหตุ contract เพิ่มเติมที่เจอ: `ContentItemsCRUDController` (`api/admin/ContentItemsCRUD`) **ไม่มี `[Authorize]` เลย**
(`iLearn.API/Controllers/Base/ContentItemsCRUDController.cs:28`) → ปัจจุบันเปิดถึงแค่ "authenticated domain user"
(ต่ำกว่า Admin ด้วยซ้ำ) — หน้า detail/editor โหลด/ลบ/แก้ผ่าน endpoint นี้ (`ContentItemDetailPage.tsx:80,157`,
`ContentItemEditorPage.tsx:43,105`) แผนนี้ปิดช่องนี้ไปพร้อมกัน

## Scope — Backend (policy matrix, ต้องครบทุก action)

⚠️ **สำคัญด้านความปลอดภัย:** เมื่อถอด `[Authorize]` ระดับ class ออก action ที่ไม่ได้ระบุ policy จะตกไปที่
`FallbackPolicy` = แค่ authenticated (ต่ำกว่า Admin) — **ห้ามมี action ไหนไม่มี `[Authorize]` เด็ดขาด**
ต้องใส่ attribute ครบทุกตัวตามตารางนี้

### 1) `iLearn.API/Controllers/ContentItemsController.cs`
ถอด `[Authorize(Policy="SuperAdminOnly")]` ระดับ class ออก แล้วใส่ระดับ method ทุกตัว:

| Action | Route | Policy ใหม่ |
|---|---|---|
| `GetAll` (56) | `GET /` | **AdminOnly** |
| `GetPaged` (63) | `GET paged` | **AdminOnly** |
| `GetById` (177) | `GET {id}` | **AdminOnly** |
| `GetContent` (185) | `GET {id}/content` | **AdminOnly** |
| `Upload` (213) | `POST upload` | SuperAdminOnly |
| `SetPublic` (267) | `POST SetPublic` | SuperAdminOnly |
| `Unpublish` (305) | `POST Unpublish` | SuperAdminOnly |
| `Delete` (330) | `DELETE {id}` | SuperAdminOnly |
| `OptimizeAnalysis` (362) | `GET Admin/OptimizeAnalysis` | SuperAdminOnly |
| `BatchUnpublish` (463) | `POST Admin/BatchUnpublish` | SuperAdminOnly |
| `PreviewBatchUnpublish` (525) | `POST Admin/PreviewBatchUnpublish` | SuperAdminOnly |
| `BatchPublish` (539) | `POST Admin/BatchPublish` | SuperAdminOnly |
| `BatchPublishStream` (587) | `POST Admin/BatchPublishStream` | SuperAdminOnly |
| `BulkSetPublic` (817) | `POST Admin/BulkSetPublic` | SuperAdminOnly |
| `BulkDeletePublished` (1032) | `DELETE Admin/BulkDeletePublished` | SuperAdminOnly |

(ทางเลือกที่ปลอดภัยกว่า: คง `[Authorize(Policy="SuperAdminOnly")]` ระดับ class ไว้เป็น default แล้ว **ใส่
`[Authorize(Policy="AdminOnly")]` เฉพาะ 4 action อ่าน** — **แต่ระวัง:** ASP.NET Core รวม `[Authorize]` แบบ AND
หลาย attribute ต้องผ่านทั้งหมด → class SuperAdmin + method Admin = ยังต้อง SuperAdmin ใช้ไม่ได้. ดังนั้น**ต้อง
ถอด class-level ออกแล้วระบุครบทุก method** ตามตารางเท่านั้น)

### 2) `iLearn.API/Controllers/Base/ContentItemsCRUDController.cs`
ปัจจุบันไม่มี `[Authorize]` — เพิ่มให้:
- class-level `[Authorize(Policy="AdminOnly")]` (อ่าน Get/Get/{id}/GetByCourse/GetServerStats/GetSummaryStats ได้)
- override `Post`/`Put`/`Delete` ใส่ `[Authorize(Policy="SuperAdminOnly")]` (จัดการเฉพาะ SuperAdmin)
  - ถ้า `Post` เป็น inherited (ไม่ override) ให้ override เพื่อใส่ attribute หรือย้าย logic — ห้ามปล่อยให้ write เป็น AdminOnly

## Scope — Frontend (ซ่อนปุ่มจัดการสำหรับ non-SuperAdmin + guard route editor)

ใช้ `useSession().isSuperAdmin` gate ให้ Admin เห็น Content Library แบบ **read-only**:

1. `iLearn.Admin.React/src/App.tsx` — ครอบ route editor ด้วย `<RequireRole superAdminOnly>`:
   - `content-library/new` (70), `content-library/:id/edit` (72)
   - route list (69) + detail (71) **คงเปิดให้ Admin**
2. `iLearn.Admin.React/src/pages/EntityListPage.tsx` — toolbar content-library (ปุ่ม New/Upload + bulk actions
   บล็อก `config.controller === 'ContentItemsCRUD'` ~บรรทัด 212) แสดงเฉพาะเมื่อ `isSuperAdmin`
3. `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx` — ปุ่ม Publish/Unpublish/Delete/Edit
   (`handlePublish/handleUnpublish/handleDelete` + ลิงก์ไป edit) แสดงเฉพาะ `isSuperAdmin`; ปุ่ม **Open/Preview
   content (`handleOpenContent`) คงไว้ให้ Admin**
4. ตรวจว่า `VersionDetailPage.tsx` ปุ่ม preview เนื้อหา (`ContentItems/{id}/content`) ใช้งานได้กับ Admin แล้ว
   (backend ข้อ 1 ปลดล็อกให้) — ไม่ต้องแก้ frontend ตรงนี้ นอกจากยืนยัน

## นอก scope
- ไม่ทำ division-isolation ให้ ContentItems (ผู้ใช้ยอมรับว่าเป็น resource รวม)
- privilege gap เรื่อง `CategoriesCRUD` create/edit/delete จาก CourseListPage (Admin แก้ category ข้าม division ได้) —
  แยกเป็นงานอื่น (บันทึกใน AGENT_LOG แล้ว)

## Verification

1. `dotnet build iLearn.Tests -o artifacts\verify-066` + `dotnet test` ผ่าน (มี test เดิมของ ContentItems ครบ)
   - **เพิ่ม/ยืนยัน test policy:** action อ่าน 4 ตัว = AdminOnly, action จัดการ = SuperAdminOnly (อย่างน้อย assert
     ว่าไม่มี action ไหนหลุดเป็น authenticated-only)
2. `cd iLearn.Admin.React && npm run lint && npm run build` ผ่าน
3. Deploy QA — ล็อกอิน **f6515 (NLC/Admin)**:
   - `content-library` list โหลดได้ (ไม่ 403), เปิด detail อ่านได้, กด **Preview/Open content ได้**
   - **ไม่เห็นปุ่ม** New/Upload/Edit/Publish/Unpublish/Delete/bulk; เข้าถึง `content-library/new` หรือ `/:id/edit`
     ตรง ๆ → เด้ง `/access-denied`
   - เปิด Course → Version detail → **preview เนื้อหา SCORM ได้ ไม่ 403**
   - ยิงตรง (DevTools/console) `POST api/ContentItems/SetPublic` ในฐานะ Admin → **ต้องได้ 403** (จัดการยังล็อก)
4. Regression **SuperAdmin**: Content Library ครบทุกปุ่ม (upload/publish/unpublish/delete/bulk/edit) ทำงานเหมือนเดิม

## Implementer Notes
- ปรับเปลี่ยน Backend Controllers สำหรับสิทธิ์การใช้งานของ Content Library:
  - ใน [ContentItemsController.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/ContentItemsController.cs) ถอด class-level SuperAdminOnly policy ออก และจัดแจงระบุ [Authorize] ในแต่ละ action method ครบถ้วน (อ่าน = AdminOnly, จัดการ = SuperAdminOnly)
  - ใน [ContentItemsCRUDController.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/Base/ContentItemsCRUDController.cs) เพิ่มสิทธิ์ระดับ class เป็น [Authorize(Policy = "AdminOnly")] และ override เมธอด Post/Put/Delete ให้ตรวจสอบสิทธิ์เป็น [Authorize(Policy = "SuperAdminOnly")] เพื่อปิดสิทธิ์ของผู้ใช้ทั่วไป
- ปรับเปลี่ยน Frontend React components:
  - ใน [App.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/App.tsx) ทำการครอบสิทธิ์สำหรับหน้าสร้าง/แก้ไขสคอร์มด้วย `<RequireRole superAdminOnly>`
  - ใน [EntityListPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/EntityListPage.tsx) และ [ContentItemDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx) ดึงข้อมูล `isSuperAdmin` จาก `useSession` เพื่อคุมสิทธิ์การซ่อนปุ่ม Upload, Edit, Publish/Unpublish, Delete ไม่ให้แสดงหากผู้ใช้ไม่มีบทบาท SuperAdmin
- การตรวจสอบความถูกต้อง:
  - รันคำสั่ง `npm run lint` และ `npm run build` ของ React frontend ผ่านโดยไม่มีข้อผิดพลาด
  - รัน unit tests ของ C# ทั้งหมด 136/136 รายการผ่านครบ 100%

## Reviewer Sign-off (Claude Code) — VERIFIED
- **Security (จุดสำคัญสุด) — PASS:** grep ทุก `[Http*]` ใน `ContentItemsController` หลังถอด class-level → **ทั้ง 15 action มี `[Authorize]` กำกับครบ ไม่มีตัวหลุดไป fallback (authenticated-only)**. Read 4 ตัว (GetAll/GetPaged/GetById/GetContent)=AdminOnly, Write 11 ตัว (Upload/SetPublic/Unpublish/Delete/Admin\* ทั้งหมด)=SuperAdminOnly ตรง matrix เป๊ะ
- **`ContentItemsCRUDController` — PASS:** class-level `AdminOnly` (ปิดช่อง authenticated-only เดิม) + `Post`(override ใหม่)/`Put`/`Delete` = `SuperAdminOnly`. หมายเหตุ ASP.NET Core รวม `[Authorize]` แบบ AND → write = AdminOnly∧SuperAdminOnly = SuperAdmin (ถูกต้อง เพราะ SuperAdmin ผ่านทั้งคู่); read (Get/GetByCourse/stats) = AdminOnly
- **Frontend gating — PASS:** `App.tsx` guard route `content-library/new` + `/:id/edit` ด้วย `superAdminOnly` (list+detail คงเปิด Admin); `EntityListPage` ปุ่ม Upload SCORM gate `isSuperAdmin`, grid actionButtons เหลือแค่ "Open Details" (อ่าน); `ContentItemDetailPage` ซ่อน Edit/Publish/Unpublish/Delete ให้ non-super. **Open SCORM Player + Download ZIP คงไว้ให้ Admin — ยิง `ContentItems/{id}/content` = GetContent (AdminOnly) จึงไม่ 403** ✓
- **ไม่ regress learner/player:** `{id}/content` เดิมเป็น SuperAdminOnly อยู่แล้ว learner ใช้ endpoint player คนละตัว → เปลี่ยนเป็น AdminOnly แค่ "ขยาย" สิทธิ์ ไม่ตัดใคร
- **Reviewer รันเอง:** `dotnet build iLearn.Tests` 0 error; `dotnet test` **136/136 passed**; `npm run lint` clean; `npm run build` เขียว
- **คงเหลือ (QA smoke ต้อง redeploy ทั้ง API + React ก่อน):** ล็อกอิน f6515 → content-library โหลด/preview ได้, ไม่เห็นปุ่มจัดการ, เข้าตรง `/content-library/new`→access-denied, `POST api/ContentItems/SetPublic` ในฐานะ Admin ต้องได้ **403**, Course Version preview SCORM ผ่าน; regression SuperAdmin ครบทุกปุ่ม
- **หมายเหตุ commit:** ยังไม่ commit (รอผู้ใช้เคาะ scope — 065+066 พันกันใน EntityListPage.tsx + ไฟล์ค้างนอก scope BulkAssign\*)

