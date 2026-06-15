# PLAN-020: เปลี่ยนหน้า Courses เป็น Explorer (Division → Category → Course) แบบ Learner Group Explorer

- **Status:** VERIFIED ✅ (Claude review 2026-06-15 — เจอ+แก้ data bug 1 จุด ดู note ท้ายไฟล์)
- **Assigned:** Gemini
- **Priority:** Medium
- **Estimated scope:** เขียนใหม่ `CourseListPage.tsx` 1 ไฟล์ (frontend ล้วน — ไม่แตะ backend)

## Problem / เป้าหมาย

ผู้ใช้ต้องการให้หน้า `/courses` ทำงานแบบ **Explorer** เหมือน Learner Group Explorer (`iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`) — เดินเข้าโฟลเดอร์ทีละชั้น มี breadcrumb trail, drill-in/back, ค้นหาภายในโฟลเดอร์ปัจจุบัน — แทนของเดิมที่เป็น tree sidebar ซ้าย + grid ขวา

## โครงสร้างข้อมูล (สำคัญ — ต่างจาก Learner Group)

Learner Group: category ซ้อนตัวเอง (parentId) ชั้นเดียว
**Courses: Division → Category → Course (2 ชั้นโฟลเดอร์ แล้วเป็น course)** — ยืนยันจาก entity:
- `Category` (`iLearn.Domain/Entities/Category.cs`): มี `DivisionId` (**ไม่มี ParentId** — ไม่ซ้อนตัวเอง) สังกัด 1 division
- `Course` (`iLearn.Domain/Entities/Course.cs`): มี `CategoryId` (+ category มี divisionId)

ดังนั้น Explorer มี 3 ระดับ:
| ระดับ | URL | แสดงเป็นโฟลเดอร์/ไอเทม |
|---|---|---|
| root | ไม่มี param | **Divisions** (โฟลเดอร์) |
| ในแผนก | `?divisionId=N` | **Categories** ของ division N (โฟลเดอร์) |
| ในหมวด | `?categoryId=M` | **Courses** ของ category M (ไอเทม leaf → ดับเบิลคลิกไป `/courses/:id`) |

(breadcrumb ของ `?categoryId=M` ย้อนหา division จาก `category.divisionId`)

## Backend ที่มีอยู่แล้ว (ใช้ได้ ห้ามแก้)

- `GET api/Courses?isActive=false` → `{ success, data: CourseDto[] }` โหลด course **ทั้งหมด** (มี division isolation ฝั่ง admin ให้แล้ว) — `CourseDto` (`iLearn.Application/DTOs/CourseDto.cs`) มี: `id, code, title, status, statusName, courseTypeId, typeName, categoryId, categoryName, divisionId` (camelCase หลัง serialize) — **ใช้ `isActive=false` เพื่อให้เห็นทุกสถานะ** (Draft/Open/Retired) ในมุมมอง admin
- `GET admin/DivisionsCRUD/Get` → `{ data: {id,name}[] }` รายชื่อ division (มี isolation)
- `GET admin/CategoriesCRUD/Get` → categories (มี `id, name, divisionId`)
- `GET api/Courses/course-types-lookup` → `[{id,name}]` (chip filter ประเภท — มีใช้อยู่เดิม)

> โหลดทั้งหมดครั้งเดียวแล้ว build แผนผังใน memory เหมือน LearnerGroupListPage (Promise.all 3-4 ก้อน)

## Scope (ทำแค่นี้)

เขียน `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx` ใหม่ ตาม pattern ของ `LearnerGroupListPage.tsx`:

