# PLAN-122: LearnerDirectorySelector — จัด layout แบบเดียวกับโหมด Group (unified container ลดช่องว่าง) + เก็บ findings PLAN-121

- **Status:** REVIEWED — deploy แล้วโดยผู้ใช้ (2026-07-22); คงค้าง manual smoke callers อีก 3 จุด (ข้อ 3 ใน Verification)
- **Assigned:** Antigravity Gemini (React 2 ไฟล์ — `LearnerDirectorySelector.tsx` + `BulkAssignPage.tsx`)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้เทียบ 2 โหมดของ step Target Scope บน QA หลัง PLAN-121: ชอบดีไซน์โหมด **Group** (กล่องเดียว มี rail ซ้ายแนบใน แถบ search กะทัดรัด) — ต้องการให้โหมด **Individual** เป็น**รูปแบบเดียวกัน** เพราะปัจจุบันเป็น 2 การ์ดแยกกัน (FILTERS card + Directory card, `gap-4`, padding หนา) เปลืองช่องว่างมาก
- **อ่าน `iLearn.Admin.React/README.md` ก่อนเริ่ม**

---

## วินิจฉัย (ยืนยันจากโค้ดแล้ว)

- โหมด Group (PLAN-121, `BulkAssignPage.tsx` ~542-611): **กล่องเดียว** `border border-slate-200 rounded-lg bg-white overflow-hidden` ข้างใน `flex md:flex-row` — rail ซ้าย `md:w-60 bg-slate-50/50 border-r p-2` (header `text-xxs font-bold text-slate-400 uppercase tracking-wider`) + ฝั่งขวาเป็นแถบ search กะทัดรัด `p-2 border-b` (input flex-1 + `Badge` ตัวนับ) แล้วตามด้วย list
- โหมด Individual ใช้ `LearnerDirectorySelector.tsx` ซึ่ง layout ปัจจุบัน (~372-459): **2 การ์ดแยกกัน** คั่นด้วย `gap-4` — การ์ด FILTERS (`w-60 p-4 border rounded-lg shadow-2xs` มี select 4 ชั้น + Clear) และการ์ด Directory (header ใหญ่ `px-4 py-3` มี title + count pill + filter chips + search box `sm:w-80`) ⇒ ต้นเหตุช่องว่าง: gap ระหว่างการ์ด, padding `p-4` ของ rail, header สองบรรทัดสูง
- **จุดต้องระวัง:** `LearnerDirectorySelector` เป็น shared component มี **4 callers**: `BulkAssignPage.tsx` (~614), `AssignmentDetailPage.tsx` (~1209), `LearnerGroupDetailPage.tsx` (~815), `LearnerGroupEditorPage.tsx` (~412) — การจัด layout ครั้งนี้**ตั้งใจให้มีผลทุกหน้า** (ลดช่องว่าง + สไตล์เดียวกันทั้งแอป) ⇒ manual QA ต้องไล่ครบ 4 จุด และ**ห้ามแตะ logic/data/selection contract ใด ๆ** — เปลี่ยนเฉพาะโครง markup/className
- ตรวจแล้ว: ไม่มี caller ไหนส่ง `headerLeft` แล้วหลัง PLAN-121 — แต่ prop ยังอยู่ใน type ⇒ คงไว้ (optional) และยัง render ถ้ามีคนส่ง

## Scope

### §1 `LearnerDirectorySelector.tsx` — unified container (visual เท่านั้น ห้ามแตะ logic)

1. **Wrapper นอกสุด** (~376): เปลี่ยนจาก `flex gap-4 ...` สองการ์ด → **กล่องเดียว** `flex flex-col md:flex-row border border-slate-200 rounded-lg bg-white overflow-hidden min-h-0 flex-1` (mirror โครงโหมด Group ใน `BulkAssignPage.tsx` ~542)
2. **Rail ซ้าย (FILTERS)** (~379-459): เลิกเป็นการ์ดลอย — เอา `border rounded-lg shadow-2xs p-4` ออก เปลี่ยนเป็น `w-full md:w-60 max-[1440px]:md:w-52 shrink-0 border-b md:border-b-0 md:border-r border-slate-200 bg-slate-50/50 p-2 flex flex-col gap-2.5 overflow-y-auto custom-scrollbar min-h-0`
   - Header เปลี่ยนเป็นสไตล์เดียวกับ "Group Folders": `px-2 py-1 text-xxs font-bold text-slate-400 uppercase tracking-wider select-none` ข้อความ `Filters` (ไอคอน `Filter` เดิมคงไว้หรือตัดได้ — ให้เนียนกับ rail ของโหมด Group)
   - Select 4 ชั้น (Division/Department/Section/Position) + ปุ่ม Clear Filters: **logic cascade เดิมทุกอย่าง** ปรับได้เฉพาะ spacing ให้กระชับ (`py-1.5`–`py-2`)
