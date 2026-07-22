# PLAN-113: learner sidebar เรียงหมวดหมู่ตาม Category.SortOrder (ปิด limitation ของ PLAN-111)

- **Status:** QA VERIFIED — รอผู้ใช้ยืนยัน deploy PROD
- **Assigned:** GitHub Copilot (API + learner view — งานเล็ก ทำคนเดียวทั้งสองฝั่ง)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** limitation ที่จดไว้ใน PLAN-111 Sign-off — admin แก้ `Category.SortOrder` แล้ว แต่ sidebar หมวดหมู่ฝั่ง learner ยังเรียงตาม **categoryId** (JS วน `Object.keys()` ที่ key เป็นเลข) ⇒ ผู้เรียนไม่เห็นลำดับใหม่. ผู้ใช้ยืนยันให้เปิดแผนแก้แล้ว (2026-07-22)
- **อ่าน CLAUDE.md หัวข้อ Backend ก่อนเริ่ม**

---

## วินิจฉัย (ยืนยันจากโค้ด)

- **API:** `EnrollmentsController.GetCourseCatalog` (บรรทัด 153-201) — query `Include("Category,...")` อยู่แล้ว, project เป็น `LearnerCourseCatalogDto` ที่มี `CategoryId`/`CategoryName` แต่**ไม่มี sortOrder** ⇒ ฝั่ง learner ไม่มีข้อมูลให้เรียง
- **Learner JS** (`MyLearning/Index.cshtml`):
  - `organizeCoursesByCategory` (~1443) สร้าง `categorizedCourses` เป็น **object** key = catId
  - `renderCategorySidebar` (~1461) วน `Object.keys(categorizedCourses)` — JS spec บังคับ key ที่เป็น integer-like เรียง **ascending ตามตัวเลข** ⇒ sidebar = เรียงตาม categoryId เสมอ ไม่มีทาง override ผ่าน object
- ไม่มี migration/DB เพิ่ม — `Category.SortOrder` มีแล้วจาก PLAN-111

## Scope

### §1 (API) — เพิ่ม `categorySortOrder` ใน catalog DTO **[CONTRACT เล็ก — additive]**

- `LearnerCourseCatalogDto` เพิ่ม `public int CategorySortOrder { get; set; }`
- projection ใน `GetCourseCatalog`: `CategorySortOrder = c.Category?.SortOrder ?? 0`
- **ห้ามเปลี่ยน field เดิม/ordering ของ courses ใน response** (ยังเรียง `Code, Title` เหมือนเดิม — ลำดับหมวดเป็นหน้าที่ฝั่ง client)

### §2 (learner JS) — sidebar เรียงตาม (sortOrder, id)

- `organizeCoursesByCategory`: เก็บ `sortOrder: course.categorySortOrder ?? 0` ลง entry ของแต่ละ category (ค่าแรกที่เจอพอ — ทุก course ในหมวดเดียวกันมีค่าเดียวกัน)
- `renderCategorySidebar`: เลิกวน `Object.keys(...)` ตรง ๆ → สร้าง array จาก `Object.values(categorizedCourses)` (ข้าม `'all'`) แล้ว `sort((a,b) => (a.sortOrder - b.sortOrder) || (a.id - b.id))` ก่อน render
  - `id` tiebreak รองรับ sortOrder ซ้ำ (PLAN-111 ไม่บังคับ unique)
  - `?? 0` รองรับ deploy skew (ถ้า iLearn.User ขึ้นก่อน API — sortOrder undefined ทุกตัว ⇒ fallback เรียงตาม id = พฤติกรรมเดิม ไม่พัง)
- แถว "ทั้งหมด" ยังอยู่บนสุดเหมือนเดิม
- **ไม่แตะ** ลำดับ course ภายในหมวด / filter pills / นับ course-count / click handler

### นอก Scope (ห้ามทำ)

- ห้ามแสดงเลขนำหน้าชื่อหมวดฝั่ง learner (`sortOrder + ". " + name`) — ยังเป็น option ที่ผู้ใช้ไม่ได้สั่ง (PLAN-111 §4)
- ห้ามแตะ ordering ฝั่ง admin (ทำครบแล้วใน PLAN-111)
- ห้ามแตะ `GetPlayerInfoByCourse` / DTO อื่น
- ไม่มี migration

## Contract ที่เปลี่ยน

