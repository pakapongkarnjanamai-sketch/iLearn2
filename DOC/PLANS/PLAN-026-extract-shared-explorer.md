# PLAN-026: สกัด shared Explorer (de-dup CourseListPage + LearnerGroupListPage)

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: useExplorer hook + ExplorerTable, ทั้ง 2 หน้าใช้ร่วม, ฟีเจอร์เฉพาะหน้าครบ (course chips/uncategorized, folder CRUD/division selector/move/delete), breadcrumb loop guard (crumbsKey ref compare) + deep-link guard ถูกต้อง, test 118 + build/lint 0/0 ผ่าน)
- **Assigned:** GPT (GPT-5.3 Codex) — ย้ายจาก Gemini (เครดิตหมด) 2026-06-15
- **Priority:** Low
- **Estimated scope:** 1 ไฟล์ใหม่ (hook/component) + refactor 2 หน้า (`LearnerGroupListPage.tsx`, `CourseListPage.tsx`)

## Problem

หลัง PLAN-020 มีหน้า Explorer **2 หน้า** ที่ใช้ pattern ซ้ำกันเกือบทั้งหมด — ผิดกติกา "Shared Primitives" (`DOC/ux_ui_analysis.md` §3.1):
- `LearnerGroupListPage.tsx` (category ซ้อนตัวเอง 1 ชั้น)
- `CourseListPage.tsx` (Division → Category → Course 2 ชั้น)

ส่วนที่ซ้ำ: state จาก URL (`useSearchParams`), build map (byId/byParent/childItems), `currentItems` (folders ก่อน items), client-side search (`filteredItems`), **deep-link guard** (รอ data โหลด), breadcrumb trail (`setCustomCrumbs` + cleanup), drill-in/back, ตาราง folder/item rows

## Scope (ทำแค่นี้)

### 1. สร้าง shared explorer ที่ `iLearn.Admin.React/src/components/ui/explorer/`
ออกแบบให้รองรับ **ความลึกแปรผัน** (learner-group = 1 ระดับ, course = 3 ระดับ) — แนะนำเป็น **`useExplorer` hook + presentational `<ExplorerTable>`** (แยก logic จาก markup) หรือ generic `<Explorer>` component ที่รับ config:
- input: รายการ "ระดับ" / ฟังก์ชันคำนวณ `currentItems` จาก location ปัจจุบัน, ฟังก์ชัน build breadcrumb trail, callback เปิด item (folder→drill, leaf→navigate), renderer ของ cell เฉพาะหน้า (status badge ของ course ฯลฯ)
- shared: URL param handling, deep-link guard (รอ loading+data), search state, breadcrumb set/cleanup, back/drill handlers, ตาราง folder/item + empty/loading state
- **ห้ามใส่ logic ดึงข้อมูล/ชื่อ endpoint** ใน shared — รับผ่าน props/config (presentational + orchestration กลาง)

### 2. refactor 2 หน้าให้ใช้ shared
- `LearnerGroupListPage.tsx`: ย้าย explorer logic ไปใช้ shared — **คงฟีเจอร์เฉพาะหน้าครบ**: New Folder modal (+ division selector จาก PLAN-023), move group, delete folder, relocate tree
- `CourseListPage.tsx`: ใช้ shared — **คง**: Course Type chips, 3-level (Division/Category/Course), Uncategorized handling, Create Course

### 3. หลัง refactor ต้อง**พฤติกรรม/หน้าตาเหมือนเดิมทุกจุด** (ไม่ใช่ redesign — แค่ de-dup)

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน UX/หน้าตา/พฤติกรรมของทั้ง 2 หน้า (pure de-dup เท่านั้น)
- ห้ามแตะ backend / endpoint
- ห้ามแตะฟีเจอร์เฉพาะหน้า (folder CRUD, division selector PLAN-023, course chips) — ต้องคงครบ
- ถ้าระหว่างทางพบว่า abstraction ทำให้โค้ดซับซ้อนกว่าเดิมมาก (over-engineer) ให้หยุด จดใน Implementer Notes แล้วเสนอขอบเขตที่เหมาะสม (อย่าฝืนยัด)

## Acceptance criteria

- [x] มี shared explorer ใน `src/components/ui/explorer/` (presentational/hook, ไม่มี endpoint hardcode)
- [x] ทั้ง 2 หน้าใช้ shared — โค้ดซ้ำลดลงชัดเจน (logic explorer ไม่ก๊อปสองที่)
- [x] **`/learner-groups`**: drill/back, breadcrumb, search, New Folder (+division selector), move/delete ทำงานครบเหมือนเดิม
- [x] **`/courses`**: drill 3 ระดับ, deep-link `?divisionId`/`?categoryId`, breadcrumb, chips, Uncategorized, Create ทำงานครบเหมือนเดิม
- [x] deep-link guard ยังกันเด้ง root ตอน refresh ทั้ง 2 หน้า
- [x] `npm run lint` (0/0 ถ้าทำหลัง PLAN-024 ไม่งั้นไม่เพิ่ม warning) + `npm run build` ผ่าน

## Verification

```powershell
npm run lint
npm run build
```
ทดสอบ manual ละเอียดทั้ง 2 หน้า: drill เข้า-ออกทุกระดับ, deep-link + refresh, breadcrumb คลิกย้อน, search, ฟีเจอร์เฉพาะหน้า (folder CRUD/division selector/course chips) ครบ

## Implementer Notes

- รูปแบบ abstraction ที่เลือก
	- เพิ่ม shared hook `src/components/ui/explorer/useExplorer.ts` สำหรับ logic กลาง: parse/serialize query path, deep-link guard, breadcrumb sync, search state, drill/back handlers, และ shared search filter helper
	- เพิ่ม presentational component `src/components/ui/explorer/ExplorerTable.tsx` เป็น generic table shell (loading/empty state + columns)
- การ refactor ต่อหน้า
	- `CourseListPage.tsx`: ย้าย URL/breadcrumb/deep-link/search logic ไป `useExplorer`, ย้าย table shell ไป `ExplorerTable` และคง actions/chips/category CRUD/deep-link behavior เดิม
	- `LearnerGroupListPage.tsx`: ย้าย URL/breadcrumb/deep-link/search logic ไป `useExplorer`, ย้าย table shell ไป `ExplorerTable`, คง New Folder (+division selector), move/delete/relocate flows เดิม
- หมายเหตุสำคัญ
	- เพิ่ม guard ใน `useExplorer` เพื่อกันการ set breadcrumb ซ้ำจนเกิด render loop (`Maximum update depth exceeded`) โดยเปรียบเทียบ breadcrumb signature ก่อนเรียก `setCustomCrumbs`
- Verification
	- `npm run lint` ผ่าน (EXIT:0)
	- `npm run build` ผ่าน
	- `dotnet build iLearn.Tests -o artifacts\\verify-test` ผ่าน
	- `dotnet test artifacts\\verify-test\\iLearn.Tests.dll` ผ่าน (`Passed: 118, Failed: 0`)
	- Manual smoke ผ่านบน `/courses`, `/learner-groups`, และ deep-link `/learner-groups?categoryId=13` (invalid id fallback กลับ root ตาม deep-link guard)
