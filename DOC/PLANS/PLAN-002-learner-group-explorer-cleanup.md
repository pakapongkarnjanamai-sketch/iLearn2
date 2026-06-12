# PLAN-002: เก็บกวาดหลังย้าย Learner Groups ไปใช้ Explorer

- **Status:** VERIFIED
- **Assigned:** GPT
- **Priority:** Medium
- **Estimated scope:** 3 ไฟล์เล็ก ๆ (ลบ dead code + แก้ป้ายคอลัมน์)

## Problem

หลังเปลี่ยน route `/learner-groups` จาก `EntityListPage` ไปใช้ `LearnerGroupListPage` (Explorer) มีของตกค้าง:

1. **`adminListConfigs.learnerGroups`** ใน `iLearn.Admin.React/src/pages/moduleConfigs.ts` ไม่มีผู้ใช้แล้ว (grep `adminListConfigs.learnerGroups` ทั้ง src = 0 ที่) — รวมถึง logic เฉพาะ `LearnerGroupsCRUD` ใน `EntityListPage.tsx` (crudControllers set, `getRoutePrefix`, gridActions ปุ่ม "Create Group", การโหลด categories lookup สำหรับ LearnerGroupsCRUD) ที่ไม่มีทางถูกเรียกอีก
2. **คอลัมน์ "Division / Updated" ของแถวโฟลเดอร์** ใน `LearnerGroupListPage.tsx` แสดง `createdAt` ของโฟลเดอร์ (โฟลเดอร์ไม่มี updatedAt) — ความหมายคลาดเคลื่อนเล็กน้อย

**หมายเหตุ:** `PaginationParams.CategoryId` / `RootCategoryOnly` ฝั่ง backend ยังไม่มี frontend เรียกใช้ แต่**ให้คงไว้** — เป็น capability สำหรับสลับไป server-side filtering เมื่อข้อมูลโต (ดู Out of scope)

## Scope (ทำแค่นี้)

1. `moduleConfigs.ts`: ลบ config `learnerGroups` ทั้งก้อน
2. `EntityListPage.tsx`: ลบ branch ที่ตายแล้วของ `LearnerGroupsCRUD` —
   - `crudControllers` Set (เหลือแต่ LearnerGroupsCRUD อยู่ตัวเดียว → ลบ Set แล้วให้ `isCrudEnabled = false` คงที่ หรือลบตัวแปรถ้าไม่เหลือผู้ใช้)
   - เงื่อนไข `config.controller === 'LearnerGroupsCRUD'` ทุกจุด (getRoutePrefix, gridActions, categories lookup ใน useEffect, `createRestDataSource` branch)
   - ระวัง: **ContentItemsCRUD ยังใช้ `createRestDataSource` อยู่ — ห้ามลบ branch นั้น**
3. `LearnerGroupListPage.tsx`: แถวโฟลเดอร์ — คอลัมน์วันที่ให้แสดง `createdAt` พร้อมเปลี่ยนความหมายให้ถูก เช่น หัวคอลัมน์เป็น "Division / Created" ไม่ได้เพราะแถว group ใช้ updatedAt → ทางที่ง่ายกว่า: แสดงวันที่ของโฟลเดอร์ใน tooltip/cell เหมือนเดิมแต่ prefix ด้วย "Created " เฉพาะแถวโฟลเดอร์ (หรือแนวทางอื่นที่สื่อความถูกต้องโดยไม่เพิ่ม backend)

## Out of scope (ห้ามแตะ)

- ห้ามลบ `PaginationParams.CategoryId` / `RootCategoryOnly` และ filter ใน `LearnerGroupService.GetPagedAsync` (เก็บไว้ใช้อนาคต)
- ห้ามแตะ `createRestDataSource.ts` และ branch ContentItemsCRUD
- ห้ามเปลี่ยนพฤติกรรม Explorer นอกเหนือจากป้ายวันที่โฟลเดอร์

## Acceptance criteria

- [x] grep `learnerGroups` ใน moduleConfigs.ts ไม่เจอ config ที่ลบ และ `LearnerGroupsCRUD` ใน EntityListPage = 0 ที่
- [x] หน้า list อื่นทุกหน้า (courses, content-library, assignments, learners, learning-logs, enrollments, master-data ทั้ง 4, users) ยังทำงานเหมือนเดิม
- [x] แถวโฟลเดอร์ใน Explorer สื่อชัดว่าวันที่คือวันสร้าง

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด `/assignments`, `/content-library`, `/master-data/divisions` ดูว่าตารางโหลดปกติ + `/learner-groups` explorer ปกติ

## Implementer Notes

- ลบ `adminListConfigs.learnerGroups` ออกจาก `moduleConfigs.ts` ตาม scope
- ลบ branch dead code `LearnerGroupsCRUD` ใน `EntityListPage.tsx` ครบทุกจุด (lookup, route prefix, action button, rest datasource branch, crudControllers set)
- คง branch `ContentItemsCRUD` บน `createRestDataSource` ไว้ตามข้อห้ามในแผน
- ปรับ Explorer คอลัมน์วันที่ให้สื่อความหมายชัดเจน: แถวโฟลเดอร์แสดง `Created <date>`, แถวกลุ่มแสดง `Updated <date>`
- Verification: `npm run lint` (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน
- Smoke (browser): เปิด `/assignments`, `/content-library`, `/master-data/divisions`, `/learner-groups` ได้ตามคาด
- **[Claude/planner review 2026-06-12]** ✅ VERIFIED — diff ลบ branch `LearnerGroupsCRUD` ครบทุกจุด (datasource/route prefix/grid action/categories lookup/crudControllers→`isCrudEnabled=false`), คง `ContentItemsCRUD`+`createRestDataSource` ตามข้อห้าม, import (`Plus`/`AppButton`/`createRestDataSource`) ไม่เหลือ dead, ป้ายวันที่ folder=`Created` group=`Updated` ตรง scope — รัน lint (0 error/10 warning baseline) + build ผ่านอีกครั้ง
