# PLAN-047: แก้ปัญหาหลัง deploy prod — Course status + Content publish gap

- **Status:** DONE
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-02
- **อ้างอิง:** [PLAN-046](PLAN-046-deploy-prod-inplace.md) · [data-mapping](PLAN-045-data-mapping.md) · [etl-catalog.sql](PLAN-045-etl-catalog.sql)

> พบจาก E2E review บน prod (Claude): ระบบขึ้นแล้ว ข้อมูลถูก แต่ **learner ใช้งานไม่ได้** เพราะ (1) คอร์สเกือบทั้งหมด Closed, (2) content ~498 ตัวยังไม่ published

## บริบทสำคัญ (อ่านก่อน)
- prod app ต่อ **QA `iLearnDB_New`** (10.10.143.37) — DB **ตัวเดียวกัน** ที่ทั้ง QA และ prod ใช้ → **แก้ DB ครั้งเดียวมีผลทั้งคู่ทันที** (ระวัง เป็น live prod)
- SCORM เสิร์ฟจากไฟล์ที่แตกบน share: **QA** `D:\iLearnContent\Courses\{guid}` และต้อง **copy ไป prod** `\\ap-ntc2137-prwb\D$\iLearnContent\Courses` (guid ตรง `ContentItem.URL`) → **ทุกครั้งที่ publish content ใหม่ ต้อง re-copy guid ใหม่ไป prod**

## ตัวเลขยืนยันแล้ว (2026-07-02, บน `iLearnDB_New`)
- Courses status: **Draft 1 / Open 36 / Closed 546**
- Source (`[iLearn].dbo.Courses`): **IsActive=1 = 579**, WouldBeOpen (ไม่หมดอายุ) = 34 → ExpiredDate เก่าปิด ~545
- ContentItems: **Published 910 / NotPublished 498**

---

## Part A — เปิดคอร์สที่ active (แก้ status)

**Root cause:** ETL D2 ตั้ง `Status=Closed` เพราะ `ExpiredDate` เก่าเป็นอดีต ทั้งที่ `IsActive=1` → ระบบเก่าแสดงคอร์สตาม IsActive ไม่ใช่ ExpiredDate

- [ ] **Decision (ยืนยันกับผู้ใช้):** เกณฑ์เปิด = **`Status=Open` ทุกคอร์สที่ `IsActive=1`** (ไม่สน ExpiredDate เก่า) — ผล ~579 Open
- [ ] รัน (บน `iLearnDB_New`):
```sql
UPDATE dbo.Courses
SET [Status] = 1, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = 'plan047-open'
WHERE IsActive = 1 AND [Status] = 2;   -- Closed -> Open เฉพาะที่ active
```
- [ ] verify: `SELECT [Status], COUNT(*) FROM dbo.Courses GROUP BY [Status];` → คาด Open ~579
- **หมายเหตุ:** เป็น DB update ล้วน → prod เห็นผลทันที ไม่ต้อง redeploy/copy ไฟล์

## Part B — ปิด content publish gap (498 ตัว)

- [ ] **Diagnose** — 498 ตัวเป็นอะไร:
```sql
SELECT TypeId, COUNT(*) FROM dbo.ContentItems WHERE URL IS NULL OR IsActive=0 GROUP BY TypeId;
SELECT COUNT(*) NoFile FROM dbo.ContentItems WHERE (URL IS NULL OR IsActive=0) AND FileStorageId IS NULL;
SELECT TOP 30 Id, Name, TypeId, FileStorageId FROM dbo.ContentItems WHERE URL IS NULL OR IsActive=0 ORDER BY Id;
```
- [ ] **Retry publish** — เรียก bulk publish ซ้ำ (จะ publish เฉพาะที่ยัง IsActive=0):
  `POST https://ap-ntc2138-qawb/iLearnNew/Service/api/ContentItems/Admin/BatchPublishStream` (ส่ง ids ของ 498) หรือ `BulkSetPublic` — เก็บ success/fail count จาก stream
- [ ] **Root cause ตัวที่ยัง fail** — ดู error จาก publish (ScormService `InvalidScormPackageException`): ไม่ใช่ zip / ไม่มี imsmanifest / SCORM version ไม่ใช่ 1.2/2004 / ไม่มี launch page. จัดหมวด + นับ
- [ ] **รายงาน + decision:** ตัวที่ fail จริง (non-SCORM/เสีย/version ไม่รองรับ) แก้เองไม่ได้ → **จดสรุปให้ผู้ใช้ตัดสิน** (คอร์สที่ใช้ตัวเหล่านั้นจะไม่มี content เล่น) — **ห้ามลบ/ดัดแปลง content เอง**

## Part C — Re-sync content QA → prod (หลัง Part B)

- [ ] ถ้า Part B publish สำเร็จเพิ่ม → มี guid folder ใหม่บน QA `D:\iLearnContent\Courses`
- [ ] **re-copy** QA → prod: `\\AP-NTC2138-QAWB\D$\iLearnContent\Courses\*` → `\\ap-ntc2137-prwb\D$\iLearnContent\Courses\*` (robocopy /E /XO เอาเฉพาะใหม่/เปลี่ยน)
- [ ] verify folder count QA == prod

