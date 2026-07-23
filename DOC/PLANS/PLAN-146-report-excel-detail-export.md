# PLAN-146 — Excel Export รายคน + Date Filter สำหรับ Assignment / Learner Group Reports

- **สถานะ:** DONE
- **Assigned:** GitHub Copilot (GPT)
- **วันที่:** 2026-07-23
- **ต่อยอดจาก:** PLAN-143 (Assignment Summary Report), PLAN-145 (Learner Group Summary Report)
- **หน้าที่กระทบ:** `/admin-react/reports/assignments`, `/admin-react/reports/learner-groups`

## เป้าหมาย

Admin ต้องโหลดข้อมูลจาก report ทั้งสองหน้าไป "รายงานต่อ" (ผู้บริหาร/HR) ได้ทันที — ความต้องการที่ยืนยันกับผู้ใช้แล้ว:

1. **ละเอียดระดับรายคน** — 1 แถว = ผู้เรียน × คอร์ส พร้อมสถานะ/progress/วันครบกำหนด/วันที่จบ (summary อย่างเดียวไม่พอ)
2. **ไฟล์ Excel (.xlsx)** generate จาก backend — หัวตาราง format จริง, % และวันที่เป็น native type, หลาย sheet ในไฟล์เดียว (Summary + Detail)
3. **Filter ช่วงวันที่** ก่อน export

CSV export ฝั่ง frontend ที่มีอยู่ **คงไว้ตามเดิม** (อย่าลบ) — Excel เป็นปุ่มเพิ่ม

**ข้อกำหนดโครงสร้าง (ยืนยันจากผู้ใช้):** Learner Group report และ Assignment report **แยกหน้ากันเสมอ** — คนละ route, คนละหน้า, คนละ export endpoint, คนละไฟล์ Excel — ห้ามรวมเป็นหน้าเดียวหรือไฟล์เดียว

## Scope

### 1. Backend — NuGet + Excel builder

- เพิ่ม package **ClosedXML** (ล่าสุด stable) ใน `iLearn.Application` — **ห้ามใช้ EPPlus** (v5+ ต้องซื้อ commercial license)
- สร้าง service สร้างไฟล์ Excel (แนะนำ `ReportExcelBuilder` หรือ method ใน `ReportService`) คืน `byte[]` + expose ผ่าน `IReportService`

### 2. Backend — endpoints ใหม่ (ReportsController)

```
GET /api/Reports/assignments/export?from=YYYY-MM-DD&to=YYYY-MM-DD&lang=th|en
GET /api/Reports/learner-groups/export?from=YYYY-MM-DD&to=YYYY-MM-DD&lang=th|en
```

- คืน `File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName)` — **ไม่ใช่** envelope `{ success, data }` แบบ endpoint อื่น
- ชื่อไฟล์: `assignment-report-YYYYMMDD-HHmm.xlsx` / `learner-group-report-YYYYMMDD-HHmm.xlsx` (เวลาไทยจาก `IDateTime.Now`)
- `from`/`to` optional — ไม่ส่ง = ทั้งหมด; `lang` default `th` (เลือกชุดหัวคอลัมน์)
- Division scope ผ่าน `_currentUser.DivisionId` เหมือน summary endpoints เดิม

### 3. เนื้อหาไฟล์ Excel

**Assignment report (2 sheets):**
- Sheet "Summary" — คอลัมน์เดียวกับ `AssignmentSummaryRow` ปัจจุบัน
- Sheet "Detail (รายคน)" — 1 แถว = enrollment (ผู้เรียน × คอร์ส ใน batch): เลขที่งานมอบหมาย, NID, ชื่อ-สกุลผู้เรียน, division ผู้เรียน, ชื่อคอร์ส, วันเริ่ม (effective), วันครบกำหนด (effective), สถานะ, progress %, วันที่เรียนจบ, เกินกำหนด (วัน)

**Learner Group report (3 sheets):**
- Sheet "Summary" — คอลัมน์เดียวกับ `LearnerGroupSummaryRow` ปัจจุบัน
- Sheet "Members" — 1 แถว = สมาชิก: ชื่อกลุ่ม, NID, ชื่อ-สกุล, division, วันที่เข้ากลุ่ม
- Sheet "Detail (รายคน)" — 1 แถว = enrollment ของสมาชิกในกลุ่ม: ชื่อกลุ่ม, NID, ชื่อ-สกุล, ชื่อคอร์ส, เลขที่งานมอบหมาย, วันเริ่ม/ครบกำหนด (effective), สถานะ, progress %, วันที่เรียนจบ, เกินกำหนด (วัน)

**Format ขั้นต่ำ:** แถวหัวเรื่อง (ชื่อ report + ช่วงวันที่ + generated at), header row ตัวหนา + พื้นสีอ่อน + freeze pane, วันที่เป็น date format จริง (`dd/mm/yyyy`), progress/completion เป็น number format `0.0%` (เก็บค่า 0–1), AutoFilter บน header, ปรับความกว้างคอลัมน์พอประมาณ (อย่าใช้ `AdjustToContents()` กับ dataset ใหญ่ทั้ง sheet — ช้า; fix width พอ)

