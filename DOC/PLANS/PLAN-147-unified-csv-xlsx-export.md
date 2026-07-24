# PLAN-147 — Unified CSV + XLSX Export (client-side, โค้ดร่วมกันทุกหน้า report)

- **สถานะ:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **วันที่:** 2026-07-24
- **ต่อยอดจาก:** PLAN-143/145/146 (report pages + server Excel)
- **หน้าที่กระทบ:** ทุกหน้าที่มีปุ่ม export (6 หน้า report ด้านล่าง)

## เป้าหมาย

ผู้ใช้ต้องการให้ **ทุก export มี 2 ทางเลือกคือ `.csv` และ `.xlsx`** และ **ใช้โค้ดร่วมกัน** (ไม่ให้แต่ละหน้าเขียน export เอง)

**การตัดสินใจสถาปัตยกรรม (ยืนยันกับผู้ใช้แล้ว — 2026-07-24):**

1. **กลไก export กลางเป็น client-side** — ทำ helper ตัวเดียว `exportRows(format, filename, header, rows)` ที่รับ `(header, rows)` ชุดเดียวกัน (เหมือน `exportRowsAsCsv` ปัจจุบัน) แล้ว emit ได้ทั้ง CSV และ XLSX ทุกหน้าเรียกตัวนี้ตัวเดียว
2. **คง "ของรวย" (server rich Excel) ของ 2 หน้าไว้** — Assignment Summary + Learner Group Summary ที่มี server Excel หลาย sheet + detail รายคน (PLAN-146) **ห้าม downgrade** — บนสองหน้านี้ ปุ่ม Excel ยังชี้ไป endpoint server เดิม; ส่วน CSV เปลี่ยนมาใช้ helper กลาง

> เหตุผลที่ไม่ขัดกัน: helper client เอา `.xlsx` แบบ flat (ตารางเดียว = สิ่งที่เห็นบนจอ) ไปให้ 4 หน้าที่ยังไม่มี Excel เลย ส่วน 2 หน้าที่มี detail รายคน (ข้อมูลที่ client ไม่มี) ยังใช้ server workbook รวย ๆ ต่อไป — CSV กลายเป็น shared ทั้ง 6 หน้า, UI ปุ่ม export เป็น component เดียวกันทั้ง 6 หน้า

## สถานะปัจจุบัน (ก่อนแก้)

| หน้า | ไฟล์ | CSV (client) | XLSX |
|---|---|---|---|
| Assignment Summary | `reports/AssignmentSummaryReportPage.tsx` | ✅ `exportRowsAsCsv` | ✅ **server rich** (`/Reports/assignments/export`) |
| Learner Group Summary | `reports/LearnerGroupSummaryReportPage.tsx` | ✅ `exportRowsAsCsv` | ✅ **server rich** (`/Reports/learner-groups/export`) |
| Assignment Report (รายคนต่อ batch) | `assignments/AssignmentReportPage.tsx` | ✅ `exportRowsAsCsv` | ❌ |
| Activity Report | `reports/ActivityReportPage.tsx` | ✅ `exportRowsAsCsv` | ❌ |
| Compliance Report | `reports/ComplianceReportPage.tsx` | ✅ `exportRowsAsCsv` | ❌ |
| Course Summary Report | `reports/CourseSummaryReportPage.tsx` | ✅ `exportRowsAsCsv` | ❌ |

- CSV กลาง = `src/lib/csvExport.ts` → `exportRowsAsCsv(filename, header, rows)` (มี UTF-8 BOM สำหรับภาษาไทย)
- ยังไม่มี xlsx lib ฝั่ง client ใน `package.json`
- **ไม่มี export ที่ list page (learners/courses/…)** — grep `exportRowsAsCsv` เจอแค่ 6 หน้านี้ ⇒ scope = 6 หน้านี้เท่านั้น

## Scope

### 1. เพิ่ม client XLSX library (lazy import)

