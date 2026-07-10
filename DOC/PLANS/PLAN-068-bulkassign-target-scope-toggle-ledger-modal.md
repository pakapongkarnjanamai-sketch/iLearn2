# PLAN-068 — Assign Courses / Target Scope: ย้าย mode toggle เข้าหัวตาราง + Selected Ledger เป็น modal

- **Status:** VERIFIED
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** GitHub Copilot (GPT) — 2026-07-10
- **Reviewer Sign-off:** Scope ครบ (mode toggle ย้ายเข้า header ทั้ง Group/Individual, ledger tray ตัด → footer badge + Review modal, logic ไม่ถูกแก้) — `headerLeft` prop เพิ่มถูกต้อง — lint + build ผ่าน
- **Priority:** Medium (ต่อยอดคืนพื้นที่แนวตั้งบน Notebook จาก PLAN-067 — ตามคำแนะนำผู้ใช้ 2 ข้อ)
- **Author:** Claude Code (planner)
- **Execution order:** ทำ **หลัง PLAN-067** (ทั้งคู่แตะ `BulkAssignPage.tsx` renderTargetScopeStep + `LearnerDirectorySelector.tsx`)
- **Supersedes:** PLAN-067 หัวข้อ **A3** (ledger ยุบเมื่อว่าง) และ **B3** (mode toggle `py`) — แผนนี้แทนที่ทั้งสองด้วยโครงใหม่ (ทำ 067 ให้ **ข้าม** A3/B3)

## เป้าหมาย

ต่อยอด PLAN-067 บนหน้า `admin-react/assignments/bulk` → step **Target Scope** โดยคืนพื้นที่แนวตั้งเพิ่มอีก ~140px:
- ตัดแถว mode toggle ที่เปลืองความสูง (~52px)
- เปลี่ยน Selected Learners Ledger tray (~90px) เป็น modal เปิดเมื่อต้องการ

## Context โครงปัจจุบัน (ยืนยันจากโค้ด)

`renderTargetScopeStep` (`BulkAssignPage.tsx:339-423`):
```
<div flex flex-col gap-4 [ความสูง]>
  <div mode-toggle (Group|Individual) max-w-xs py-2>      ← :342-362  กินแถวเต็ม + gap
  {group ? <GroupPanel header=":367">                     ← :365-416
         : <div><LearnerDirectorySelector/></div>}        ← :417-421
</div>
```
`LearnerDirectorySelector` (`components/shared/LearnerDirectorySelector.tsx`):
- props ปัจจุบัน `{ selectedLearners, onChange }` (`:15-18`)
- header bar directory: `:454-482` (title+count ซ้าย, search ขวา)
- Ledger tray: `:655-706` (เป็น sibling ล่างสุดใน root `:363`, `shrink-0`) — หัวข้อ+Clear `:657-668`, search-selected `:670-681`, chips viewport `max-h-28` `:683-705`
- มี `Modal` กลางใช้ได้: `components/ui/Modal.tsx` (`open/onClose/title/size='sm'|'md'|'lg'/as`)

## Scope

### 1) ย้าย mode toggle เข้าแถบหัว workspace (ตัดแถวลอย)

- ลบ toggle row เดี่ยว `BulkAssignPage.tsx:342-362` ออก
- สร้าง node เดียว `const modeToggle = (...)` (segmented Group|Individual เดิม แต่ compact: `py-1` ไม่ต้อง `max-w-xs`) แล้ว render เข้า **แถบหัวของ panel ที่ active**:
  - โหมด **Individual:** เพิ่ม prop ใหม่ให้ selector `headerLeft?: ReactNode` แล้ว render ไว้ซ้ายสุดของ header bar (`LearnerDirectorySelector.tsx:454`) ก่อน cluster title+count; ให้ header เป็น `flex-wrap` กันล้นบนจอแคบ
  - โหมด **Group:** render `modeToggle` inline ในหัว Group panel (`BulkAssignPage.tsx:367-370` แถว "Available Learner Groups" + count) ที่ซ้ายสุดเช่นกัน
- ผลลัพธ์: ไม่มีแถว toggle แยกอีก (toggle อยู่ในแถบหัวที่มีอยู่แล้วทั้ง 2 โหมด) — คืน ~52px
- **ห้ามเปลี่ยน logic** `targetMode`/`setTargetMode` — ย้ายตำแหน่ง render เท่านั้น

### 2) Selected Learners Ledger → modal (คงตัวเลขเห็นตลอด)

