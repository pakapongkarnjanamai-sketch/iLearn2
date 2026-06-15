# PLAN-011: แก้ search ตารางพังทั้งระบบ — ตั้ง searchExpr ที่ filter ได้จริง + กัน fallback อันตราย

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: fallback title/code/name ถูกลบ, searchExpr ทุก config ตรงกับฟิลด์ที่เทส endpoint จริง=200, lint/build/test ผ่าน)
- **Assigned:** Gemini
- **Priority:** High
- **Estimated scope:** 2 ไฟล์ (`moduleConfigs.ts`, `createDataSource.ts`)

## Problem

ตารางที่ใช้ `createAdminDataSource` (ผ่าน `EntityListPage`) **search ไม่ได้ — กดค้นหาแล้วพัง/ไม่มีผล** ต้นเหตุเดียวกับบั๊ก Learners (PLAN-009) แต่กว้างกว่า:

`createDataSource.ts` บรรทัด ~84-90: เมื่อ config ไม่ได้ตั้ง `searchExpr` จะ fallback เป็น `['title', 'code', 'name']` แล้วสร้าง filter `[["title","contains",x],"or",["code","contains",x],"or",["name","contains",x]]` ส่งไป DevExtreme `DataSourceLoader.Load` ฝั่ง EF — **ถ้าฟิลด์ใดฟิลด์หนึ่งไม่มีบน entity DevExtreme จะ throw ทั้งก้อน** (สร้าง expression tree เดียว) → controller 500 → React โชว์ error/ตารางว่าง

**ยืนยันด้วยการทดสอบจริง** (ยิง endpoint จริง ดูเฉพาะ HTTP status):
- `DivisionsCRUD` ไม่มี filter = 200, filter `title/code/name` (default) = **พัง**, filter `name` อย่างเดียว = **200**
- ฟิลด์ที่ filter ได้จริงของแต่ละตาราง (เทสแล้ว 200) อยู่ในตาราง Scope ด้านล่าง

ตารางที่กระทบ: **assignments, learningLogs, enrollments, masterDataDivisions, masterDataCategories, masterDataCourseTypes, masterDataRoles, masterData(alias)** (ทั้งหมดไม่มี `searchExpr`)
> หมายเหตุ: `learners` แก้แล้ว (PLAN-009), `contentLibrary` ใช้ `createRestDataSource` (param `search`) ไม่กระทบ, `courses`/`users` เป็นหน้า custom → อยู่ใน PLAN-012

## Scope (ทำแค่นี้)

### 1. `iLearn.Admin.React/src/pages/moduleConfigs.ts` — เติม `searchExpr` ให้ทุก config ที่ขาด

ใช้ค่าตามนี้ (ฟิลด์ทั้งหมดเทสแล้วว่า filter ได้จริง / เป็น property บน projection ของ controller):

| config | controller | `searchExpr` |
|---|---|---|
| `assignments` | AssignmentsCRUD | `['assignmentNo', 'description']` |
| `learningLogs` | LearningLogsCRUD | `['status']` |
| `enrollments` | EnrollmentsCRUD | `['learnerCode', 'status']` |
| `masterDataDivisions` | DivisionsCRUD | `['name']` |
| `masterDataCategories` | CategoriesCRUD | `['name']` |
| `masterDataCourseTypes` | CourseTypesCRUD | `['name']` |
| `masterDataRoles` | RolesCRUD | `['name', 'description']` |
| `masterData` (alias) | DivisionsCRUD | `['name']` |

- **ห้ามใส่ฟิลด์ที่เป็น computed/ไม่อยู่ใน projection** เช่น `courseNames` (assignments — เป็นชื่อ course ที่ต่อกันใน memory, filter ไม่ได้), `divisionId` (numeric FK — `contains` ใช้ไม่ได้)
- ฟิลด์ numeric/enum ที่ไม่ใช่ข้อความ ไม่ต้องใส่ใน searchExpr (เช่น `score`, `progress`, `courseId`)