1. **State/URL:** ใช้ `useSearchParams` — อ่าน `divisionId` / `categoryId` (ตามตารางบน) กำหนด "ตำแหน่งปัจจุบัน"; ไม่มี param = root
2. **โหลดข้อมูล:** `Promise.all` ดึง divisions + categories + courses (+ course types) → เก็บ state แล้ว build map: `categoriesByDivision`, `coursesByCategory`, `divisionsById`, `categoriesById` (เลียน `categoriesByParent`/`groupsByCategory`)
3. **`currentItems`** (เลียนของเดิม): 
   - root → divisions เป็นโฟลเดอร์ (countText = `${จำนวน category} categories`)
   - ใน division → categories เป็นโฟลเดอร์ (countText = `${จำนวน course} courses`)
   - ใน category → courses เป็นไอเทม (แสดง code + status badge + type; ดับเบิลคลิก/คลิก → `navigate('/courses/'+id)`)
   - โฟลเดอร์ขึ้นก่อน ไอเทมทีหลัง, sort ตามชื่อ (ใช้ `sortByNameAsc` แบบเดียวกัน)
4. **Search:** ช่องค้นหา client-side กรอง `currentItems` ในโฟลเดอร์ปัจจุบัน (เลียน `filteredItems`)
5. **Breadcrumb:** `useBreadcrumbs().setCustomCrumbs` — `Courses → [Division] → [Category]` (ตั้ง `to` ให้คลิกย้อนได้) + cleanup `setCustomCrumbs(null)` ตอน unmount (ลอกจาก LearnerGroupListPage บรรทัด ~230-261)
6. **Navigate/back:** คลิกโฟลเดอร์ = setSearchParams ระดับถัดไป; ปุ่ม back = ขึ้นระดับบน (root←division←category) ; reset searchTerm เมื่อย้ายโฟลเดอร์
7. **Deep-link guard:** ก่อน validate `divisionId`/`categoryId` ต้องรอ data โหลดเสร็จ (`loading || ว่าง` ให้ return ก่อน) — **สำคัญ** (เคยเป็นบั๊กใน LearnerGroupListPage ที่เด้งกลับ root ถ้า validate ก่อนโหลด ดูบรรทัด ~219-228)
8. **คงฟีเจอร์เดิมที่ยังมีประโยชน์:** ปุ่ม **Create Course** (`/courses/new`), chip filter **Course Type** — ให้ chip กรอง courses แบบ client-side เฉพาะตอนอยู่ในมุมมอง category (ถ้าอยู่ root/division จะซ่อน chip ได้)
9. ใช้ shared components ตามกติกา: `DataGridSurface`, `LoadingState`, `formatDate`, ไอคอน `Folder`/`Plus`/`Search`/`ChevronLeft` จาก lucide — โฟลเดอร์/ไอเทม render สไตล์เดียวกับ LearnerGroupListPage (การ์ดในการ์ด `rounded-lg border border-slate-200 bg-white`)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend (`CoursesController`, `DivisionsController`, entity, DTO) — โครงสร้างมีพอแล้ว
- ห้ามแตะ `LearnerGroupListPage.tsx` (ต้นแบบ — อ้างอิงเฉย ๆ)
- ห้ามแตะหน้า course detail/editor/version (`CourseDetailPage`, `CourseEditorPage`, `VersionFormPage`)
- ห้ามลบ component `AppTreeView` (learner-group relocate modal ยังใช้อยู่ — แค่เลิกใช้ใน CourseListPage)
- **ยังไม่ต้องสกัด shared `<Explorer>` component** ในงานนี้ (จะมี explorer 2 หน้าแล้ว = ควรสกัดร่วม แต่เป็นแผนต่อไปแยก เพื่อไม่ให้ destabilize learner groups — จดเป็น follow-up ใน Implementer Notes)

## ข้อควรระวัง / edge case

- **Course ที่ไม่มี category** (`categoryId = 0`/ไม่มี division): จัดเป็นโฟลเดอร์ pseudo "Uncategorized" ที่ root หรือซ่อน — เลือกวิธีที่สมเหตุผลแล้วจดใน Notes
- จำนวน courses อาจมาก: โหลดทั้งหมด client-side แบบเดียวกับ learner groups (ยอมรับได้) — ถ้าพบว่าช้ามากจริงค่อยเสนอ server-side ใน follow-up
- route `/courses` ใน `App.tsx` ใช้ `<CourseListPage />` อยู่แล้ว ไม่ต้องเพิ่ม route (เป็น list page ไม่ต้อง Remount)