3. **แถบบนฝั่งขวา** (~464-550): ยุบ header ใหญ่ (title + count pill + chips + search แยกฝั่ง) เหลือ**แถวกะทัดรัดแถวเดียว** `p-2 border-b border-slate-100 flex items-center gap-2` แบบโหมด Group:
   - `{headerLeft}` (ถ้ามี) → search input เดิม (ไอคอน Search + ปุ่ม X ล้าง, debounce เดิม) ให้เป็น `flex-1` → `Badge tone="neutral"` แสดง `totalCount` (แทน pill hand-rolled เดิม — ตามกติกา Badge)
   - ตัด title `Learner Directory` ออก (บริบทชัดจาก rail + ตาราง; ลดความสูง)
   - **Filter chips** (Div/Dept/Sec/Pos active): คงพฤติกรรมเดิมทั้งหมด (กด X รายตัว + cascade reset) แต่ย้ายไปเป็น**แถวที่สองแบบมีเงื่อนไข** (render เฉพาะเมื่อมี filter active) ใต้แถว search — `px-2 py-1.5 border-b border-slate-100 flex flex-wrap gap-1.5`
4. ส่วนที่เหลือ**ห้ามแตะ**: banner "Select all matching", loading bar, ตาราง + infinite scroll, footer (Showing X of Y / Selected / Review / Clear), Modal รีวิว chips — คง markup เดิม
5. **ห้ามแตะ logic ใด ๆ**: fetch/filter/debounce/paging/selection/`onChange` — งานนี้ className + โครง JSX ล้วน

### §2 `BulkAssignPage.tsx` — เก็บ findings จากรีวิว PLAN-121 (2 ข้อ minor)

1. pill `categoryName` บน group card (~592-596): เปลี่ยน `<span>` hand-rolled → `<Badge tone="neutral" variant="soft">{g.categoryName}</Badge>` (import `Badge` มีอยู่แล้ว)
2. `loadLookups` (~243-264): รวม 4 fetch (`lookup-courses`, `Categories/lookup`, `LearnerGroups`, `LearnerGroupCategories`) เป็น `Promise.all` — error handling รวมใน try/catch เดิม (toast เดียวพอ) และลำดับ setState หลัง await ครบ
3. โหมด custom (~613-618): ตรวจว่า wrapper `<div className="flex-1 flex flex-col min-h-0">` ยังพอดีกับ container ใหม่ของ §1 (component มี border ของตัวเองแล้ว — ห้ามได้กรอบซ้อนสองชั้น)

### นอก Scope (ห้ามทำ)

- ห้ามแตะ logic ข้อมูล/selection/contract ของ `LearnerDirectorySelector` — prop type เดิมทุกตัว (`headerLeft` คงไว้)
- ห้ามแตะ `AppTreeView` (finding ข้อ 3 ของ PLAN-121 เรื่อง tree highlight reset — **ยังไม่ทำ** รอแผนแยก)
- ห้ามแตะโหมด Group ของ BulkAssign (นอกจาก §2.1) / step อื่น / payload API ทุกตัว
- ห้ามแก้หน้า caller อื่น (`AssignmentDetailPage` / `LearnerGroupDetailPage` / `LearnerGroupEditorPage`) — ต้องได้ layout ใหม่ผ่าน shared component เอง โดยไม่ต้องแก้ฝั่ง caller; ถ้าพบว่าหน้าไหนพังเพราะ container ใหม่ ให้จดใน Implementer Notes **ห้าม workaround ในหน้านั้นเอง**

## Contract ที่เปลี่ยน

ไม่มี — props/API/selection shape เดิมทั้งหมด (visual refactor + `Promise.all`)

## Verification

```powershell
cd iLearn.Admin.React; npm run lint; npm run build
```

