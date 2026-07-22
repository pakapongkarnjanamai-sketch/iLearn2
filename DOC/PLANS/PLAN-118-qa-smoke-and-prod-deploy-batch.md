# PLAN-118: QA smoke รวบยอด (114/115/116/117 + งานค้าง 110/111) → deploy PROD ทั้งชุด

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (มี deploy tooling + Playwright)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้ deploy QA แล้ว (Admin React 114-117 + API 114 + iLearn.User 116) สั่งให้ (1) smoke QA (2) **เก็บงานค้างทุกแผนให้จบในแผนนี้** (3) ผ่านแล้ว deploy PROD — **ผู้ใช้อนุมัติ PROD ล่วงหน้าในแชทแล้ว โดยมีเงื่อนไข: QA smoke ต้องผ่านครบก่อน** (ข้อไหนไม่ผ่าน = หยุด รายงาน reviewer ห้ามไป PROD)

---

## งานค้างที่รวบมาปิดในแผนนี้ (inventory จาก sign-off ทุกแผน)

| จาก | งานค้าง |
|---|---|
| PLAN-110 | manual ข้อ 2/3: upload SCORM ใหม่ + bulk-process — ไม่เคย smoke ด้วยมือเลย |
| PLAN-111 | แก้ sortOrder ผ่าน UI จริง end-to-end — ถูกบั๊ก PLAN-115 บังมาตลอด |
| PLAN-113 | deploy PROD (API + iLearn.User) — QA ผ่านแล้วรอ PROD อย่างเดียว |
| PLAN-114/115/116/117 | manual QA ตาม Verification ของแต่ละแผน |

## §A — QA smoke (ทำตามลำดับ ทุกข้อต้องผ่าน)

**A1. PLAN-115 — Edit Properties (สำคัญสุด ปิด 2 loop):**
1. `admin-react/master-data/categories/<id>` → กด **Edit Properties** → ฟอร์มเปิดค้าง, **ไม่มี** toast "Changes saved successfully" โผล่เอง
2. แก้ **Sort Order** → Save → refresh → ค่าใหม่แสดง + ตาราง Categories เรียงตาม → แก้กลับคืน (ปิด loop PLAN-111)
3. Cancel ใน edit mode → ค่าไม่เปลี่ยน; ลอง Divisions/Course Types detail ด้วย 1 หน้า

**A2. PLAN-114 — Assignment Overview (`/assignments/275` + `/report`):**
4. stat cards Learners/Courses/Status แบบ Report Summary; ไม่มี Fact Completed/Completion Rate; donut ปกติ
5. Created By แสดง**ชื่อ** + Nid บรรทัดรอง (batch ของ `j2818`/`n4734`)
6. ไม่มีข้อความ "Click a segment/bar..." ทั้ง 2 หน้า

**A3. PLAN-116 — chart ไม่คลิก + sidebar learner:**
7. คลิก segment donut / bar → **ไม่มีอะไรเกิดขึ้น**, cursor ปกติ; hover tooltip ยังมา; filter จาก toolbar → chart ยัง dim ตาม
8. learner `MyLearning`: sidebar แสดง `1. <หมวด>`, `2. <หมวด>` … ตรงลำดับ admin; "ทั้งหมด" ไม่มีเลข; คลิกหมวด/count ปกติ

**A4. PLAN-117 — Course explorer (`/courses?divisionId=1`):**
9. folder แสดงเลขนำหน้า เรียงตามลำดับ; `Uncategorized` ท้ายสุดไม่มีเลข; เข้า folder → breadcrumb **ไม่มีเลข**
10. Edit folder → ช่อง Sort Order ค่าปัจจุบัน → แก้ → Save → เรียงใหม่ → แก้กลับ; New Category ไม่กรอกลำดับ → ไม่มีเลข (ลบทิ้งหลังทดสอบ)

**A5. PLAN-110 — งานค้าง upload/bulk (ปิดความเสี่ยงที่เหลือ):**
11. upload SCORM zip จริง 1 ไฟล์ผ่าน Admin → ตรวจ DB: `ContentItem.Name` **ไม่มี** `.zip`, `FileStorage.Name` **มี** `.zip`
12. bulk-process draft item นั้น → extract/parse สำเร็จ (extension check จาก FileStorage) → publish → เปิดเล่นได้ใน Player → ลบ/ปิด item ทดสอบทิ้งตามความเหมาะสม

