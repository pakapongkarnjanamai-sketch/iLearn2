# PLAN-121: Bulk Assign wizard UX — Category filter ใน Choose Courses + Learner-group tree ใน Target Scope + แก้ toggle ขยับตำแหน่ง

- **Status:** REVIEWED — รอ deploy QA + manual smoke (ข้อ 1-4 ใน Verification)
- **Assigned:** Antigravity Gemini (React ไฟล์เดียวเป็นหลัก — `BulkAssignPage.tsx`)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้รีวิว `/admin-react/assignments/bulk` บน QA: (1) Syllabus Catalog มี 589 คอร์สแบน ๆ หายาก — อยากได้โครงสร้าง **Category** ช่วยฟิลเตอร์ (2) Target Scope โหมด Group เป็น list แบน — อยากใช้ **โครงสร้าง learner-group categories** (แบบหน้า Learner Groups) (3) ปุ่ม toggle `Group | Individual` **ขยับตำแหน่งหนี**เวลาสลับโหมด ทำให้สับสน
- **อ่าน `iLearn.Admin.React/README.md` (API Contract Sync) ก่อนเริ่ม**

---

## วินิจฉัย (ยืนยันจากโค้ดแล้ว — ไม่ต้องแตะ backend เลย)

- **§1:** `Assignments/lookup-courses` (`AssignmentsController.cs:189-206`) **ส่ง `categoryId` + `divisionId` มาอยู่แล้ว** ผ่าน `LookupCourseDto` — แต่ mirror type `LookupCourse` ใน `BulkAssignPage.tsx` (~20-25) มีแค่ `id/code/title/courseTypeName` ⇒ เติม field ใน type + โหลดชื่อ category จาก `Categories/lookup` (`CategoriesController.cs:29-52` — คืน `{ id, name, divisionId, sortOrder }`, division-scoped ตาม current user เหมือนกับ lookup-courses ⇒ ชุดข้อมูลสอดคล้องกันเอง)
- **§2:** learner groups มีโครงสร้าง category tree อยู่แล้ว: `GET LearnerGroupCategories` คืน `LearnerGroupCategoryDto` (`id, name, parentId, depth, childCount, learnerGroupCount, divisionId`) ลึกสุด 4 ชั้น และ `GET LearnerGroups` (ที่หน้านี้โหลดอยู่แล้ว) คืน `categoryId`/`categoryName` ต่อ group อยู่แล้ว (`LearnerGroupDto`) — mirror type `LearnerGroupLookup` (~27-31) ยังไม่มี 2 field นี้. มี shared component **`AppTreeView`** (`src/components/ui/AppTreeView.tsx` — `TreeViewNode { id, text, items, categoryId, divisionId, isRoot, isDivision }`) ที่หน้า `LearnerGroupListPage` ใช้เป็น tree เลือก category อยู่แล้ว (modal Move group ~813) — ใช้ซ้ำได้เลย
- **§3 root cause ปุ่มขยับ:** `renderModeToggle()` ถูก render **คนละ container ตามโหมด** — โหมด group อยู่ใน header ของ panel รายชื่อกลุ่ม (~361) แต่โหมด custom ถูกส่งเป็น `headerLeft` เข้าไปใน header ของ `LearnerDirectorySelector` (~417) ซึ่ง layout ภายในต่างกัน (มี sidebar ฟิลเตอร์ซ้าย + ข้อความ header คนละความยาว) ⇒ ตำแหน่งปุ่มกระโดดทุกครั้งที่กด ⇒ ต้อง**ยกปุ่มออกมาไว้แถวคงที่ของ step เอง** เหนือ panel ทั้งสองโหมด

## Scope (React ล้วน — ไฟล์หลัก `BulkAssignPage.tsx`; ห้ามแก้ `LearnerDirectorySelector`/`AppTreeView`/`SegmentedToggle` เอง)

### §1 Choose Courses — Category filter

1. Mirror types (พร้อมคอมเมนต์ `// Mirrors ...` ตามกติกา README):
   - `LookupCourse` เพิ่ม `categoryId?: number | null`, `divisionId?: number | null` (mirror `LookupCourseDto`)
   - เพิ่ม `type CategoryLookup = { id: number; name: string; divisionId?: number | null; sortOrder: number }` (mirror `CategoryDto` ผ่าน `GET Categories/lookup`)