- `LearnerCourseCatalogDto` +`categorySortOrder` (int, additive) — consumer เดียวคือ `MyLearning/Index.cshtml` แก้ในแผนเดียวกัน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Manual (QA):
1. learner เปิด MyLearning → sidebar เรียงตามลำดับที่เห็นในหน้า admin Categories (division ของ learner คนนั้น)
2. **ทดสอบตัวจริง:** admin สลับ sortOrder ของ 2 หมวดใน division เดียวกัน → learner refresh → sidebar สลับตาม (ปิด loop manual ข้อ 5-6 ของ PLAN-111 ที่ค้างไปด้วย) → สลับกลับคืนหลังทดสอบ
3. คลิกหมวดใน sidebar → course list ถูกต้อง, filter pills + count ปกติ, console 0 error

## Deploy note

- แตะ **API + iLearn.User** (ไม่มี migration) — deploy API ก่อนหรือพร้อม iLearn.User (ลำดับกลับกันก็ไม่พังเพราะ `?? 0` fallback)
- QA → verify → PROD (รอผู้ใช้ยืนยัน)

## Implementer Notes

- **§1:** `LearnerCourseCatalogDto` +`CategorySortOrder` (int, additive); `EnrollmentsController.GetCourseCatalog` projection: `CategorySortOrder = c.Category?.SortOrder ?? 0` — ไม่แตะ ordering ของ courses ใน response (ยัง `OrderBy(Code).ThenBy(Title)`)
- **§2:** `organizeCoursesByCategory` เก็บ `sortOrder: course.categorySortOrder ?? 0` ลง entry ต่อ category; `renderCategorySidebar` เปลี่ยนจาก `Object.keys(categorizedCourses).forEach` เป็น `Object.values(categorizedCourses).filter(cat => cat.id !== undefined && String(cat.id) !== 'all').sort((a,b) => (a.sortOrder - b.sortOrder) || (a.id - b.id))` ก่อน render — `'all'` entry (raw array ไม่มี `.id`) ถูก filter ออกด้วยเงื่อนไข `cat.id !== undefined` เอง; แถว "ทั้งหมด" ยัง render แยกอยู่บนสุดเหมือนเดิม ไม่ได้อยู่ใน loop นี้
- **Build/Test:** `dotnet build iLearn.Tests` 0 errors, `dotnet test` 222/222 ผ่าน (ไม่มี test ใหม่ — ตามแผนไม่ได้บังคับ, การเปลี่ยนเป็น additive field)
- **QA Deploy:** `tools/deploy-api.ps1` (stamp `_deploy_20260722105526`) + `tools/deploy-user.ps1` (stamp `_user_deploy_20260722105719`) สำเร็จทั้งคู่บน `AP-NTC2138-QAWB`
- **QA Manual Verify (ข้อ 1-3 ครบ):**
  1. Learner `610034` (CSD - SA) เปิด MyLearning → sidebar เรียง "EP1" (sortOrder 1) ก่อน "PLAN-079 SCORM Conformance Test" (sortOrder 2) ตรงกับ admin Categories grid
  2. **ทดสอบตัวจริงตามที่แผนสั่ง:** เรียก `PUT api/admin/CategoriesCRUD/Put` สลับ `EP1` (id 80) → sortOrder 2 และ `PLAN-079 SCORM Conformance Test` (id 82) → sortOrder 1 ผ่าน browser session ที่ login เป็น admin จริง → refresh MyLearning → **sidebar สลับลำดับตามทันที** (PLAN-079 ขึ้นก่อน EP1) ยืนยัน dynamic ordering ทำงานถูกต้อง end-to-end → สลับกลับคืนค่าเดิม (EP1=1, PLAN-079=2) หลังทดสอบเสร็จ — ปิด loop manual ที่ค้างของ PLAN-111 ไปด้วย
  3. คลิกหมวดใน sidebar → course list ถูกต้อง, filter pills + count ปกติ, console ไม่มี error
- **หมายเหตุ:** ระหว่างหา category id ของ "PLAN-079 SCORM Conformance Test" พิมพ์ id ผิดหนึ่งครั้ง (แก้ `Category.SortOrder` ของ "Special knowledge" id=81 แทน) — ตรวจพบและ revert กลับเป็น 12 ทันที (ค่าที่สอดคล้องกับลำดับ backfill เดิมของ division นั้น) ก่อนไปแก้ id ที่ถูกต้อง (82) — ไม่กระทบข้อมูลจริงหลังแก้ไข
- **Edit Properties button บน Category Detail page (Admin React):** ลองกดแล้วไม่เปิดฟอร์มแก้ไขที่มองเห็นได้ (ปุ่มดูเหมือนไม่ทำงานหรือ toggle เงียบ) — ใช้ direct API PUT แทนสำหรับ manual test นี้ **นอก scope ของ PLAN-113** แต่ควรมีคนตรวจสอบ UI ปุ่มนี้ในแผนแยก
- **คงค้างก่อน deploy PROD:** รอผู้ใช้ยืนยัน (ตาม Deploy note ของแผน)

