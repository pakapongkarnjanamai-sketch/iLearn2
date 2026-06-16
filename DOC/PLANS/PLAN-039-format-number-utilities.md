# PLAN-039: เพิ่ม `formatNumber` / `formatPercent` / `formatBytes` ใน format.ts

- **Status:** VERIFIED ✅ (Claude Code review 2026-06-16 — ดู follow-up PLAN-040 เรื่อง formatPercent precision)
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** Medium
- **Estimated scope:** ขยาย `src/lib/format.ts` + ย้าย formatter ที่เขียนเองในหน้า ~3 ไฟล์มาใช้ของกลาง

## Problem

`src/lib/format.ts` มีแค่ `formatDate` / `formatDateTime` ยัง **ไม่มีฟอร์แมตตัวเลข** ทำให้แต่ละหน้าฟอร์แมตเอง ไม่สม่ำเสมอ และตัวเลขจำนวนมากแสดงเป็นเลขดิบไม่มีตัวคั่นหลักพัน:
- [DashboardPage.tsx:59](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/DashboardPage.tsx#L59), [684](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/DashboardPage.tsx#L684) — เปอร์เซ็นต์ด้วย `n.toFixed(...)` เขียนเอง
- [ContentItemDetailPage.tsx:49-58](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx#L49) — `fmtBytes()` helper เฉพาะหน้า (B/KB/MB/GB)
- [ContentItemEditorPage.tsx:200](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx#L200), [255](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx#L255) — `Math.round(file.size / 1024) KB` เขียนเองอีกแบบ (ผลต่างจาก fmtBytes ข้างบน)

> เชื่อมกับ [datagrid_skill_gap_analysis.md](file:///c:/Users/n4734/source/repos/iLearn2/DOC/datagrid_skill_gap_analysis.md): gap จริงที่ฟิตกับ iLearn2 คือ **number formatting (thousands separator)** ที่ตอนนี้ยังแสดงเลขดิบ — แผนนี้คือการวางรากฐานนั้น

## Scope (ทำแค่นี้)

### 1. เพิ่มฟังก์ชันใน `src/lib/format.ts`
ใช้ `Intl.NumberFormat('en-GB')` แคชเป็น module-level (เหมือน date formatter เดิม) คืน `'-'`/`'—'` เมื่อ null/undefined ให้สอดคล้องกับ `formatDate`:

```ts
// formatNumber(1234567) -> "1,234,567" ; null -> "-"
export const formatNumber = (value: number | null | undefined) => { ... }

// formatPercent(87.5) -> "88%" ; formatPercent(87.5, 1) -> "87.5%"
export const formatPercent = (value: number | null | undefined, fractionDigits = 0) => { ... }

// formatBytes(1536) -> "1.5 KB" ; 0/null -> "—"
export const formatBytes = (bytes: number | null | undefined) => { ... }
```
- `formatBytes` ยกตรรกะจาก `fmtBytes` ที่ [ContentItemDetailPage.tsx:49](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx#L49) มาเป็นมาตรฐานกลาง (units B/KB/MB/GB, ปัดทศนิยมแบบเดิม)
- รักษา convention คืนค่าว่างให้ตรงกับของเดิมในแต่ละจุด (ดูข้อ 2 ก่อนเปลี่ยน sentinel)

### 2. ย้าน call site มาใช้ของกลาง
- `ContentItemDetailPage` — ลบ `fmtBytes` local แล้วใช้ `formatBytes`
- `ContentItemEditorPage:200,255` — ใช้ `formatBytes(file.size)` แทน `Math.round(.../1024) KB` (รวมการแสดงผลขนาดไฟล์ให้เป็นแบบเดียวทั้งแอป)
- `DashboardPage:59,684` — ใช้ `formatPercent` (ตรวจ sentinel เดิม: บรรทัด 59 คืน `'—'` em-dash — คง behaviour เดิม)
- เผื่อพบจุดอื่นที่โชว์ count/ตัวเลขดิบ (เช่น KPI, "X items") **ที่ควรมีตัวคั่นหลักพัน** ให้เปลี่ยนเป็น `formatNumber` — แต่ **เฉพาะตัวเลขที่เป็นปริมาณจริง** ห้ามแตะ ID/รหัส/เลขเวอร์ชัน/ลำดับแถว (index) ที่ไม่ควรมี comma

### ขอบเขตที่ห้ามทำ
- ห้ามเปลี่ยน locale เป็นอย่างอื่นนอก `en-GB` (ให้ตรงกับ date formatter เดิม)
- ห้ามใส่ comma กับเลขที่ไม่ใช่ปริมาณ (รหัส/เวอร์ชัน/index)
- ห้ามแตะ backend / `iLearn.Admin` (MVC)

---

## Verification
```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
# ยืนยันว่าไม่มี byte/percent formatter เขียนมือหลงเหลือในหน้า
rg "toFixed\(|/ 1024|fmtBytes" src/pages
```
- เปิดด้วยตา: Content-item detail (ขนาดไฟล์), Content-item editor (อัปโหลด แสดงขนาด), Dashboard (เปอร์เซ็นต์/KPI) — ตัวเลขถูกต้อง มี comma ที่ควรมี

## Implementer Notes
- เปลี่ยนจุด count/number เพิ่มเติมเป็น `formatNumber` ที่:
	- `DashboardPage` (KPI + ตาราง/subtitle ที่เป็นปริมาณ)
	- `ContentItemDetailPage` ช่อง `Courses Linked`
- จุดที่ตั้งใจไม่แตะเพราะเป็นรหัส/index: `File Storage Id`, รหัสคอร์ส/รหัสอื่น และลำดับแถวทั้งหมด (ไม่ใส่ comma)
- ย้าย formatter ตาม scope:
	- `DashboardPage`: ลบ local `formatNumber/formatPercent` และใช้จาก `src/lib/format.ts`, รวม `CompletionBar` ที่เคย `toFixed(0)`
	- `ContentItemDetailPage`: ลบ `fmtBytes` local และใช้ `formatBytes`
	- `ContentItemEditorPage`: ลบ `Math.round(file.size / 1024) KB` ทั้ง 2 จุดและใช้ `formatBytes(file.size)`
- Verification:
	- `npm run lint` ผ่านในระดับไม่มี error (มี warning เดิมนอก scope ที่ `src/pages/learners/LearnerProfilePage.tsx`)
	- `npm run build` **ไม่ผ่าน** เนื่องจาก issue นอก scope ที่ `src/components/ui/Card.tsx` (`exactOptionalPropertyTypes` ของ prop `icon`)
	- `rg "toFixed\(|/ 1024|fmtBytes" src/pages` ไม่พบผลลัพธ์