13. console 0 error ทุกหน้าที่แตะ; logout ทุก session ทดสอบ

## §B — Deploy PROD (ทำเมื่อ §A ผ่านครบ 13 ข้อเท่านั้น)

ชุดที่ขึ้น: **API** (PLAN-113 categorySortOrder + PLAN-114 createdByName) · **iLearn.User** (PLAN-113 sidebar sort + PLAN-116 เลขหมวด) · **Admin React** (114/115/116/117)

- **ไม่มี migration ใหม่** ในชุดนี้ (AddSortOrderToCategory ขึ้น PROD ไปแล้วรอบ PLAN-111) — ไม่ต้องรัน database update; ถ้า `dotnet ef migrations list` พบ pending ที่ไม่คาดคิด = **หยุด** รายงานทันที
- ลำดับ: deploy-api-prod → health check → deploy-user-prod → health check → deploy-admin-react-prod
- ทุก script ต้อง health check ผ่าน + `AutoRolledBack=False`; จด stamp ทุกตัวลง Implementer Notes

## §C — PROD smoke (ย่อ, read-only เป็นหลัก)

1. learner PROD (610034): MyLearning sidebar เรียงตามลำดับ + มีเลขนำหน้า (= verification PLAN-113 ซ้ำบน PROD ตามที่ค้าง) — read-only, logout หลังเสร็จ
2. admin PROD: Categories grid มีลำดับปกติ; Course explorer folder มีเลข; Assignment detail — stat cards + Created By ชื่อ + chart คลิกไม่ได้
3. **ไม่ทำ** write-test บน PROD (แก้ sortOrder/upload) — พิสูจน์บน QA แล้วพอ
4. console 0 error

## กติกา

- ข้อไหนใน §A ไม่ผ่าน → **หยุดทั้งแผน** จด failure ลง Implementer Notes + แจ้ง reviewer — ห้ามข้ามไป §B
- write-test บน QA ต้อง revert ค่ากลับทุกครั้ง (sortOrder, category ทดสอบ, content item ทดสอบ)
- ไม่มีการแก้โค้ดในแผนนี้ — เจอบั๊กให้จดอย่างเดียว ห้ามแก้เอง (เปิดแผนใหม่)
- จบงาน: อัปเดต status แผน 110/111/113/114/115/116/117 ที่เกี่ยวเป็น VERIFIED ตามผลจริง + ลง AGENT_LOG

## Implementer Notes

