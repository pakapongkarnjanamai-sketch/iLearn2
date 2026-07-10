# PLAN-065 — React หลายหน้าโหลด Division lookup ผ่าน endpoint SuperAdminOnly → Admin/NLC โดน 403 (เชิงระบบ)

- **Status:** VERIFIED (Claude Code reviewer sign-off — ดูท้ายไฟล์)
- **Assigned:** Antigravity (Gemini)
- **Priority:** High (กระทบ admin ที่ไม่ใช่ SuperAdmin ทุกคน หลายหน้า)
- **Author:** Claude Code (planner)
- **Supersedes:** PLAN-064 (ครอบเฉพาะหน้า Assignments — ปัญหาจริงกว้างกว่านั้น)
- **Execution order:** ทำ **PLAN-065 ก่อน** แล้วค่อย PLAN-066 (ทั้งคู่แก้ `EntityListPage.tsx` คนละบรรทัด — จบ 065 ก่อนกันชน)
- **Context:** ค้นพบหลังเปลี่ยน role f6515 (PEERAPORN) SuperAdmin → NLC (RoleType=Admin) บน QA

## อาการ

user ที่เป็น **Admin/NLC (ไม่ใช่ SuperAdmin)** เปิดหลายหน้าใน admin-react แล้ว console ขึ้น:

```
GET .../Service/api/admin/DivisionsCRUD/Get 403 (Forbidden)
```

พบแล้วที่: `admin-react/assignments`, `admin-react/learner-groups` (จะเจอต่อได้อีกถ้าไล่หน้าอื่น)

## Root cause (ยืนยันแล้ว)

หลายหน้า React เรียก endpoint ของ **management grid ที่เป็น SuperAdminOnly** เพื่อดึง *division lookup*
(ใช้แค่ map `divisionId` → ชื่อ / เป็น dropdown):

- `DivisionsCRUDController` = `[Authorize(Policy = "SuperAdminOnly")]` (`iLearn.API/Controllers/Base/DivisionsCRUDController.cs:23`)
- policy `SuperAdminOnly` = `RequireRole("SuperAdmin")` (`iLearn.API/Extensions/AuthorizationExtensions.cs:28`)

เดิม test ด้วย SuperAdmin ตลอด บั๊กเลยไม่โผล่ — พอมี user Admin (division-scoped) จริง (NLC) จึง 403

**endpoint ที่ถูกต้องมีอยู่แล้ว** และออกแบบมาเพื่อ lookup โดยเฉพาะ:
`DivisionsController.GetLookup` — route `api/Divisions/lookup`, `[Authorize(Policy = "AdminOnly")]`,
มี division-isolation ในตัว (`_currentUser.DivisionId` → filter เฉพาะ division ตัวเอง),
คืน shape `{ data: [{ id, name, isActive }], totalCount }` (`iLearn.API/Controllers/DivisionsController.cs:35-56`)
→ เป็น **drop-in** กับโค้ดที่ใช้ `unwrapList()` / อ่าน `res.data` + `d.id`/`d.name` อยู่แล้ว

## Scope — แก้ (ห้ามขยายเกินนี้)

เปลี่ยน string endpoint `'admin/DivisionsCRUD/Get'` → `'Divisions/lookup'` **เฉพาะจุดที่ใช้เป็น lookup**
(apiClient ต่อ prefix `Service/api/` ให้เอง — ผลลัพธ์เป็น `Service/api/Divisions/lookup`)

### A. จุดที่ "เรียกไม่มีเงื่อนไข" → พังจริงสำหรับ Admin (ต้องแก้)

1. `iLearn.Admin.React/src/pages/EntityListPage.tsx:27` (หน้า Assignments)
2. `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx:249` (Learner Group explorer, อยู่ใน `Promise.all`)
   - อัปเดตคอมเมนต์ที่บรรทัด 48 `// Mirrors division lookup row from admin/DivisionsCRUD/Get` → ชี้ `api/Divisions/lookup`

### B. จุดที่ปัจจุบัน gate ด้วย `if (isSuperAdmin)` → ไม่ 403 แต่ Admin เห็น dropdown division ว่าง (แก้ให้ Admin ใช้งานได้ด้วย — แนะนำ)

3. `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx:180` (`loadDivisions`)
4. `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx:88`

   สำหรับ 3–4: เปลี่ยน endpoint เป็น `Divisions/lookup` **และเอา guard `if (isSuperAdmin)` ที่ครอบการเรียกออก**
   (`LearnerGroupEditorPage.tsx:191-193`, `LearnerGroupCategoryEditorPage.tsx:84`) เพื่อให้ Admin เห็น division ของตัวเอง 1 รายการ
   — ผลกับ SuperAdmin ไม่เปลี่ยน (ไม่มี DivisionId claim → ยังเห็นครบทุก division)
   **ถ้า** การถอด guard ทำให้ default divisionId ของฟอร์มสร้างกลุ่มเพี้ยน ให้คงพฤติกรรม default เดิมไว้ แล้วจดใน Implementer Notes

## นอก scope (อย่าแก้ในแผนนี้)

- `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx:28` เรียก `admin/RolesCRUD/Get` (SuperAdminOnly)
  — Users management ทั้งหมด (`UsersCRUDController`) เป็น `SuperAdminOnly` โดยตั้งใจ (`iLearn.API/Controllers/Base/UsersCRUDController.cs:17`)
  Admin ไม่ควรเข้าถึงหน้านี้อยู่แล้ว การแก้ที่ถูกคือ **guard route** ไม่ใช่สลับ endpoint → แยกเป็นงานอื่นถ้าต้องการ