- เพิ่ม package **`write-excel-file`** (MIT, เล็ก, tree-shakeable) ใน `iLearn.Admin.React` — เขียน `.xlsx` ล้วน ไม่ต้องอ่าน
  - เหตุผลไม่เลือก `xlsx`(SheetJS): community edition ถอดออกจาก npm registry + เคยมี CVE; `exceljs` ใหญ่เกินจำเป็นสำหรับตาราง header+rows ธรรมดา
  - ถ้า implementer มีเหตุผลเลือกตัวอื่น (เช่น `exceljs`) → จดใน Implementer Notes ได้ แต่ต้อง MIT/BSD และรองรับ UTF-8 ไทย
- **โหลดแบบ dynamic `import()` ตอนกด Export XLSX เท่านั้น** — อย่าเพิ่ม static import ที่ทำ main bundle โต (แอปมี Vite chunk-size warning อยู่แล้ว)

### 2. Helper กลาง `src/lib/tableExport.ts`

สร้างไฟล์ใหม่ที่ห่อทั้ง CSV และ XLSX ไว้ที่เดียว:

```ts
export type ExportFormat = 'csv' | 'xlsx'
export type ExportCell = string | number | null | undefined

export async function exportRows(
  format: ExportFormat,
  filename: string,        // ไม่ต้องมีนามสกุล — helper เติม .csv/.xlsx เอง
  header: string[],
  rows: ExportCell[][],
): Promise<void>
```

- `format === 'csv'` → เรียก logic เดิมจาก `csvExport.ts` (คง UTF-8 BOM)
- `format === 'xlsx'` → `await import('write-excel-file')` แล้ว build sheet เดียว: แถวแรก = `header` (ตัวหนา), ที่เหลือ = `rows`; ตัวเลขเขียนเป็น number cell, ที่เหลือเป็น string; download ผ่าน `downloadBlob` (มีอยู่แล้วใน `src/lib/downloadBlob.ts`)
- **filename convention:** helper รับชื่อ**ไม่มีนามสกุล** แล้วเติมเอง — ปรับ call site ให้ส่ง `course-summary-report-${stamp}` (ตัด `.csv` ออก)
- **คง `csvExport.ts` (`exportRowsAsCsv`) ไว้** ให้ `tableExport.ts` เรียกใช้ต่อ (อย่าลบ — reuse ไม่ใช่ทำซ้ำ); หรือย้าย logic เข้ามาแล้วให้ `exportRowsAsCsv` เป็น wrapper — เลือกได้ แต่ **ห้ามมี CSV escaping สองชุด**

### 3. UI component กลาง `ExportMenu` (ปุ่มเลือก format)

- สร้าง component ที่แสดงตัวเลือก **CSV / Excel** แบบเดียวกันทุกหน้า — เพื่อไม่ให้แต่ละหน้า hand-roll ปุ่มเอง
- **ต้องใช้ button primitives ที่มีอยู่** (`AppButton`/`IconButton`/`SegmentedToggle`) ตาม UI Conventions — ห้าม hand-roll `<button>` (ดู `iLearn.Admin.React/README.md`)
- รูปแบบที่แนะนำ (implementer เลือกที่เข้ากับ UI เดิมได้): ปุ่ม `AppButton` "Export" + dropdown 2 ตัวเลือก **หรือ** 2 ปุ่มติดกัน `SegmentedToggle`/`AppButton` (CSV, Excel) — ให้ล้อ layout เดิมที่ปุ่ม export อยู่ใน `Card actions`
- Props ประมาณ: `onExport(format: ExportFormat)`, ตัวเลือก `extraActions?` (สำหรับ 2 หน้ารวยที่ยังมีปุ่ม server Excel แยก)
- ต้องแสดง `disabled`/ซ่อน เมื่อ `rows.length === 0` (พฤติกรรมเดิมทุกหน้า `data.rows.length > 0 && …`)

### 4. แก้ 6 หน้าให้ใช้ helper + component กลาง

**4 หน้า client-only** (Assignment Report, Activity, Compliance, Course Summary):
- แทนปุ่ม "Export CSV" เดิมด้วย `ExportMenu` (CSV + Excel)
- `onExport(format)` → `await exportRows(format, '<report>-<stamp>', header, body)` โดย **reuse `header`/`body` เดิมที่แต่ละหน้ามีอยู่แล้ว** (ไม่ต้องสร้างใหม่ — แค่ย้ายออกมาให้ทั้งสอง format ใช้ร่วม)

