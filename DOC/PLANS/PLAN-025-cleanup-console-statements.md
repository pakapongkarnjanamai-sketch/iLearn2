# PLAN-025: เก็บกวาด console.* ที่ค้างใน frontend (63 จุด / 18 ไฟล์)

- **Status:** DONE
- **Assigned:** GPT
- **Priority:** Low
- **Estimated scope:** ~18 ไฟล์ frontend (แก้เฉพาะบรรทัด console)

## Problem

มี `console.log/error/warn/debug` ค้างใน `iLearn.Admin.React/src` รวม **63 จุดใน 18 ไฟล์** — ปนกันระหว่าง debug cruft (`console.log`) กับ error logging ใน catch (`console.error`) ทำให้ console รก + หลุด debug ขึ้น production

ไฟล์ที่มี (จาก grep): `LearnerGroupDetailPage`(8), `AssignmentDetailPage`(7), `CourseDetailPage`(7), `LearnerGroupListPage`(5), `VersionFormPage`(5), `CourseListPage`(4), `BulkAssignPage`(3), `CourseEditorPage`(3), `LearnerGroupEditorPage`(3), `MasterDataDetailPage`(3), `UserEditorPage`(3), `SystemConfigPage`(2), `LearnerDirectorySelector`(5), และไฟล์ละ 1: `ContentItemEditorPage`, `DashboardPage`, `EntityListPage`, `LearnerProfilePage`, `LearnerGroupCategoryEditorPage`

## Scope (ทำแค่นี้)

ไล่ทุก console.* แล้วจัดการตามชนิด:
1. **`console.log` / `console.debug`** (debug cruft) → **ลบทิ้ง**
2. **`console.error` / `console.warn` ใน `catch`** → **เก็บไว้** (เป็น error logging ที่มีประโยชน์) แต่ต้องมั่นใจว่า**คู่กับ user-facing feedback** อยู่แล้ว (เช่น `toast.error(...)`) — ส่วนใหญ่มีแล้ว ถ้าจุดไหน catch แล้ว `console.error` เฉย ๆ ไม่มี toast → เพิ่ม `toast.error` ที่สื่อความ (อย่ากลืน error เงียบ)
3. **`console.*` นอก catch ที่ไม่ใช่ debug ชั่วคราว** → พิจารณาเป็นรายกรณี (ปกติลบ)

**กติกา:** ไม่เปลี่ยน logic การทำงาน/การจัดการ error — แค่เอา debug noise ออก + กันไม่ให้มี catch ที่เงียบ (ไม่มีทั้ง log และ toast)

> หมายเหตุ: ไม่ต้องตั้ง eslint rule `no-console` ในงานนี้ (เป็น scope แยก) — แค่เก็บกวาดของที่มี

## Out of scope (ห้ามแตะ)

- ห้ามแก้ logic/flow อื่นนอกจากบรรทัด console + เพิ่ม toast เฉพาะ catch ที่เงียบ
- ห้ามแตะ lint deps warnings (PLAN-024)
- ห้ามเพิ่ม logging library ใหม่
- ห้ามแตะ console ในไฟล์ backend (.cs) — งานนี้ frontend เท่านั้น

## Acceptance criteria

- [x] ไม่มี `console.log` / `console.debug` เหลือใน `src` (grep = 0)
- [x] `console.error`/`warn` ที่เหลือ อยู่ใน catch และมี user-facing feedback คู่กัน
- [x] ไม่มี catch ที่เงียบสนิท (ไม่มีทั้ง log และ toast)
- [x] `npm run lint` (ไม่เพิ่ม warning) + `npm run build` ผ่าน

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
grep ยืนยัน: `console\.(log|debug)` ใน src = 0; smoke เปิดหน้าหลัก ๆ ดูว่า error path ยังขึ้น toast

## Implementer Notes

- ผลลัพธ์การเก็บกวาด
	- `console.log` / `console.debug`: เหลือ 0 จุดใน `iLearn.Admin.React/src` (ยืนยันด้วย `rg 'console\.(log|debug)' ...`)
	- ลบ `console.error` นอก catch 1 จุด: `DashboardPage` (`ChartErrorBoundary.componentDidCatch`)
	- เก็บ `console.error` ใน catch ที่มีประโยชน์ไว้ และเติม user-facing feedback (`toast.error`) ให้ครบจุดที่เคยเงียบ
- จุดที่เติม `toast.error`
	- `src/components/shared/LearnerDirectorySelector.tsx`: 3 จุด (`loadInit`, `loadDepts`, `loadSections`)
	- `src/pages/EntityListPage.tsx`: 1 จุด (division lookup `.catch`)
	- `src/pages/courses/VersionFormPage.tsx`: 2 จุด (course label fetch `.catch`, `loadVersionImpact`)
	- `src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`: 1 จุด (division lookup)
	- `src/pages/master-data/MasterDataDetailPage.tsx`: 2 จุด (`handleSave`, `handleDelete`)
- Verification
	- Frontend: `npm run lint` ผ่าน (11 warnings baseline เท่าเดิม), `npm run build` ผ่าน
	- Backend tests: `dotnet test artifacts\\verify-test\\iLearn.Tests.dll` ผ่าน (`Passed: 118, Failed: 0`)