2. ใน `loadLookups` โหลดเพิ่ม `Categories/lookup` (DataSourceLoader → unwrap `data` array เหมือน pattern ที่ `CourseEditorPage.tsx:196` ใช้)
3. UI panel **Syllabus Catalog** (คอลัมน์ซ้าย): เพิ่ม `<select>` **Category filter** ไว้แถวเดียวกับ (หรือเหนือ) ช่อง search:
   - ตัวเลือกแรก `All Categories` (ค่า default, value = `'all'`)
   - รายการ category เรียง `divisionId → sortOrder → id` (ตาม order ที่ endpoint ส่งมาแล้ว) — label แสดง `${sortOrder}. ${name}` เมื่อ `sortOrder > 0` (สอดคล้อง PLAN-117)
   - เพิ่มตัวเลือก `Uncategorized` **เฉพาะเมื่อ**มีคอร์สที่ `categoryId == null` ในรายการ
   - แต่ละ option ต่อท้ายจำนวนคอร์ส เช่น `Safety (24)` — นับจาก `availableCourses` (คอร์สที่ยังไม่ถูกเลือก)
4. Filter logic: `visibleAvailableCourses` = กรองด้วย **category ∩ text search** (ทั้งสองเงื่อนไขพร้อมกัน); เปลี่ยน category ไม่ล้าง search และกลับกัน
5. แถวการ์ดคอร์สในรายการ: ใต้ code เดิม (หรือบรรทัดเดียวกัน) แสดงชื่อ category แบบ text จาง ๆ เล็ก ๆ เมื่อดูโหมด `All Categories` — ช่วย scan; ใช้ text ธรรมดา ไม่ต้องเป็น `Badge` ก็ได้ (ถ้าใช้ Badge ให้ `tone="neutral" variant="soft"`)
6. Panel ขวา (Selected Courses) **ไม่แตะ logic** — คอร์สที่เลือกแล้วต้องแสดงครบเสมอไม่ว่า filter ซ้ายเป็นอะไร (พฤติกรรมเดิมถูกแล้วเพราะแยก list กัน)

### §2 Target Scope — โครงสร้าง learner-group categories ในโหมด Group

1. Mirror types:
   - `LearnerGroupLookup` เพิ่ม `categoryId?: number | null`, `categoryName?: string | null` (mirror `LearnerGroupDto`)
   - เพิ่ม `type LearnerGroupCategoryLookup = { id: number; name: string; parentId?: number | null; depth?: number; learnerGroupCount?: number }` (mirror `LearnerGroupCategoryDto` ผ่าน `GET LearnerGroupCategories`)
2. `loadLookups` โหลดเพิ่ม `LearnerGroupCategories` (envelope `{ success, data }`)
3. Layout โหมด group เปลี่ยนเป็น **สองคอลัมน์** (โครงเดียวกับโหมด Individual ที่มี sidebar ซ้าย — ช่วยให้สองโหมดหน้าตาสมมาตรกันด้วย):
   - **ซ้าย (rail แคบ ~ w-56/64, ซ่อนบน mobile หรือ stack บนได้):** `AppTreeView` ของ category tree — root node `All Groups` (`isRoot: true`, categoryId 0) + node ตาม parent/child จาก `parentId` (เรียงชื่อ A-Z ต่อชั้น เหมือน `categoriesByParent` ใน `LearnerGroupListPage.tsx:150-165`); text ของ node ต่อท้ายจำนวนกลุ่ม เช่น `Production (5)` โดยนับ**รวม subtree**
   - **ขวา:** search box + รายการ group cards เดิม (radio-select behavior เดิมทุกอย่าง) แต่กรองตาม category ที่เลือกใน tree แบบ**รวม subtree** (เลือก parent เห็นกลุ่มของลูกทุกชั้น; root = เห็นทั้งหมด รวมกลุ่มที่ `categoryId == null`)
   - group card เพิ่มบรรทัด/badge `categoryName` จาง ๆ (เมื่อมี) ช่วยยืนยัน context