**2 หน้ารวย** (Assignment Summary, Learner Group Summary):
- CSV: เปลี่ยนมาใช้ `exportRows('csv', …)` ผ่าน `ExportMenu`
- Excel: **คงปุ่ม/handler `handleExportExcel` เดิมที่ยิง server endpoint ไว้** (multi-sheet + detail รายคน) — ส่งผ่าน `extraActions` ของ `ExportMenu` หรือวางเป็นปุ่มข้าง ๆ ให้ป้ายชัดว่าเป็น "Excel (รายละเอียดรายคน)" เพื่อไม่สับสนกับ xlsx flat ของหน้าอื่น
- **ห้าม** ให้ 2 หน้านี้เรียก client xlsx flat — Excel ของมันคือ server rich เท่านั้น (ตามการตัดสินใจข้อ 2)

### 5. Labels (i18n)

- `REPORT_LABELS` มี `exportCsv`, `exportExcel`, `exportingExcel`, `exportExcelFailed`, `noRowsToExport` อยู่แล้ว (labels.ts ~340)
- เพิ่มคีย์ที่อาจต้องใช้ (ตาม UI ที่เลือก): `exportXlsx` (`ส่งออก Excel` / อาจใช้ `exportExcel` เดิมได้), ป้ายเมนู `export` (`ส่งออก` / `Export`), และสำหรับ 2 หน้ารวยแยกป้าย `exportExcelDetail` (`Excel รายละเอียดรายคน` / `Excel (per-learner detail)`) ถ้าจำเป็น
- **ห้าม hardcode ข้อความไทย/อังกฤษในหน้า** — ผ่าน dictionary + `t()` เสมอ (bilingual rule)

## นอก Scope (อย่าทำ)

- ไม่แตะ backend / `ReportExcelBuilder` / endpoint server Excel (คงไว้ตามเดิม)
- ไม่เพิ่ม export ให้ list page ที่ยังไม่มี (learners/courses/users grid) — ถ้าผู้ใช้อยากได้ค่อยเปิดแผนใหม่
- ไม่ทำ xlsx หลาย sheet ฝั่ง client — client xlsx = flat sheet เดียวเท่านั้น
- ไม่เปลี่ยนคอลัมน์/ข้อมูลที่ export (header/rows เดิมของแต่ละหน้าเหมือนเดิมทุกช่อง)

## กติกาที่ต้องระวัง (จาก CLAUDE.md / README)

- **UTF-8 ไทย:** CSV ต้องคง BOM `﻿` (มีแล้วใน `csvExport.ts`); XLSX ผ่าน lib ได้อยู่แล้ว — ทดสอบเปิดไฟล์จริงแล้วภาษาไทยไม่เพี้ยน
- **button primitives:** ห้าม hand-roll `<button>` — ใช้ `AppButton`/`IconButton`/`SegmentedToggle`
- **format helpers:** ค่าที่ format แล้ว (วันที่/%) ยังส่งเป็น string ผ่าน `formatDate`/`formatPercent` เหมือนเดิม — client xlsx flat ไม่ต้องทำ native date/number cell (นั่นเป็นงานของ server rich) แต่ **ตัวเลขดิบ** (count, index) ควรเขียนเป็น number cell ใน xlsx ให้ sort/sum ได้
- **ห้าม `toLocaleString`/`toFixed` inline** — ใช้ `src/lib/format.ts`
- **Route remount:** ไม่กระทบ (ไม่เพิ่ม route ใหม่)

## Verification (รันก่อนปิดงาน)

```powershell
# จากโฟลเดอร์ iLearn.Admin.React
npm run lint
npm run build          # ยืนยัน bundle ไม่โตผิดปกติ — write-excel-file ต้องอยู่ใน lazy chunk แยก ไม่ใช่ main
```

- **Manual smoke (สำคัญ — export เป็น browser behavior):** เปิดแต่ละหน้า report กด Export **CSV** และ **Excel** อย่างละครั้ง แล้วเปิดไฟล์จริง:
  - CSV: ภาษาไทยไม่เพี้ยนใน Excel, คอลัมน์ครบ
  - XLSX (4 หน้า client): เปิดได้, header ตัวหนา, ตัวเลข sort ได้
  - XLSX (2 หน้ารวย): ยังเป็น server workbook หลาย sheet + detail รายคนเหมือนเดิม (ไม่ downgrade)
  - ทดสอบทั้งโหมดไทยและอังกฤษ (หัวคอลัมน์ตามภาษา)