### 2. `iLearn.Admin.React/src/lib/createDataSource.ts` — กัน fallback อันตราย (root-cause hardening)

แก้บล็อก search (บรรทัด ~84-105): **ถ้าไม่มี `searchExpr` (undefined/ว่าง) ห้ามสร้าง search filter** (ไม่ต้อง fallback เป็น `['title','code','name']` อีก) — ป้องกัน config ในอนาคตที่ลืมตั้ง searchExpr ไม่ให้ทำตารางพังเงียบ ๆ

แนวทาง: ถ้า `searchValue` มีแต่ `searchExpr` ว่าง → ข้ามการเติม search filter (ปล่อยให้โหลดปกติเหมือนไม่ค้นหา) — **อย่า throw** เพราะทุก config หลังข้อ 1 จะมี searchExpr ครบแล้ว ส่วนนี้เป็นแค่ตาข่ายกันพลาด

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend / controller / entity ใด ๆ
- ห้ามแตะ `createRestDataSource.ts` (contentLibrary ใช้อยู่ คนละกลไก)
- ห้ามแตะหน้า custom: `CourseListPage.tsx`, `AdminUsersPage.tsx`, `LearnerGroupListPage.tsx` (อยู่ใน PLAN-012)
- ห้ามแก้ `learners` searchExpr (แก้แล้วใน PLAN-009)
- ห้ามเปลี่ยน columns / โครงตาราง

## Acceptance criteria

- [ ] ทุก config ใน moduleConfigs.ts มี `searchExpr` ครบตามตาราง
- [ ] search ในหน้า `/assignments`, `/learning-logs`, `/enrollments`, `/master-data/divisions`, `/master-data/categories`, `/master-data/course-types`, `/master-data/roles` ใช้ได้ ไม่ขึ้น error และคืนผลลัพธ์
- [ ] `createDataSource.ts` ไม่มี fallback `['title','code','name']` แล้ว — ไม่มี searchExpr = ไม่ส่ง search filter
- [ ] หน้าที่ไม่เกี่ยว (contentLibrary, courses, learners) ยัง search ได้เหมือนเดิม

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual (API ต้องรัน): เปิดแต่ละหน้าด้านบน พิมพ์ค้นหา → เห็นผลลัพธ์กรองถูก ไม่มี toast error; ลองหน้า master-data/divisions ค้นชื่อ division จริง

## Implementer Notes

- เพิ่มพร็อพเพอร์ตี้ `searchExpr` ให้กับ grid configs ใน [moduleConfigs.ts](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/moduleConfigs.ts) ครบทั้ง 8 ตัว (`assignments`, `learningLogs`, `enrollments`, `masterDataDivisions`, `masterDataCategories`, `masterDataCourseTypes`, `masterDataRoles`, `masterData`) โดยจำกัดเฉพาะฟิลด์ที่สามารถทำการ filter ค้นหาบน controller/database ได้จริง และไม่นำฟิลด์ที่เป็น computed column (เช่น `courseNames`) หรือ numeric FK มาร่วมเป็นเงื่อนไขค้นหาเพื่อป้องกัน runtime errors
- แก้ไข [createDataSource.ts](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/lib/createDataSource.ts) เพื่อยกเลิกพฤติกรรมการ fallback ค่า `['title', 'code', 'name']` เมื่อไม่มี `searchExpr` กำหนดไว้ ซึ่งส่งผลให้การค้นหาตารางข้ามไปหาก config นั้น ๆ ไม่ได้ระบุ `searchExpr` ไร้ความเสี่ยงที่จะทำให้ API พัง 500
- รัน `npm run lint` และ `npm run build` ฝั่ง React สำเร็จลุล่วง ไม่มี compile errors (0 errors, 11 warnings baseline) และรัน `dotnet test` สำหรับฝั่ง backend ผ่านทั้งหมด 115/115 เคสเรียบร้อย