## Acceptance criteria

- [x] `/courses` เริ่มที่ root แสดง Divisions เป็นโฟลเดอร์
- [x] คลิก division → เห็น categories; คลิก category → เห็น courses; ดับเบิลคลิก course → ไป `/courses/:id`
- [x] breadcrumb แสดง `Courses / Division / Category` และคลิกย้อนได้; ปุ่ม back ขึ้นระดับบนถูก
- [x] deep-link `/courses?divisionId=N` และ `/courses?categoryId=M` เปิดตรงโฟลเดอร์ได้ (refresh แล้วไม่เด้งกลับ root); id ปลอม → กลับ root
- [x] ค้นหาในโฟลเดอร์ปัจจุบันได้, chip Course Type กรอง courses ได้
- [x] ปุ่ม Create Course ยังทำงาน
- [x] division isolation ถูก (admin เห็นเฉพาะ division ตัวเอง — backend กรองให้แล้ว)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: ไล่ root → division → category → course; deep-link ทั้ง 2 แบบ + refresh; breadcrumb คลิกย้อน; ค้นหา; chip type; Create Course

## Implementer Notes

- **วิธีจัดการ Uncategorized**: สำหรับ Course ที่ไม่มี `categoryId` (หรือ `categoryId === 0`) จะถูกจัดเก็บอยู่ใน pseudo-Division และ pseudo-Category พิเศษชื่อว่า `"Uncategorized"` เสมอ เพื่อให้การเดินในโฟลเดอร์มีความลึก 3 ชั้นเสถียรเท่ากันทุกคอร์ส (ไม่มีการลัดลำดับชั้น)
  - ที่ Root level: ถ้าพบว่ามี uncategorized courses จะแสดง pseudo-Division Folder ชื่อ `"Uncategorized"` (`id = 0`)
  - ที่ pseudo-Division Level: แสดง pseudo-Category Folder ชื่อ `"Uncategorized"` (`id = 0`)
  - ที่ Category Level: แสดงรายการวิชาที่ uncategorized ทั้งหมด และรองรับการกรองตาม Course Type Chip filter ตามปกติ
- **สกัด shared `<Explorer>` component**: มีหน้าจอ explorer สองหน้าแล้วในระบบ (`LearnerGroupListPage` และ `CourseListPage`) ที่ใช้ pattern, table markup, client-side search, deep-link guard, และ breadcrumb context เหมือนกัน ในอนาคตควรทำการสกัด logic และ markup ออกมาเป็น reusable generic `<Explorer>` component หรือ `useExplorer` hook เพื่อขจัด code duplication ครับ

---

## [Claude/review+hotfix 2026-06-15] เจอ + แก้ data bug (จากความผิดพลาดของแผนเอง)
- **บั๊ก:** แผนสั่งโหลด `Courses?isActive=false` แต่ตรวจ `CourseService.GetAllCoursesAsync(isActive)` แล้วพบว่า `isActive=true` คืน **เฉพาะ Open**, `isActive=false` คืน **เฉพาะ Draft/Closed/Retired** — ไม่มีค่าไหนคืน "ทั้งหมด" → explorer จะ**ขาด course ที่ Open (published) ทั้งหมด** ซึ่งสำคัญสุด (เป็นความผิดของแผนที่ assume `isActive=false`=ทั้งหมดโดยไม่ตรวจ service)
- **แก้ (Claude hotfix):** `loadData` ยิง 2 call (`isActive=true` + `isActive=false`) แล้ว merge — 2 ชุด disjoint ไม่มี dup, division isolation ครบทั้งคู่ ไม่ต้องแตะ backend
- **Verified:** endpoint จริงทั้ง 5 = HTTP 200, `npm run build` ผ่าน, `npm run lint` 0 errors; รีวิว logic 3-level nav / deep-link guard / breadcrumb / uncategorized / chips ครบถูกต้อง
- ที่เหลือทำดี: deep-link guard รอ data, breadcrumb cleanup, uncategorized pseudo-folder, type chip เฉพาะ category view