4. Search ยังใช้ `groupSearch` เดิม — กรอง **ภายใน scope ของ category ที่เลือก** (category ∩ search)
5. การเลือก group (`selectedGroupId`) และ validation `validateScope()` **ไม่เปลี่ยน contract** — payload `groupId` เดิมทุกอย่าง
6. ถ้า `LearnerGroupCategories` ว่าง (ไม่มี category เลย) ให้ render โหมด group แบบเดิม (list เต็มความกว้าง ไม่มี rail) — อย่าโชว์ tree เปล่า ๆ

### §3 แก้ตำแหน่ง toggle Group | Individual

1. ใน `renderTargetScopeStep`: เพิ่ม**แถวคงที่**บนสุดของ step (นอก conditional) — render `renderModeToggle()` ตรงนั้น**ที่เดียว** อาจมี label สั้น ๆ กำกับ เช่น `Target audience:` ด้านซ้าย
2. ลบ `renderModeToggle()` ออกจาก header ของ panel โหมด group (~361) และเลิกส่ง prop `headerLeft` ให้ `LearnerDirectorySelector` (~417) — **ห้ามแก้ตัว component `LearnerDirectorySelector`** (prop เป็น optional อยู่แล้ว แค่ไม่ส่ง)
3. ผลลัพธ์ที่ต้องได้: กดสลับโหมดไป-กลับ ปุ่ม toggle **อยู่พิกัดเดิมเป๊ะ** ไม่ขยับแม้แต่ px เดียว (เนื้อหาใต้แถวเปลี่ยนได้ตามโหมด)

### นอก Scope (ห้ามทำ)

- ห้ามแตะ backend ทุกไฟล์ — endpoint ที่ต้องใช้มีครบแล้ว
- ห้ามแก้ `LearnerDirectorySelector`, `AppTreeView`, `SegmentedToggle`, `AppWizard` (ถ้า treeต้องการ behavior ที่ component ไม่มี ให้จดใน Implementer Notes แล้วทำเท่าที่ component รองรับ)
- ห้ามแตะ step Schedule / Conflict Preview / submit payload (`validate-before-assign`, `Enrollments/BulkAssign` — body เดิมทุก field)
- ห้ามแตะหน้า `LearnerGroupListPage` / `CourseListPage`

## Contract ที่เปลี่ยน

ไม่มี — mirror type เพิ่ม field ตามที่ backend ส่งอยู่แล้วเท่านั้น; ไม่มี endpoint/DTO ใหม่

## Verification

```powershell
cd iLearn.Admin.React; npm run lint; npm run build
```

Manual (dev หรือ QA — `/admin-react/assignments/bulk`):
1. **§1:** dropdown Category โผล่ใน Syllabus Catalog; เลือก category → รายการเหลือเฉพาะคอร์สใน category นั้น + ตัวนับใน header panel สอดคล้อง; พิมพ์ search ต่อ → กรองซ้อนกัน; เลือกคอร์สจนหมด category → ข้อความ empty state สมเหตุผล; คอร์สที่เลือกไว้แล้วอยู่ panel ขวาครบแม้สลับ filter
2. **§2:** step Target Scope โหมด Group เห็น tree category ด้านซ้าย; คลิก parent เห็นกลุ่มของ subtree ทั้งหมด; root เห็นทุกกลุ่ม; search ยังทำงานใน scope; เลือกกลุ่มแล้วกด Continue → validate ผ่าน และ conflict preview/dispatch ทำงานเหมือนเดิม (ทดสอบ dispatch จริงเฉพาะ dev/QA กับกลุ่มทดสอบ — **ห้ามยิงใส่กลุ่มจริง**)
3. **§3:** สลับ Group ↔ Individual หลายครั้ง — ปุ่ม toggle อยู่ตำแหน่งเดิมตลอด ไม่กระโดด
4. Query param เดิมยังทำงาน: เปิด `/assignments/bulk?groupId=<id>` → โหมด group + กลุ่มถูก pre-select (แม้กลุ่มนั้นอยู่ลึกใน tree — root `All Groups` เป็นค่าเริ่มจึงเห็นเสมอ), `?courseId=<id>` → คอร์สถูกเลือกใน panel ขวาแม้ filter default

## Implementer Notes