### 4. กติกาข้อมูล (สำคัญ — bug จริงที่เคยหลุด)

- **วันเริ่ม/ครบกำหนดรายคนต้องเป็น effective dates** ผ่าน `BuildVisibleEnrollmentRowsQuery` / logic เดียวกับ `GetEffectiveSchedule` — ห้ามอ่าน `Enrollment.StartDate/DueDate` ดิบ (PLAN-086)
- **Date filter ตีความที่ effective DueDate**: เอาแถวที่ effective due date อยู่ใน `[from, to]` (batch ระดับ Summary ใช้ due date ของ batch; ถ้าแถวไม่มี due date ให้ตกเฉพาะตอนมี filter) — Summary sheet และ Detail sheet ต้อง filter ด้วยเกณฑ์เดียวกันให้ยอดสอดคล้องกัน
- เวลา generated ใช้ `IDateTime.Now` — ห้าม `DateTime.Now/UtcNow`
- ห้ามโหลด/Include `FileStorage.Data` ใน query ใด ๆ
- Overdue นับแบบเดียวกับ summary เดิม (`currentDate` จาก `IDateTime.Now`)

### 5. Frontend (ทั้งสองหน้า report)

- เพิ่ม date range filter (จาก/ถึง — `<input type="date">` สองช่องใน toolbar) — filter มีผลทั้ง**ตารางบนจอ** (filter ฝั่ง client ที่ effective due date ของแถว) และ**ส่งเป็น query ให้ export endpoint**
- เพิ่มปุ่ม `Export Excel` (`AppButton` variant secondary + `loading` ระหว่างโหลด, icon `FileSpreadsheet`) ข้างปุ่ม CSV เดิม — fetch เป็น blob แล้ว trigger download (ผ่าน helper ใหม่ใน `src/lib/` เช่น `downloadBlob.ts`)
- ส่ง `lang` ตามภาษา UI ปัจจุบัน
- Labels ใหม่ทั้งหมดเข้า dictionary สองภาษาใน `labels.ts` (ห้าม hardcode)
- Type/contract comment: endpoint ใหม่คืนไฟล์ binary — จดใน `reportTypes.ts` เป็นคอมเมนต์กำกับว่า export endpoints ไม่มี JSON shape

### 6. Tests

- `ReportServiceTests`: อย่างน้อย 2 tests —
  1. date filter ตัดแถวนอกช่วงออกทั้ง summary และ detail สอดคล้องกัน
  2. detail rows ใช้ effective dates (มี link → ค่า link, ไม่มี → fallback enrollment)
- ตรวจว่า workbook ที่ generate เปิดได้ (โหลดกลับด้วย ClosedXML ใน test แล้ว assert จำนวน sheet/แถว header)

## นอก Scope

- Scheduled/email report, PDF, chart ใน Excel
- แก้ CSV export เดิม
- Filter สถานะ/เลือกกลุ่มเจาะจง (ผู้ใช้ยังไม่ขอ — ถ้าจะทำให้เปิดแผนใหม่)

## Verification (รันก่อนปิดงาน)

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
# backend
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

- Manual: เปิดทั้งสองหน้า report → ตั้งช่วงวันที่ → Export Excel → เปิดไฟล์ตรวจ: จำนวน sheet ถูก, วันที่/% เป็น native format, แถว Detail ตรงกับที่เห็นบนจอ, ภาษาไทยไม่เพี้ยน

## Implementer Notes

- เพิ่ม ClosedXML `0.105.0` ใน `iLearn.Application` และ `iLearn.Tests`
- Backend เพิ่ม binary endpoints:
  - `GET /api/Reports/assignments/export?from=YYYY-MM-DD&to=YYYY-MM-DD&lang=th|en`
  - `GET /api/Reports/learner-groups/export?from=YYYY-MM-DD&to=YYYY-MM-DD&lang=th|en`
- Excel สร้างใน `ReportExcelBuilder` ด้วย fixed column widths, title/date/generated rows, bold shaded headers, freeze rows, AutoFilter, native date cells, and `0.0%` numeric percentage cells
- Assignment workbook มี `Summary` + `Detail`; Learner Group workbook มี `Summary` + `Members` + `Detail`
- Export detail rows use `BuildVisibleEnrollmentRowsQuery` effective start/due dates; date filters apply to effective due dates
- Frontend เพิ่ม date range filters + `Export Excel` button on both report pages; CSV export kept unchanged
- Added `downloadBlob.ts` helper and binary endpoint contract comment in `reportTypes.ts`
- Verification run:
  - `npm run lint` ✓
  - `npm run build` ✓ (Vite chunk-size warning เดิม)
  - focused export tests `ReportServiceTests.AssignmentExport*` + `LearnerGroupExport*` ✓
  - `dotnet build .\iLearn.Tests\iLearn.Tests.csproj -o .\artifacts\verify-test` ✓ (warnings เดิม)
  - `dotnet test .\artifacts\verify-test\iLearn.Tests.dll` → **279/279 passed** ✓
  - Cleaned `artifacts\verify-test` and `artifacts\verify-plan146-focused`