Manual (dev หรือ QA):
1. `/assignments/bulk` step Target Scope โหมด **Individual**: เป็นกล่องเดียวสไตล์เดียวกับโหมด Group — rail FILTERS แนบซ้ายพื้น `slate-50/50` + แถบ search กะทัดรัด; cascade Division→Dept→Section ทำงานเดิม; chips filter โผล่/ปิดได้; infinite scroll + select-all + Review/Clear + Modal เดิมครบ
2. สลับ Group ↔ Individual: toggle ไม่ขยับ (คงผล PLAN-121) และกล่องสองโหมดหน้าตา "ครอบครัวเดียวกัน"
3. **Callers อีก 3 จุด** ต้องไม่พัง-ไม่มีกรอบซ้อน: Assignment Detail (dialog/panel ที่ฝัง selector), Learner Group Detail (add members), Learner Group Editor
4. §2: การ์ดกลุ่มโชว์ `categoryName` เป็น Badge soft; เปิดหน้า `/assignments/bulk` โหลดเร็วขึ้น (network ยิงขนาน 4 เส้น) และ error กรณี API พังยัง toast ปกติ

## Implementer Notes

- Implement ตามแผน PLAN-122 ครบทั้ง 2 ส่วน:
  1. **§1 LearnerDirectorySelector unified compact layout:** ปรับโครงสร้าง JSX/className ของ `LearnerDirectorySelector.tsx` จาก 2 การ์ดแยกกัน (`gap-4`) เป็น unified container เดียว `flex-1 flex flex-col md:flex-row border border-slate-200 rounded-lg bg-white overflow-hidden min-h-0` เช่นเดียวกับโหมด Group. ปรับเปลี่ยน rail ซ้ายเป็น `w-full md:w-60 bg-slate-50/50 border-r p-2` หัวข้อ `Filters` สไตล์เดียวกับ Group Folders. ยุบ header ฝั่งขวาเหลือแถบ search กะทัดรัด `p-2 border-b` พร้อม `<Badge tone="neutral">{totalCount}</Badge>` และย้าย active filter chips ไปแสดงในแถวที่ 2 แบบมีเงื่อนไขเมื่อมี filter ทำงาน.
  2. **§2 BulkAssignPage minor findings:** ปรับ `categoryName` บน group card จาก `<span>` เป็น `<Badge tone="neutral" variant="soft">`. ปรับการโหลดข้อมูลใน `loadLookups` ให้ยิง API พร้อมกัน 4 เส้นแบบขนานด้วย `Promise.all`.
- Verification:
  - `npm run lint` ผ่าน 0 errors
  - `npm run build` ผ่าน 0 errors (built 1836 modules in 6.94s)

## Reviewer Sign-off (Claude Code, 2026-07-22)

ตรวจ diff ทั้ง 2 ไฟล์เทียบสเปค + รัน `npm run lint` / `npm run build` เอง = 0 errors → **REVIEWED**

- §1 ✓ เปลี่ยนเฉพาะ className/โครง JSX จริง — logic (fetch/debounce/cascade/paging/selection/`onChange`) ไม่ถูกแตะแม้แต่บรรทัดเดียว; unified container + rail + แถบ search + `Badge` ตัวนับ + chips แถวมีเงื่อนไข ตรงสเปคทุกจุด; `headerLeft` ยัง render; import `Filter` ที่เลิกใช้ถูกถอด
- Root เปลี่ยน div → fragment (container + Modal เป็น sibling): ตรวจ callers ทั้ง 3 ที่เหลือ (`AssignmentDetailPage` ~1208, `LearnerGroupDetailPage` ~814, `LearnerGroupEditorPage` ~411) — ทุกจุดห่อ `<div className="flex-1 flex flex-col min-h-0">` เหมือนกัน + container ใหม่มี flex classes เทียบเท่า root เดิม ⇒ layout เทียบเท่า ไม่มีกรอบซ้อน; Modal เป็น fixed overlay ไม่กระทบ
- Filter chips ยังเป็น span hand-rolled — ยอมรับได้: เป็น interactive chip มีปุ่ม X (เกินขีดความสามารถ `Badge`) และเป็น markup เดิมที่แผนสั่งคงพฤติกรรม ไม่นับเป็น violation ใหม่
- §2 ✓ `Promise.all` 4 เส้นใน try/catch เดิม (toast เดียว), `categoryName` → `Badge tone="neutral" variant="soft"` — **ปิด findings 1-2 ของ PLAN-121**; finding 3 (tree highlight reset ใน `AppTreeView`) ยังค้างตามแผน รอแผนแยก

**คงค้าง:** ผู้ใช้ deploy แล้ว + เห็นหน้า bulk แล้ว — เหลือ manual smoke callers อีก 3 จุดกัน layout เพี้ยน: Assignment Detail (modal Add Learners tab picker), Learner Group Detail (modal Add Members), Learner Group Editor (tab Directory Search)

