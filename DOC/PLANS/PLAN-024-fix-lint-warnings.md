# PLAN-024: แก้ lint warnings ทั้ง 11 ตัว (react-hooks/exhaustive-deps) ให้เหลือ 0

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: lint 0/0 — group A useCallback loaders, group B steps→plain array (rebuild ทุก render, ไม่กระทบ behavior), test 118 + build ผ่าน)
- **Assigned:** GPT
- **Priority:** Low
- **Estimated scope:** ~9 ไฟล์ frontend (เฉพาะจุดที่มี warning)

## Problem

`npm run lint` มี **11 warnings** (0 errors) ทั้งหมดเป็น `react-hooks/exhaustive-deps` — เป็น baseline ที่สะสมมานาน ทำให้ warning จริงใหม่ ๆ กลบหายในกองนี้ เป้าหมาย: เคลียร์ให้เหลือ **0 warnings** โดย**ไม่เปลี่ยนพฤติกรรม**

แบ่งเป็น 2 กลุ่ม:

**กลุ่ม A — useEffect ขาด data-loader dep (6 จุด):** `loadAssignmentDetails`, `loadLookups`, `loadVersionImpact`, `loadVersionData`, `loadProfile`, `loadItem`
- ไฟล์: `AssignmentDetailPage.tsx:163`, `BulkAssignPage.tsx:136` (loadLookups), `VersionFormPage.tsx:247,251`, `LearnerProfilePage.tsx:91`, `MasterDataDetailPage.tsx:91`

**กลุ่ม B — useMemo `steps` ของ wizard ขาด render/validate fns (5 จุด):** `BulkAssignPage.tsx:541`, `ContentItemEditorPage.tsx:287`, `CourseEditorPage.tsx:709`, `VersionFormPage.tsx:579`, `UserEditorPage.tsx:337`

## Scope (ทำแค่นี้)

**กลุ่ม A (วิธีสะอาด):** wrap loader function ด้วย `useCallback([...deps จริง])` แล้วใส่ loader ลงใน dependency array ของ useEffect → warning หาย + พฤติกรรมเดิม (loader stable)
- ระวัง: deps ของ useCallback ต้องครบจริง (id/params ที่ loader ใช้) ไม่งั้นจะโหลดด้วยค่าเก่า

**กลุ่ม B (wizard steps):** การใส่ render/validate fns เป็น deps จะทำให้ `steps` ถูกสร้างใหม่ทุก render (เสียจุดประสงค์ memo) เพราะ fns เหล่านั้นนิยามใหม่ทุก render — เลือก**ทางใดทางหนึ่งต่อไฟล์**:
- (แนะนำถ้าทำได้สะอาด) wrap render/validate fns ด้วย `useCallback` แล้วใส่เป็น deps
- (ยอมรับได้) ใส่ `// eslint-disable-next-line react-hooks/exhaustive-deps` เหนือ dependency array **พร้อมคอมเมนต์อธิบาย** ว่า steps ตั้งใจ rebuild เฉพาะเมื่อ state หลักเปลี่ยน (เช่น `isCreate`, `form`, `file`, `currentStep`) — อย่าใส่ disable ลอย ๆ ไม่มีเหตุผล

**กติกาเหล็ก:** ทุกการแก้ต้อง**ไม่เปลี่ยนพฤติกรรม** — wizard เดินครบ step, หน้า detail โหลดข้อมูลถูก, ไม่เกิด infinite re-render/refetch loop (ทดสอบเปิดแต่ละหน้าจริง)

## Out of scope (ห้ามแตะ)

- ห้าม refactor logic อื่นนอกจากแก้ deps/memoization
- ห้ามแตะไฟล์ที่ไม่มี warning
- ห้าม disable rule ระดับ config/ไฟล์ (ใช้ inline disable เฉพาะบรรทัด + คอมเมนต์เท่านั้น)
- ห้ามแก้ console.* (อยู่ใน PLAN-025)

## Acceptance criteria

- [x] `npm run lint` = **0 errors, 0 warnings**
- [x] ทุกหน้าที่แก้ยังทำงานเหมือนเดิม (wizard ครบ step, detail โหลดข้อมูล, ไม่มี refetch loop)
- [x] inline eslint-disable ทุกจุด (ถ้ามี) มีคอมเมนต์อธิบายเหตุผล
- [x] `npm run build` ผ่าน

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint   # ต้อง 0/0
npm run build
```
ทดสอบ manual: เปิด `/assignments/:id`, `/assignments/bulk`, `/courses/:id/version/...`, `/learners/:id/profile`, `/master-data/divisions/:id`, wizard create ของ content/course/user — ดูว่าทำงานปกติ ไม่ค้าง ไม่โหลดวน

## Implementer Notes

- วิธีที่ใช้จริง
	- กลุ่ม A (loader/useEffect): ใช้ `useCallback` + เปลี่ยน `useEffect` ให้ depend ที่ callback โดยตรง
		- `AssignmentDetailPage`: `loadAssignmentDetails`
		- `BulkAssignPage`: `loadLookups`
		- `VersionFormPage`: `loadContentLibrary`, `loadVersionImpact`, `loadVersionData`
		- `LearnerProfilePage`: `loadProfile`
		- `MasterDataDetailPage`: `loadItem`
	- กลุ่ม B (wizard steps): เลือกถอด `useMemo` เฉพาะตัวแปร `steps` ออกให้เป็น array ปกติใน 5 ไฟล์ (`BulkAssignPage`, `ContentItemEditorPage`, `CourseEditorPage`, `VersionFormPage`, `UserEditorPage`) เพื่อตัด warning `react-hooks/exhaustive-deps` แบบไม่เปลี่ยน flow ของ wizard
- eslint-disable
	- ไม่ใช้ `eslint-disable` เพิ่มในงานนี้
- Verification
	- `npm run lint` ผ่าน: 0 errors, 0 warnings
	- `npm run build` ผ่าน
	- `dotnet test artifacts\\verify-test\\iLearn.Tests.dll` ผ่าน: Passed 118, Failed 0
	- smoke route (Playwright) ผ่าน: `/assignments/bulk`, `/content-library/new`, `/courses/new`, `/users/new`, `/assignments/264`, `/master-data/divisions/1`