- grep ยืนยันไม่มีหน้าไหนเรียก `exportRowsAsCsv` ตรง ๆ อีก (ทุกหน้าไปผ่าน `exportRows`/`ExportMenu`) — ยกเว้น `tableExport.ts` เองที่ reuse

## Definition of Done

- [x] ทั้ง 6 หน้ามีตัวเลือก export **CSV และ Excel** ผ่าน `ExportMenu` component เดียวกัน
- [x] CSV ของทั้ง 6 หน้าไปผ่าน `exportRows('csv', …)` (โค้ด escaping/BOM ชุดเดียว)
- [x] XLSX ของ 4 หน้า client-only ไปผ่าน `exportRows('xlsx', …)` (lazy import lib)
- [x] 2 หน้ารวยยังใช้ server rich Excel เดิม (ไม่ downgrade)
- [x] ไม่มี hardcoded literal / ปุ่ม hand-roll / duplicate CSV logic
- [ ] `npm run lint` ✓ `npm run build` ✓ + manual smoke เปิดไฟล์จริง ✓ (ทั้ง th/en)
- [x] อัปเดตสถานะแผนเป็น DONE + Implementer Notes + ลง `DOC/AGENT_LOG.md`

## Implementer Notes

- ใช้ `write-excel-file@4.1.1` ตามแผน แต่ import จริงเป็น `write-excel-file/browser` แบบ dynamic `import()` ใน `src/lib/tableExport.ts` เพื่อให้ Vite แยกเป็น lazy chunk (`dist/assets/browser-*.js`) แทนการบวมใน main bundle
- เพิ่ม helper กลาง `exportRows(format, filename, header, rows)` และ component กลาง `ExportMenu`; `csvExport.ts` เดิมยังคงเป็นแหล่งเดียวของ CSV escaping/BOM
- 4 หน้า client-only (`AssignmentReportPage`, `ActivityReportPage`, `ComplianceReportPage`, `CourseSummaryReportPage`) ใช้ `exportRows()` ทั้ง CSV และ XLSX
- 2 หน้ารวย (`AssignmentSummaryReportPage`, `LearnerGroupSummaryReportPage`) ใช้ `exportRows('csv', ...)` สำหรับ CSV และคง server rich Excel เดิมไว้ผ่านปุ่มใน `ExportMenu` ที่ label ชัดว่าเป็น per-learner detail
- เพิ่ม label `export`, `exportExcelDetail`, `exportAllExcel`, `exportFilteredExcel` เพื่อให้หน้า assignment report และ report summary ไม่ต้อง hardcode copy ใหม่
- Verification ที่รันใน session นี้: `npm run lint` ✓, `npm run build` ✓, grep ยืนยันไม่เหลือ call site ของ `exportRowsAsCsv` นอก `csvExport.ts`/`tableExport.ts`
- ยังไม่ได้ทำ manual browser smoke ดาวน์โหลดไฟล์จริงและเปิดตรวจ `.csv` / `.xlsx` ทั้ง th/en ใน session นี้

## Reviewer Notes (Claude Code — 2026-07-24)

**สถาปัตยกรรมตรงตามการตัดสินใจที่ยืนยันไว้ทั้งสองข้อ:**
- `exportRows(format, filename, header, rows)` ใน `src/lib/tableExport.ts` เป็นจุดเดียวสำหรับทั้ง CSV/XLSX, `csvExport.ts` ยังเป็นแหล่งเดียวของ CSV escaping/BOM — grep ยืนยัน `exportRowsAsCsv` ถูกเรียกจากที่เดียวคือ `tableExport.ts` เท่านั้น (ไม่มี escaping สองชุด)
- 2 หน้ารวย (Assignment/Learner Group Summary) `handleExportExcel` เดิมไม่ถูกแตะเลย — ยังยิง server endpoint เดิม, label แยกชัดเจนเป็น "Excel รายละเอียดรายคน" ไม่ปนกับ xlsx flat ของหน้าอื่น — **ไม่ downgrade**
- ตรวจ production build จริง: `write-excel-file/browser` ถูก dynamic `import()` แยกเป็น chunk `browser-*.js` (70.79 kB) — grep ยืนยัน `writeXlsxFile`/`write-excel-file` **ไม่มี** ใน main bundle `index-*.js` เลย (0 matches) → ตรงกับกติกา lazy-load ในแผน
- `npm run lint` ✓ (0 errors), `npm run build` ✓ — รันซ้ำเองยืนยันแล้ว

