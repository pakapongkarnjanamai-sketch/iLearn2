# PLAN-064 — Assignments page: division lookup ใช้ endpoint SuperAdminOnly → Admin โดน 403

- **Status:** SUPERSEDED by PLAN-065 (พบว่าเป็นปัญหาเชิงระบบหลายหน้า ไม่ใช่แค่ Assignments — รวมไว้ที่ PLAN-065)
- **Assigned:** Antigravity (Gemini)
- **Priority:** High (กระทบ admin ที่ไม่ใช่ SuperAdmin ทุกคน — หน้า Assignments โหลด lookup ไม่ได้)
- **Author:** Claude Code (planner)
- **Related:** ค้นพบระหว่างเปลี่ยน role f6515 (PEERAPORN) SuperAdmin → NLC บน QA

## อาการ

หน้า `admin-react/assignments` (EntityListPage, controller `AssignmentsCRUD`) เมื่อเปิดด้วย user
ที่เป็น **Admin/NLC (ไม่ใช่ SuperAdmin)** จะขึ้น error ใน console:

```
GET .../Service/api/admin/DivisionsCRUD/Get 403 (Forbidden)
Failed to load divisions for lookup  ApiError: Forbidden
```

ตาราง assignments ยังโหลดได้ แต่คอลัมน์ Division จะ map ชื่อไม่ได้ (fallback เป็น `Division {id}`)
และมี toast error เด้ง

## Root cause (ยืนยันแล้ว)

`EntityListPage.tsx` โหลด division lookup (ใช้แค่ map `divisionId` → ชื่อ division ในเซลล์)
โดยเรียก endpoint ของ **management grid ที่เป็น SuperAdminOnly**:

- `iLearn.Admin.React/src/pages/EntityListPage.tsx:27` → `fetchWithAccessControl('admin/DivisionsCRUD/Get')`
- `DivisionsCRUDController` มี `[Authorize(Policy = "SuperAdminOnly")]` (`iLearn.API/Controllers/Base/DivisionsCRUDController.cs:23`)
- policy `SuperAdminOnly` = `RequireRole("SuperAdmin")` (`iLearn.API/Extensions/AuthorizationExtensions.cs:28`)

ก่อนหน้านี้ PEERAPORN เป็น SuperAdmin เลยเรียกผ่าน — พอเปลี่ยนเป็น NLC (RoleType=Admin) จึงโดน 403
**ไม่ใช่ปัญหา cache/data — เป็นบั๊กเลือก endpoint ผิดสิทธิ์** เดิมซ่อนอยู่เพราะทดสอบด้วย SuperAdmin ตลอด

## วิธีแก้ (Scope — ห้ามขยาย)

เปลี่ยนให้ใช้ endpoint division ที่เป็น **AdminOnly** และออกแบบมาเพื่อ lookup อยู่แล้ว
(มี division-isolation ในตัว — Admin ที่มี DivisionId จะเห็นเฉพาะ division ตัวเอง ซึ่งถูกต้อง):

- `DivisionsController.GetLookup` — route `api/Divisions/lookup`, `[Authorize(Policy = "AdminOnly")]`
  (`iLearn.API/Controllers/DivisionsController.cs:35-56`) คืน shape `{ data: [{ id, name, isActive }], totalCount }`
  ผ่าน `DataSourceLoader` — เข้ากับโค้ดที่อ่าน `res.data` และ `d.id`/`div.name` เดิมได้เลย

**แก้ไฟล์เดียว** `iLearn.Admin.React/src/pages/EntityListPage.tsx` บรรทัด 27:

```diff
- fetchWithAccessControl<any>('admin/DivisionsCRUD/Get')
+ fetchWithAccessControl<any>('Divisions/lookup')
```

(ตรวจ base prefix: apiClient ต่อ `Service/api/` ให้อยู่แล้ว — `admin/DivisionsCRUD/Get` map เป็น
`Service/api/admin/DivisionsCRUD/Get` ⇒ `Divisions/lookup` จะเป็น `Service/api/Divisions/lookup` ตรงกับ route)

### หมายเหตุพฤติกรรมที่เปลี่ยน (ตั้งใจ)
- SuperAdmin: เดิม lookup เห็นทุก division → หลังแก้ก็เห็นทุก division เหมือนเดิม (ไม่มี DivisionId claim → ไม่ filter)
- Admin/NLC: เดิม 403 → หลังแก้เห็นเฉพาะ division ของตัวเอง (พอสำหรับ map ชื่อของ assignment ที่ตัวเองเห็น)

## Verification

1. `cd iLearn.Admin.React && npm run lint && npm run build` ผ่าน
2. Deploy QA แล้วเปิด `admin-react/assignments` ด้วย user **f6515 (NLC/Admin)**:
   - ไม่มี 403 / ไม่มี toast "Failed to load division lookup" ใน console
   - คอลัมน์ Division แสดงชื่อถูก (ไม่ใช่ `Division {id}`)
3. Regression: เปิดด้วย user **SuperAdmin** — หน้า assignments ยังโหลด lookup + map ชื่อ division ได้เหมือนเดิม

## Implementer Notes
(เติมหลังทำเสร็จ)