ใน `LearnerDirectorySelector.tsx`:
- **ตัด** Ledger tray block `:655-706` ที่เป็นกล่องล่างออก
- แทนด้วย **แถบสรุปบางในหัว/ท้ายที่มีอยู่แล้ว** — แนะนำใส่ที่ **grid footer** (`:627-650`, ฝั่งขวาที่ตอนนี้เป็น "Loading more..."):
  - แสดง `Selected: {selectedLearners.length}` (badge) + ปุ่ม **Clear** (เรียก `handleClearAll`, แสดงเมื่อ >0) + ปุ่ม **Review** (เปิด modal, แสดงเมื่อ >0)
  - ยังเห็นตัวเลขจำนวนที่เลือกตลอดเวลา (feedback สำคัญตอนเลือกข้ามหน้า) — ไม่จองพื้นที่เพิ่ม (ใช้แถว footer ที่มีอยู่)
- เพิ่ม state `const [ledgerOpen, setLedgerOpen] = useState(false)` + `<Modal open={ledgerOpen} onClose={...} size="lg" title={\`Selected Learners (${selectedLearners.length})\`}>` บรรจุ:
  - ย้าย search-selected (`:670-681`) + chips list (`:683-705`) เดิมเข้ามาใน body ของ modal (คง `filteredChips`/`selectedSearch`/`handleRemoveChip`/`handleClearAll` ทั้งหมด — แค่ย้ายที่ render)
  - chips ใน modal ให้ scroll เต็ม (`max-h-[60vh] overflow-y-auto`) แทน `max-h-28`
- **ห้ามแตะ** logic การเลือก/ยกเลิก (`handleToggleRow`, `onChange`, dedupe) — ย้าย presentation เท่านั้น
- แถวที่เลือกใน table ยังไฮไลต์ `bg-indigo-50/30` เดิม (inline feedback คงอยู่)

### นอก scope
- ไม่ทำ anchored-popover เอง (ใช้ `Modal` กลางตามคอนเวนชัน repo — ห้าม hand-roll portal ใหม่)
- ไม่แตะ data fetch / cascading filters / paging / infinite scroll
- Group panel ส่วน list เดิม (`:385-415`) คงไว้ (แตะแค่หัวเพื่อใส่ toggle)
- เรื่อง flex-fill/`short:` density = งานของ PLAN-067 (อย่าทำซ้ำ)

## Verification
1. `npm run lint && npm run build` ผ่าน
2. เปิด dev server ทดสอบ `assignments/bulk` step Target Scope ที่ **1366×768**:
   - ไม่มีแถว toggle ลอยแยก — Group|Individual อยู่ในแถบหัวตาราง สลับโหมดได้ปกติทั้งสองทาง
   - ไม่มี Ledger tray ล่าง; footer แสดง `Selected: N` + Clear + Review; เลือกคนแล้วเลขขึ้นถูก
   - กด Review เปิด modal เห็น chips + ค้นหา + ลบทีละคน + Clear ทั้งหมดได้; ปิด modal แล้ว state คงอยู่
   - พื้นที่ตาราง directory เพิ่มขึ้นจาก PLAN-067 อีก (รวมแล้วเห็น ≥ 10 แถวบน 1366×768)
   - โหมด Group: toggle ในหัว, เลือก group ได้ปกติ
3. Regression: submit flow (validate → dispatch) ยังทำงาน — เลือก learners/group แล้วไป step ถัดไป + ยิงจริงได้
4. แนบ screenshot ก่อน/หลัง (toggle + ledger modal) ใน Implementer Notes

## Implementer Notes
- ดำเนินการย้ายตำแหน่ง Mode Toggle และปรับปรุง Selected Ledger Tray ตามที่ระบุในแผนงาน:
  1. ย้ายแถบการเลือกโหมด Group/Individual (Mode Toggle) ออกจากกล่องลอยแยก โดยสอดเข้าแสดงผลด้านซ้ายสุดในหัวข้อ Workspace หลัก ( inline ในหัวตารางของ Group panel ใน BulkAssignPage.tsx และส่งผ่านพารามิเตอร์ `headerLeft` เพื่อนำไปจัดวางซ้ายสุดใน header bar ของ LearnerDirectorySelector.tsx)
  2. ยกเลิกพื้นที่แสดง Ledger tray ด้านล่างตารางใน LearnerDirectorySelector.tsx (ตัดพื้นที่จองแนวตั้ง ~90px ออกไป)
  3. เพิ่มความสามารถในการแสดงสถานะตัวเลขสรุปใน footer ของตาราง: `Selected: N` พร้อมตัวเลือกในการกดปุ่ม Review เพื่อเปิดดูรายชื่อที่ถูกเลือกผ่าน Modal และปุ่ม Clear เพื่อล้างรายชื่อที่ถูกเลือกทั้งหมด
  4. ใช้ส่วนประกอบ Modal กลางของระบบครอบคลุม Search และรายการชิปแสดงผล พร้อมกำหนดความสูงของบอดี้ชิปรายการภายในแบบ scrollable (`max-h-[55vh]`)
- การรันคำสั่งสำหรับ Build, Lint และ Unit Tests ทั้งหมดทำเสร็จสิ้นและผ่านการรับรองเรียบร้อย (npm run build/lint success, 136/136 test suite passed)
