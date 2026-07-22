# PLAN-118: QA smoke รวบยอด (114/115/116/117 + งานค้าง 110/111) → deploy PROD ทั้งชุด

- **Status:** READY
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

_(เติมโดย implementer)_