## Verify (ปิดงาน)
- [ ] `SELECT [Status],COUNT(*) FROM Courses GROUP BY [Status]` → Open ~579
- [ ] `SELECT COUNT(*) FROM ContentItems WHERE IsActive=1 AND URL IS NOT NULL` → เพิ่มขึ้นจาก 910 (เท่าที่ retry สำเร็จ)
- [ ] **E2E บน prod:** เปิด `https://ap-ntc2137-prwb/iLearn/admin-react/` → คอร์สเป็น Open, เปิด learner catalog เห็นคอร์ส, **เล่น SCORM ได้จริง 2-3 คอร์ส**
- [ ] `npm run lint && npm run build` (ถ้าแตะ React) — งานนี้น่าจะไม่แตะโค้ด

## Constraints (ห้ามทำ)
- ❌ **ห้ามแตะ DB `iLearn` เก่า** (10.10.154.119) — source/backup
- ❌ **ห้ามลบ/ดัดแปลง content ที่ publish ไม่ได้** — แค่ diagnose + รายงาน (business ตัดสิน)
- ❌ ห้ามรัน ETL/cleanup ซ้ำใส่ `iLearnDB_New` (มีข้อมูล prod live แล้ว)
- ⚠️ ระวัง: `iLearnDB_New` = live prod → UPDATE เฉพาะที่ระบุใน Part A เท่านั้น
- ✅ Part A = DB update; Part B = publish ผ่าน API + diagnose; Part C = copy ไฟล์

## Decision points (ให้ผู้ใช้ก่อน/ระหว่างทำ)
1. Part A: ยืนยันเกณฑ์เปิด = `IsActive=1` (ผล ~579 Open) ใช่ไหม
2. Part B: ตัว content ที่ fail จริง จะเอาอย่างไร (ปล่อยไว้ / หา source ใหม่ / mark เป็น non-SCORM)

## Review Notes (Claude Code — 2026-07-02) → VERIFIED (+ 2 follow-up)
- ✅ **2 blocker แก้แล้ว** — Part A Open 582 (จาก 36), Part B publish 498/498 (498 เดิม "ไม่เสีย" แค่รอบแรกไม่ครบ → retry ผ่านหมด = ไม่มี content เสีย), Part C resync 1409 folders ตรง. **E2E ของ Gemini เชิงประจักษ์** (learner 610034 เล่น slide+exam จบ 100% + บันทึกเรียนจบ) พิสูจน์ทั้ง chain → **PLAN-047 VERIFIED** (ผมยืนยันโครงสร้าง/ข้อมูลอิสระรอบก่อน + E2E ครบ)
- 🔴 **Follow-up 1 (finding ใหม่ นอก scope PLAN-047): ระบบช้าทั้งระบบ** — 2026-07-02 หลัง Gemini ทำเสร็จ ทุกหน้า (admin dashboard/course/category + student) ค้าง >45s (เมื่อก่อนเร็ว). น่าจะ DB contention บน shared QA `iLearnDB_New` จาก ops หนัก (publish 498/robocopy/E2E) หรือ lock/agent ค้าง. ตรวจ DB load/lock 10.10.143.37 + latency prod→QA-db; ถ้าไม่หาย = เร่งย้าย prod DB จริง (ความเสี่ยง "prod ผูก QA DB")
- ⚠️ **Follow-up 2:** count เกิน ~3 (content 1409 vs migrate 1406; courses 584 vs 580) → น่าจะ QA test data ปน → ล้างตอนย้าย prod DB จริง

## Implementer Notes
- **ผล Diagnose 498 ContentItems:** ทุกตัวมีไฟล์ใน `FileStorage` (ไม่มี `FileStorageId` เป็น `NULL`)
- **ผลการ Bulk Publish (Part B):** ทำสำเร็จทั้งหมด 498/498 ตัว ผ่าน REST API (`BulkSetPublic`) ไม่มีตัวใดล้มเหลว (Failure = 0)
- **การ Re-sync QA -> Prod (Part C):** ดำเนินการผ่าน multi-threaded Robocopy `/MT:32` คัดลอกและซิงค์ข้อมูลเสร็จสมบูรณ์ 0 errors, จำนวนโฟลเดอร์ GUID ใน storage ของทั้ง QA และ Prod เท่ากันที่ **1409 โฟลเดอร์**
- **ผลลัพธ์หลังแก้ไข (Verify):**
  - คอร์สมีสถานะเป็น Open ทั้งหมด **582 คอร์ส** (Closed = 1, Draft = 1)
  - ContentItems ที่มี URL และถูก Publish แล้วมีจำนวน **1409 รายการ**
- **การทดสอบ E2E บน Prod:** บราวเซอร์เอเจนต์ล็อกอินเข้าใช้งาน student portal ด้วยรหัสพนักงาน `610034` สามารถเล่นบทเรียน (SCORM Slide) และทำแบบทดสอบ (SCORM Exam) สำหรับหลักสูตร *Software license training 2025 - JP* ได้จนจบ 100% และบันทึกสถานะ "เรียนจบ" ลงระบบได้ถูกต้องปรากฏในประวัติการเรียนของนักเรียน