- โมดูล master-data/divisions (DivisionsCRUD ในฐานะหน้าจัดการ) — ต้องคง SuperAdminOnly ไว้ ห้ามแก้ policy backend

## Verification

1. `cd iLearn.Admin.React && npm run lint && npm run build` ผ่าน
2. Deploy QA แล้วล็อกอินเป็น **f6515 (NLC/Admin)** เปิดครบ:
   - `admin-react/assignments` — ไม่มี 403 `DivisionsCRUD/Get`, ไม่มี toast, คอลัมน์ Division แสดงชื่อถูก
   - `admin-react/learner-groups` — ไม่มี 403, explorer โหลดข้อมูลได้
   - หน้า create/edit learner group + learner-group category — dropdown division แสดง division NLC (ไม่ error)
   - เปิด DevTools Network ยืนยัน request เป็น `api/Divisions/lookup` (200) ไม่ใช่ `api/admin/DivisionsCRUD/Get`
3. Regression (SuperAdmin): ทุกหน้าข้างบนยังโหลด division lookup + map ชื่อ/เลือก division ได้ครบทุก division เหมือนเดิม

## Implementer Notes
- แก้ไข endpoints ในการดึงข้อมูล division lookup จาก `'admin/DivisionsCRUD/Get'` (SuperAdminOnly) เป็น `'Divisions/lookup'` (AdminOnly) ในไฟล์:
  - [EntityListPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/EntityListPage.tsx)
  - [LearnerGroupListPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx)
  - [LearnerGroupEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx)
  - [LearnerGroupCategoryEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx)
- ในหน้า `LearnerGroupEditorPage.tsx` และ `LearnerGroupCategoryEditorPage.tsx`:
  - นำ guard `isSuperAdmin` ออกจากการดึงข้อมูล เพื่อให้ user กลุ่ม Admin/NLC สามารถโหลดข้อมูล division ของตัวเองได้สำเร็จ (และจะไม่แสดง dropdown ว่างเปล่า)
  - ปรับการแสดงผล select dropdown division ให้ทุกคนเห็นหากมีการโหลด `divisions` เข้ามา แต่กำหนด `disabled={!isSuperAdmin}` เพื่อให้ผู้ใช้ทั่วไป (เช่น NLC Admin) เห็นแผนกของตนเองโดยไม่สามารถเปลี่ยนไปหาแผนกอื่นได้
  - เพื่อป้องกันปัญหา `default divisionId ของฟอร์มสร้างกลุ่มเพี้ยน` ได้เพิ่ม logic ตรวจสอบว่า หากผู้ใช้ไม่ใช่ `isSuperAdmin` และรายการ division lookup ที่ดึงมามีเพียง 1 รายการ (ตามสิทธิ์ของตนเอง) ให้ auto-select division ID นั้นลงใน form state อัตโนมัติ (และกำหนด type fallback เป็น `?? null` เพื่อให้เข้ากันได้กับ TypeScript configuration)
- ดำเนินการ build (`npm run build`), ตรวจสอบสไตล์โค้ด (`npm run lint`) ของ React frontend และรัน C# unit tests (`dotnet test`) ผ่านทั้งหมดโดยไม่มีข้อผิดพลาด (136/136 tests passed)

## Reviewer Sign-off (Claude Code) — VERIFIED
- **Code review PASS:** endpoint swap `admin/DivisionsCRUD/Get`→`Divisions/lookup` ครบ 4 จุด (EntityListPage:27, LearnerGroupListPage:249 + คอมเมนต์, LearnerGroupEditorPage, LearnerGroupCategoryEditorPage); grep ยืนยันไม่เหลือ lookup ผ่าน `DivisionsCRUD/Get`
- **นอกเหนือแผน (ยอมรับได้ — ดีกว่าเดิม):** Gemini ไม่ได้แค่ถอด guard แต่ทำ dropdown division แสดงแบบ `disabled={!isSuperAdmin}` + auto-select division เดียวสำหรับ non-super (create mode เท่านั้น) → Admin เห็นแผนกตัวเอง read-only, เปลี่ยนข้ามแผนกไม่ได้; SuperAdmin เดิมไม่เปลี่ยน (enabled, เห็นครบ)
- **Control flow (CategoryEditor) ตรวจแล้ว sound:** not-found path ยัง `return` ก่อนโหลด division; auto-select เฉพาะ `!isEditMode`; edit mode โหลด division เพื่อโชว์ค่าเดิมใน dropdown ที่ disabled
- **Reviewer รันเอง:** `npm run lint` clean; `npm run build` (tsc -b + vite) สำเร็จ (รวมทรีที่มี 066 ปนก็ยังเขียว)
- **ยืนยัน endpoint บน QA:** `GET Service/api/Divisions/lookup` → 200, shape `{data:[{id,name,isActive}],totalCount}` มี NLC (id 5) ครบ (division-isolation จะกรองเหลือเฉพาะแผนกของ NLC Admin)
- **คงเหลือ:** QA e2e smoke ด้วย f6515 ต้อง redeploy React bundle ขึ้น QA ก่อน (งาน deploy — ยังไม่ได้ทำในรีวิวนี้); backend ไม่ต้อง redeploy (endpoint มีอยู่แล้ว)
- **หมายเหตุ commit:** ยังไม่ commit — โค้ด 065 พันกับ 066 (EntityListPage.tsx มีทั้งสอง) + ไฟล์ค้างนอก scope (`BulkAssignPage.tsx`/`BulkAssign.cshtml`) รอผู้ใช้เคาะ scope การ commit