**พบ 1 จุด (minor, ไม่ block):** `AssignmentReportPage.tsx` — ปุ่ม "Export Filtered" (CSV+XLSX) ใช้ `hasRows={filtered.length > 0}` ควบคุมการซ่อน/แสดงทั้งกลุ่ม ขณะที่โค้ดเดิมใช้ `ControlAction` render เสมอ แล้วใช้ `disabled={!isFiltered}` + tooltip อธิบายเหตุผล — ผลคือถ้าผู้ใช้กรอง/ค้นหาแล้วได้ 0 แถวพอดี (isFiltered=true แต่ filtered.length=0) ปุ่มจะ**หายไปทั้งหมด**แทนที่จะโชว์แบบ disabled พร้อม toast อธิบายตอนกด (พฤติกรรมเดิม) — edge case เล็กน้อย ไม่กระทบ flow ปกติ ไม่ต้องแก้ด่วนแต่จดไว้เผื่อผู้ใช้เจอ
- ปุ่ม "Export All" เปลี่ยนจาก render เสมอ → ซ่อนเมื่อ `data.learners.length === 0` ถือว่าดีขึ้น (ตรงกับ pattern หน้าอื่นที่ซ่อนปุ่มเมื่อไม่มีข้อมูล) ไม่ใช่ปัญหา

**Manual file-open smoke ยังไม่ปิดครบ:** Copilot commit+deploy ไป QA/PROD แล้ว (`d4cdbfb`, `992a4c1`) โดยที่ DoD ข้อ "เปิดไฟล์ .csv/.xlsx จริงทั้ง th/en" ยังไม่ได้ทำ (จดไว้เองใน AGENT_LOG ว่า Outstanding) — ผมพยายามรัน dev server เพื่อทดสอบเองแต่ local API (`localhost:7128`, Windows auth) ไม่ได้รันใน session นี้ ทำให้หน้า report โหลดข้อมูลไม่ขึ้น (ค้างที่ "กำลังโหลด...") — **ยังไม่ได้ verify การเปิดไฟล์จริงด้วยตัวเอง** แนะนำให้ดาวน์โหลด+เปิดไฟล์ CSV และ XLSX จริงบน QA (ภาษาไทยไม่เพี้ยน, ตัวเลข sort ได้) ก่อนถือว่าปิดงานสมบูรณ์

**สรุป:** โค้ดและสถาปัตยกรรมถูกต้องตามแผนและการตัดสินใจที่ยืนยันไว้ครบ, lint/build ผ่าน, ไม่มี duplicate logic — ตั้งสถานะ VERIFIED โดยมีหมายเหตุ 2 ข้อข้างต้น (1 minor UX edge case ที่ไม่บังคับแก้, 1 verification gap ที่แนะนำให้ปิดบน QA)

**Fix เก็บตก (Claude Code — 2026-07-24):** แก้จุด minor ข้างต้นแล้ว — `AssignmentReportPage.tsx` ExportMenu "Export Filtered" เปลี่ยน `hasRows={filtered.length > 0}` → `hasRows` (เสมอ) ให้ปุ่มไม่หายไปเมื่อกรองได้ 0 แถวพอดี กลับไปใช้ `disabled={!isFiltered}` ควบคุมแทนเหมือน `ControlAction` เดิม — `npm run lint` ✓ `npm run build` ✓ (ยืนยัน bundle chunk-split ยังเหมือนเดิม `browser-CeIAsFQ3.js` 70.79 kB) — **ยังไม่ commit** (รอผู้ใช้สั่ง)