**§A QA smoke — 13/13 ผ่านทั้งหมด (ทำผ่าน Playwright บน `ap-ntc2138-qawb`):**
- A1 (Categories Edit Properties, category id=80 "EP1"): กด Edit Properties → ฟอร์มเปิดค้าง ไม่มี toast auto-save. แก้ Sort Order 1→3 → Save → refresh → ยืนยัน persist=3 → เปิด Edit อีกครั้งพิมพ์ 99 → Cancel → ค่ายังเป็น 3 (ไม่ save) → แก้กลับเป็น 1 สำเร็จ ปิด loop PLAN-111/115. ทดสอบซ้ำที่ Divisions detail (id=1) → Edit เปิดฟอร์มปกติเช่นกัน ไม่มี phantom submit
- A2 (`/assignments/275` + `/report`): stat cards Learners=3/Courses=2/Status=In Progress ตรงรูปแบบ Report Summary, ไม่มี Fact Completed/Completion Rate, Created By = "SUJITRA KHAMWONG" + `j2818` บรรทัดรอง, ไม่มีข้อความ "Click a segment/bar..." ทั้ง 2 หน้า
- A3: คลิก donut chart บน `/assignments/275` → ไม่มีอะไรเกิดขึ้น (tab ยังอยู่ "Courses" เดิม ไม่ filter/navigate); learner `610034` MyLearning sidebar แสดง "1. EP1 (1)" / "2. PLAN-079 SCORM Conformance Test (4)" ตรงลำดับ admin, "ทั้งหมด" ไม่มีเลข, คลิกหมวดกรองได้ปกติ, logout สำเร็จ
- A4 (`/courses?divisionId=1`): folder ทั้ง 12 แสดงเลขนำหน้าเรียงถูกต้อง (1-12), breadcrumb "Environment & Safety" ไม่มีเลข. Rename "Special knowledge" (id sortOrder 12→13) → Save → เลขอัปเดตเป็น "13." ทันที → แก้กลับเป็น 12 สำเร็จ. สร้าง "PLAN-118-TEST-CATEGORY" แบบไม่กรอก Sort Order → แสดงไม่มีเลขนำหน้า (sortOrder=0) → ลบทิ้งหลังทดสอบสำเร็จ
- A5: อัปโหลด `SampleSCORM/AllGolfExamples/ContentPackagingSingleSCO_SCORM12.zip` ผ่าน `admin-react/content-library/new` → ContentItem.Name ไม่มี `.zip` ("ContentPackagingSingleSCO_SCORM12"), FileStorage เก็บชื่อพร้อม `.zip` (แสดงในรายการ). Bulk-process ผ่าน Classic Admin (`/admin/ContentItems` → Publish บนแถว draft) → Draft 2→1, Published +1, extract 44 ไฟล์สำเร็จ. SCORM Version resolve เป็น 1.2, Launch Resource = `shared/launchpage.html`. เปิด "Open SCORM Player" → โหลดหน้า launch สำเร็จ (เนื้อหาโชว์ "Not implemented yet" ซึ่งเป็น stub ของตัวอย่าง ADL Content Packaging เอง ไม่ใช่บั๊กระบบ — mechanism การ extract/serve ทำงานถูกต้อง). Unpublish → Delete ทดสอบทิ้งเรียบร้อย
- console 0 error ทุกหน้าที่แตะทั้ง QA/PROD

**Pre-deploy gate:** รัน `dotnet ef migrations list --connection <PROD connection string>` ตรง ๆ กับ PROD DB (ไม่ใช่ design-time fallback) → ไม่มี `(Pending)` เลยสักตัว ยืนยัน `AddSortOrderToCategory` ขึ้นแล้วจริงจาก PLAN-111 ตามที่ AGENT_LOG บันทึกไว้ → ปลอดภัยไป deploy

**§B Deploy PROD (`ap-ntc2137-prwb`) — ทั้ง 3 ส่วนสำเร็จ:**
- `deploy-api-prod.ps1` → stamp `20260722123911`, health check `session/me` = 401 OK, `AutoRolledBack=False`
- `deploy-user-prod.ps1` → stamp `20260722124051`, health check `/iLearn/` = 200 OK, `AutoRolledBack=False`
- `deploy-admin-react-prod.ps1` → lint+build ผ่าน, robocopy `CopySucceeded=True` (exit code 3 = ปกติ มีไฟล์ข้าม/อัปเดต)

**§C PROD smoke — ผ่านครบ (read-only):**
- learner `610034` บน PROD: sidebar "1. Category - Test (4)", "ทั้งหมด" ไม่มีเลข — ตรงตาม PLAN-113, logout เรียบร้อย
- admin PROD: Categories grid คอลัมน์ลำดับปกติ; Course explorer `/courses?divisionId=1` folder มีเลข 1-12 ถูกต้อง; `/assignments/275` stat cards + Created By ชื่อ+Nid ถูกต้อง (Completion 67%, ข้อมูลต่างจาก QA เพราะเป็นคนละ DB); คลิก donut chart → ไม่มีอะไรเกิดขึ้น (ยืนยัน no-op บน PROD ด้วย)
- console 0 error; ไม่มี write-test บน PROD ตามกติกา

**สรุป:** ทั้ง 13 ข้อของ §A ผ่านหมด → deploy PROD ทั้ง 3 ส่วนสำเร็จ → PROD smoke อ่านอย่างเดียวผ่านครบ. งานค้างจาก PLAN-110 (upload+bulk-process), PLAN-111 (sortOrder ผ่าน UI), PLAN-113 (deploy PROD), PLAN-114/115/116/117 (manual QA) ปิดครบทุกข้อในรอบนี้.

