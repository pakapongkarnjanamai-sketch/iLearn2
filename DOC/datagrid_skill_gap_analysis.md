# DataGrid Skill — Gap Analysis (GPCS.Workspace ↔ iLearn2)

วิเคราะห์เทียบ skill `datagrid-design` ของโปรเจกต์ **GPCS.Workspace** (`.github/skills/datagrid-design/SKILL.md`) กับมาตรฐานตารางของ **iLearn2.Admin.React** — เพื่อดูว่าหลักการไหนนำมาปรับใช้ได้ ไหนขัด convention

> เขียนโดย Claude Code 2026-06-16 — **เอกสารวิเคราะห์เท่านั้น ยังไม่แก้โค้ด**

---

## 1. ทำไมใช้ skill นี้ตรง ๆ ไม่ได้

skill เขียนผูกกับ **component และ stack เฉพาะของ GPCS.Workspace** ซึ่งต่างจาก iLearn2 ในระดับโครงสร้าง ไม่ใช่แค่สไตล์:

| ประเด็น | GPCS (skill) | iLearn2 |
|---|---|---|
| **Grid component** | `NativeDataGrid` — declarative config (`<DataGrid><Column/><FilterRow/><Scrolling/>...`) แบบ DevExtreme | `AppTable` (list) + `ExplorerTable`/`useExplorer` (explorer) — props/columns เป็น array config คนละ API |
| **Data loading** | `dataSource="/api/X"` string → grid ยิง skip/take/filter/sort เอง คาด `{data,totalCount}` | `createAdminDataSource`/`createRestDataSource` (store object) + page-specific loaders; explorer โหลด client-side ทั้งก้อน |
| **Paging paradigm** | **Pagination footer** ("Showing X–Y of N" + per-page selector 10/25/50/100) | **Infinite scroll** (เลื่อนโหลดหน้าถัดไป + footer "Loading more") — เลือกโดยตั้งใจ (ux_ui_analysis §2.2) |
| **Filter** | `FilterRow` (ราย column) + `SearchPanel` (global) ราย column ฝั่ง server | global search ผ่าน `ListToolbar` + chips; ไม่มี per-column filter |
| **Date** | en-GB Gregorian ใน grid | `formatDate`/`formatDateTime` (`src/lib/format.ts`) |

**สรุป:** skill = "วิธีใช้ component `NativeDataGrid`" — iLearn2 ไม่มี component นั้นและใช้ data-loading/paging คนละ paradigm → **ยกโค้ด/config มาทั้งดุ้นไม่ได้** ต้องดึงเฉพาะ **หลักการ (design principle)** ที่ data-layer-agnostic มาเทียบ

---

## 2. Gap analysis (หลักการ skill ↔ iLearn2)

| หลักการใน skill | iLearn2 ปัจจุบัน | verdict |
|---|---|---|
| Viewport-fill (`flex h-full flex-col` + `min-h-0 flex-1`, ไม่มี page scroll) | ✅ AppLayout (`h-[calc(100vh-56px)] min-h-0 overflow-auto`) + DataGridSurface (`flex min-h-0 flex-1`) | **ตรงแล้ว** |
| `dataType` built-in rendering (date/datetime/boolean) | ✅ AppTable: boolean→Yes/No pill, date/datetime→`formatDate` | **ตรงแล้ว** (สไตล์ต่าง: Yes/No vs ✅/❌) |
| null → em-dash `—` | ✅ มี (`String(val) ?? '—'`) | **ตรงแล้ว** |
| Width guidance (fixed คอลัมน์สั้น, flex คอลัมน์ชื่อ) | ✅ `width`/`minWidth` ต่อ column | **ตรงแล้ว** |
| `cellRender` memoized (useCallback) | ✅ columns ใน `useMemo` | **ตรงแล้ว** |
| onRowClick navigate | ใช้ `onRowDblClick` (double-click) ทั้งระบบ | **ต่าง (convention เรา)** — single vs double click |
| **Number formatting** (thousands `#,##0.00`, null→—) | ❌ **ไม่มี** — `dataType:'number'` แสดง `String(value)` ดิบ (เช่น score/progress) | **🟢 ช่องว่างจริง — ปรับได้ ฟิต** |
| Per-column `FilterRow` (server filter ราย column) | ❌ ไม่มี (global search อย่างเดียว) | **ต่าง (decision)** — เพิ่ม UX/งานใหญ่ |
| Pagination footer + per-page selector | ❌ ใช้ infinite scroll (ตั้งใจ) | **ขัด convention** — ไม่ควรเปลี่ยน |
| en-GB date inline | ใช้ `formatDate` (มาตรฐานเรา) | **คงของเรา** |
| Checklist: PageHeader first, keyExpr, noDataText | ✅ มี title/note (DataGridSurface), key, noDataText | **ตรงแล้ว** (PageHeader.tsx เราลบไป — ใช้ DataGridSurface header แทน) |

---

## 3. ข้อสรุป

iLearn2 ตารางมาตรฐาน **สอดคล้องกับหลักการ skill เกือบครบ** (viewport-fill, dataType, null em-dash, width, memoized cellRender) — เพราะทั้งสองโปรเจกต์ได้แรงบันดาลใจจาก DevExtreme grid เหมือนกัน

**ช่องว่างที่ปรับได้จริง + ฟิต (ไม่ขัด convention):**
- 🟢 **Number formatting** — `dataType:'number'` ควร format thousands separator + null→`—` (ตอนนี้แสดงดิบ) เป็น improvement เล็ก low-risk

**สิ่งที่ skill แนะนำแต่ iLearn2 เลือกต่างโดยตั้งใจ (ไม่ควรเปลี่ยนตาม):**
- **Pagination footer** ↔ iLearn2 ใช้ **infinite scroll** (ux_ui_analysis §2.2 ระบุชัด)
- **Per-column FilterRow** ↔ iLearn2 ใช้ **global search + chips** (ux_ui_analysis §2.2: "ยกเลิกการแก้ไข/ฟิลเตอร์ inline")
- **Single-click** ↔ iLearn2 ใช้ **double-click → detail** ทั้งระบบ

→ 2 อย่างหลังเป็น **product/UX decision** ถ้าจะเปลี่ยนต้องตัดสินใจระดับ design ไม่ใช่ "ทำตาม skill"

---

## 4. ข้อเสนอ (ยังไม่ดำเนินการ)

ถ้าต้องการ implement ภายหลัง:
- **Quick win:** เพิ่ม number formatting ใน `AppTable`/`ExplorerTable` (`dataType:'number'` → `Intl.NumberFormat` + null em-dash) — แผนเล็ก
- เรื่อง per-column filter / pagination เป็น decision แยก (ไม่อยู่ในข้อเสนอนี้)

> เอกสารนี้เป็น analysis ตามที่ผู้ใช้ขอ — **ยังไม่แก้โค้ด** จนกว่าจะสั่ง