- Implement ตามแผน PLAN-121 ครบ 3 ส่วนใน `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`:
  1. **§1 Choose Courses Category Filter:** เพิ่ม `CategoryLookup` mirror DTO + โหลด `Categories/lookup`. เพิ่ม `<select>` Category filter ใน Syllabus Catalog panel มี `All Categories` (default), `Uncategorized` (โผล่เฉพาะเมื่อมี course `categoryId == null`), และหมวดหมู่เรียงตาม `sortOrder` (มี prefix `${sortOrder}. ` เมื่อ `sortOrder > 0`) พร้อมแสดงจำนวนคอร์สที่ยังไม่ได้เลือก. เพิ่ม muted text แสดงชื่อหมวดหมู่ในการ์ดคอร์สเมื่อเปิดดูโหมด `All Categories`. Filter ทำงานแบบ `category ∩ search`.
  2. **§2 Target Scope Learner Group Tree:** เพิ่ม `LearnerGroupCategoryLookup` mirror DTO + โหลด `LearnerGroupCategories`. ปรับ Layout โหมด Group เป็น 2 คอลัมน์โดยมี `AppTreeView` ด้านซ้าย แสดง tree หมวดหมู่กลุ่มผู้เรียน และแสดงจำนวนกลุ่มรวม subtree. การเลือกหมวดหมู่ใน tree จะกรองรายชื่อกลุ่มทางขวาแบบรวม subtree recursively (รวม search ภายใน scope). มี fallback เป็น single column หากไม่มี category ในระบบ. การ์ด group แสดง `categoryName` badge เมื่อมี.
  3. **§3 Toggle Position Fix:** เพิ่มแถวคงที่ด้านบนสุดของ Target Scope step สำหรับ render `SegmentedToggle` (`Group | Individual`) พร้อม label `Target audience:` เพื่อให้ตำแหน่งปุ่มสม่ำเสมอไม่ขยับเมื่อสลับโหมด. ถอด toggle ออกจาก panel header และ `LearnerDirectorySelector`.
- Verification:
  - `npm run lint` ผ่าน 0 errors
  - `npm run build` ผ่าน 0 errors (built 1836 modules in 7.02s)

## Reviewer Sign-off (Claude Code, 2026-07-22)

ตรวจโค้ดเทียบสเปคครบทุกข้อ + รัน `npm run lint` / `npm run build` เอง = 0 errors ทั้งคู่ → **REVIEWED**

- §1 ✓ mirror types + คอมเมนต์ path ถูกต้อง (ยืนยัน `LookupCourseDto.cs` มีจริง), filter category ∩ search, Uncategorized conditional, prefix sortOrder, ตัวนับจาก `availableCourses`, panel Selected ไม่ถูกแตะ
- §2 ✓ `categorySubtreeMap` recursion ถูกต้อง (memoize + นับรวม subtree), root เห็นทุกกลุ่มรวม `categoryId == null`, fallback ซ่อน rail เมื่อไม่มี category, การเลือกกลุ่ม/validation/contract เดิมครบ
- §3 ✓ toggle render จุดเดียวในแถวคงที่นอก conditional — เลิกส่ง `headerLeft`; `LearnerDirectorySelector`/`AppTreeView`/`SegmentedToggle` ไม่ถูกแก้ (git diff มีแค่ `BulkAssignPage.tsx`)
- Payload `validate-before-assign` / `Enrollments/BulkAssign` ไม่เปลี่ยนแม้แต่ field เดียว ✓

**Findings (minor — ไม่ block, เก็บรอบถัดไป):**
1. `categoryName` บน group card (~593) เป็น `<span>` pill hand-rolled — ขัดกติกา CLAUDE.md ต้องใช้ `<Badge tone="neutral" variant="soft">`
2. `loadLookups` ยิง 4 fetch sequential — ควรรวมเป็น `Promise.all` ลด latency ตอนเปิดหน้า
3. สลับโหมด Individual↔Group แล้ว highlight ใน `AppTreeView` รีเซ็ต (internal state) แต่ filter ยังค้างตาม `selectedGroupCategoryId` — แก้ต้องเพิ่ม controlled-selection prop ใน shared component (นอก scope แผนนี้ จดไว้เผื่อแผนหน้า)

**คงค้าง:** deploy QA (`tools/deploy-admin-react.ps1`) + manual smoke ตาม Verification ข้อ 1-4 — แล้วค่อยพิจารณา PROD

