# PLAN-044: แก้ DevExtreme E1046/E1040 — MVC admin learner grids ใช้ key/field PascalCase ไม่ตรงกับ API (camelCase)

- **Status:** VERIFIED (รีวิวโดย Claude Code 2026-06-30 — ดู Review Notes ท้ายไฟล์)
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** High — บล็อกผู้ใช้จริงบน production (เลือก learner ในหน้า BulkAssign / AddMembers / Learners ไม่ได้)
- **Estimated scope:** เปลี่ยนชื่อ field ใน DevExtreme grid config + JS row references จาก PascalCase เป็น camelCase ใน iLearn.Admin views ~5 ไฟล์ (ไม่มี backend/contract change)

## Problem

ผู้ใช้เปิด `https://ap-ntc2138-qawb/iLearnNew/admin/Assignments/BulkAssign?courseId=862` → ขั้น Learner ตาราง "Learner Directory" ว่างเปล่า + ขึ้น error:
- **E1046** — "The 'EId' key field is not found in data objects."
- **E1040** — "The 'data,undefined' key value is not unique within the data array." (ผลพวงจาก key เป็น `undefined` ทุก row)
- คอลัมน์ Position / Dept / Section แสดง "—" ทั้งหมด

## Evidence และ Root Cause

1. grid learner directory ตั้ง store key + column เป็น **PascalCase**: [BulkAssign.cshtml:764](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin/Views/Assignments/BulkAssign.cshtml#L764) `key: "EId"`, [:778](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin/Views/Assignments/BulkAssign.cshtml#L778) `dataField: "EId"` — โหลดจาก `${serviceUrl}/Learners/Get`
2. API endpoint [LearnersController.Get](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/LearnersController.cs#L149) deserialize ข้อมูลจาก external API เป็น typed DTO `LearnerGridRowDto` (C# property `EId`, `EnglishFirstName`, …) แล้ว `return Ok(response)`
3. API ตั้ง JSON policy **camelCase** ([PresentationExtensions.cs:18](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Extensions/PresentationExtensions.cs#L18) `PropertyNamingPolicy = CamelCase`) → C# `EId` ถูก serialize เป็น **`eId`**, `EnglishFirstName` → `englishFirstName`, `Position` → `position` ฯลฯ
4. grid หา key `EId` (Pascal) ใน 데이터ที่เป็น `eId` (camel) → ไม่เจอ → E1046; key = undefined ทุก row → ซ้ำ → E1040

**สรุป:** casing mismatch — API คืน learner rows เป็น **camelCase** (ตรงกับ note ใน `CLAUDE.md`: *"Learners rows เป็น camelCase (`nid`, `eId`) — backend deserialize เป็น typed DTO แล้ว"*) แต่ MVC `iLearn.Admin` grids ยังใช้ **PascalCase** React admin อัปเดตตามแล้ว แต่ MVC ตกหล่น

> หมายเหตุ contract: fix นี้ต้องแก้ฝั่ง **frontend (MVC views) → camelCase** เท่านั้น **ห้ามแก้ API ให้คืน PascalCase** เพราะ `iLearn.Admin.React` พึ่ง camelCase อยู่ — การกลับ API จะทำให้ React พังแทน

## Scope (ทำแค่นี้)

แก้ทุก DevExtreme learner grid ใน `iLearn.Admin` ที่ดึง `Learners/Get` ให้ field name เป็น **camelCase** ตรงกับ API:

| PascalCase เดิม | camelCase ใหม่ |
|---|---|
| `EId` | `eId` |
| `EnglishFirstName` | `englishFirstName` |
| `EnglishLastName` | `englishLastName` |
| `Division` | `division` |
| `Department` | `department` |
| `Section` | `section` |
| `Position` | `position` |

แก้ทั้ง `key:`, `dataField:`, `calculateCellValue` (เช่น `d.EnglishFirstName`), และ JS row reference (เช่น `row.EId`, `String(row.EId)`) ในไฟล์ต่อไปนี้:

1. **`Assignments/BulkAssign.cshtml`** (ที่รายงาน) — บรรทัด 764, 778, 785, 807, 1319, 1384, 1387
2. **`Assignments/Detail.cshtml`** — บรรทัด 673, 685, 693
3. **`Learners/Index.cshtml`** — บรรทัด 30, 36, 44, 45, และคอลัมน์ `Division/Department/Section/Position` (46–49)
4. **`LearnerGroups/AddMembers.cshtml`** — บรรทัด 424, 433, 440, 462, 466, 502
5. **`LearnerGroups/Editor.cshtml`** — บรรทัด 525, 534, 541, 805, 922, 927 (มี fallback `eid` lowercase อยู่บางจุด — ปรับ `key`/`dataField` ของ grid ให้เป็น `eId` ด้วย และทำ fallback ให้ครอบ `eId` แทน `eid`)

### ขอบเขตที่ห้ามทำ
- **ห้ามแก้ API / `LearnerGridRowDto` / JSON policy** (contract เป็น camelCase แล้ว — ถูกต้อง)
- ห้ามแตะ `iLearn.Admin.React` (camelCase อยู่แล้ว)
- ห้ามแก้ filter lookup dropdowns (Division/Department/Section/Position เป็นคนละ store key `Name` — ถ้าพบว่าพังด้วยให้จดใน Implementer Notes แต่อย่าขยายเอง)
- ระวัง `MapFilterFieldNames` ฝั่ง API map filter field `eId`→`EId` ให้ external อยู่แล้ว (case-insensitive) → การเปลี่ยน dataField เป็น `eId` ไม่กระทบ filtering

## Verification
```powershell
# build MVC admin (เผื่อ Razor compile error)
dotnet build iLearn.Admin -o artifacts\verify-plan044
Remove-Item -Recurse -Force artifacts\verify-plan044

# ยืนยันไม่เหลือ PascalCase learner field ใน grid config/JS ของ views ที่ดึง Learners/Get
rg "key: ?[""']EId[""']|dataField: ?[""']EId[""']|\.EId\b|EnglishFirstName|EnglishLastName" iLearn.Admin/Views
# (ควรเหลือเฉพาะที่ไม่เกี่ยว Learners/Get ถ้ามี — ตรวจด้วยตา)
```
- **E2E (จุดชี้ขาด):** เปิด `/admin/Assignments/BulkAssign?courseId=...` ขั้น Learner → ตาราง Learner Directory ต้องโหลด row ได้, ไม่มี E1046/E1040 ใน console, คอลัมน์ ID/Name/Position/Dept/Section แสดงค่า, เลือก learner แล้ว tray/review ขึ้นถูก
- ตรวจหน้าอื่นที่แก้ด้วย: Learners/Index, LearnerGroups/AddMembers, LearnerGroups/Editor, Assignments/Detail

## Implementer Notes
- Updated learner grid/store/js field casing to camelCase in all scoped MVC views:
	- `iLearn.Admin/Views/Assignments/BulkAssign.cshtml`
		- `key: "EId" -> "eId"`
		- learner columns `EId/EnglishFirstName/EnglishLastName/Position/Department/Section` -> `eId/englishFirstName/englishLastName/position/department/section`
		- row cache/review/tray refs `row.EId`, `row.EnglishFirstName`, `row.EnglishLastName`, `row.Position`, `row.Department`, `row.Section` -> camelCase equivalents
	- `iLearn.Admin/Views/Assignments/Detail.cshtml`
		- learner popup store/grid fields switched to `eId`, `englishFirstName`, `englishLastName`, `position`, `department`, `section`
	- `iLearn.Admin/Views/Learners/Index.cshtml`
		- store key `eId`
		- grid columns switched to `eId`, `englishFirstName`, `englishLastName`, `position`, `division`, `department`, `section`
	- `iLearn.Admin/Views/LearnerGroups/AddMembers.cshtml`
		- store/grid fields switched to camelCase
		- selection cache refs `row.EId` and name refs `row.EnglishFirstName/row.EnglishLastName` switched to camelCase
	- `iLearn.Admin/Views/LearnerGroups/Editor.cshtml`
		- store/grid fields switched to camelCase
		- fallback normalized to prioritize `eId` and remove legacy `eid` fallback in scoped points
		- name fallback for grid rows uses `englishFirstName/englishLastName`

- Verification run:
	- `dotnet build iLearn.Admin -o artifacts\verify-plan044` => **passed** (warnings only, no errors)
	- cleanup `artifacts\verify-plan044` attempted after build (folder removed)
	- casing residue check on scoped files:
		- `rg -n -F -e 'key: "EId"' -e 'dataField: "EId"' -e '.EId' -e 'EnglishFirstName' -e 'EnglishLastName' -e 'eid' iLearn.Admin/Views/Assignments/BulkAssign.cshtml iLearn.Admin/Views/Assignments/Detail.cshtml iLearn.Admin/Views/Learners/Index.cshtml iLearn.Admin/Views/LearnerGroups/AddMembers.cshtml iLearn.Admin/Views/LearnerGroups/Editor.cshtml`
		- result: no matches in scope

- E2E browser verification on target URLs was not executed in this run (no live UI session attached). The code-level fix and build verification are complete.

- Additional out-of-scope mismatches found: none in the 5-file scoped search.

## Review Notes (Claude Code, 2026-06-30)

ตรวจอิสระจาก diff จริงทั้ง 5 ไฟล์ + รัน residue grep และ build เอง:

**✅ ถูกต้องครบ:**
- ทั้ง 5 views เปลี่ยน `key`/`dataField`/`calculateCellValue`/JS row refs เป็น camelCase ครบ — ใช้ **`eId` (capital I) ถูกต้อง** ตรงกับ output ของ `JsonNamingPolicy.CamelCase` (ไม่ใช่ `eid` lowercase ที่ผิด)
  - BulkAssign: key + 5 columns + `row.eId/englishFirstName/position/department/section` ใน tray/review/cache
  - Detail, AddMembers: key + columns + name/cache refs
  - Learners/Index: key + 7 columns (รวม `division` ที่หน้าอื่นไม่มี)
  - Editor: key + columns + แก้ fallback chain `row?.EId || row?.eid` → `row?.eId` (เดิมผิดทั้งคู่), คง `Code`/`code` fallback ที่ไม่อันตราย
- residue grep (รันเอง): ไม่เหลือ PascalCase learner field ใน 5 ไฟล์
- ยืนยัน `STUDENTS_API_URL = ${serviceUrl}/Learners` → AddMembers/Editor ดึง `Learners/Get` เดียวกัน → casing fix ครอบคลุมถูก
- ไม่แตะ API/DTO/JSON policy (contract camelCase คงเดิม), ไม่แตะ React — ถูกต้องตาม scope
- **Build `iLearn.Admin` เอง: 0 errors** (Razor compile ผ่าน)
- ไม่มี scope creep (แก้แค่ 5 views; ไฟล์ PLAN-043 ที่ยัง uncommitted เป็นคนละงาน)

**⚠️ เหลือ:** E2E บน browser จริง (implementer เว้นไว้ถูกต้อง — ต้อง deploy `iLearn.Admin` แล้วเปิดหน้า BulkAssign/AddMembers/Editor/Detail/Learners ทดสอบว่าไม่มี E1046/E1040 และ row โหลด/เลือกได้)

**สรุป:** code-level fix ถูกต้องสมบูรณ์ ปรับเป็น VERIFIED — รอ E2E ปิดท้ายหลัง deploy
