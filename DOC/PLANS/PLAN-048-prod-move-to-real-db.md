# PLAN-048: ย้าย prod จาก QA DB → Production DB จริง (10.10.154.119)

- **Status:** DONE -> VERIFIED (reconciled 2026-07-17)
- **Assigned:** GitHub Copilot (GPT)  _(deploy/infra owner; ผู้ใช้ reroute ได้)_
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-02
- **อ้างอิง:** [PLAN-046](PLAN-046-deploy-prod-inplace.md) · [PLAN-047](PLAN-047-prod-post-deploy-fixes.md)

> เป้าหมาย: ตัด prod ออกจากการพึ่ง **QA `iLearnDB_New` (10.10.143.37)** → ย้ายไป **prod DB จริงบน `10.10.154.119` (AP-NTC2139-COSS)** เพื่อแก้ (1) ระบบช้าจาก DB contention ที่ share กัน, (2) ตัดความเสี่ยง prod ผูก QA infra, (3) ล้าง QA test data ปน

## บริบท / สถานะปัจจุบัน
- prod app (`/iLearn/*` บน ap-ntc2137-prwb) ต่อ **QA `iLearnDB_New`** อยู่ — ทำงานครบแล้ว (582 Open courses, 1409 content published, E2E ผ่าน)
- Content ที่แตกแล้วอยู่บน **prod storage** `D:\iLearnContent\Courses\{guid}` = 1409 folders (guid ตรงกับ `ContentItem.URL` ใน QA DB)
- Old prod DB `iLearn` (10.10.154.119) = ของเก่า — **ห้ามแตะ** (source/backup)

## กลยุทธ์ = Backup / Restore (ไม่ต้อง ETL ใหม่)
Restore สำเนา QA `iLearnDB_New` (HEAD schema + `__EFMigrationsHistory` + CourseTypes + ข้อมูล + ทุก fix จาก PLAN-047) ไป prod server → เร็ว, ตรงเป๊ะ, ไม่ต้อง re-apply fix
- ✅ **Content ไม่ต้อง copy ใหม่** — guid ใน DB ที่ restore = guid ที่มีบน prod storage อยู่แล้ว → เล่นได้ทันที
- ✅ ไม่ต้อง provision schema/CourseTypes เอง (มากับ restore หมด)

---

## Steps

### 1. Pre-flight
- [ ] ยืนยัน sa access + สิทธิ์ create/restore DB บน `10.10.154.119`
- [ ] ที่เก็บ `.bak` (share ที่ทั้ง 2 SQL server เข้าถึง หรือ copy ไฟล์ข้ามเครื่อง)
- [ ] **Maintenance window สั้น ๆ** — ดู §ความเสี่ยง (learner progress ระหว่าง backup→switch)
- [ ] **Decision:** ชื่อ prod DB = `iLearnDB_New` (ยืนยัน) · connection secret จะจัดการแบบไหน (ดู step 4)

### 2. Backup QA DB (จับ point-in-time ที่นิ่ง)
- [ ] วาง `app_offline.htm` ที่ prod (หรือทำช่วงไม่มี learner) เพื่อ **freeze write** กัน progress หายระหว่าง backup→cutover
- [ ] `BACKUP DATABASE [iLearnDB_New] TO DISK='...\iLearnDB_New_toProd.bak' WITH COPY_ONLY, COMPRESSION;` (บน 10.10.143.37)

### 3. Restore ไป prod server
- [ ] copy `.bak` ไป 10.10.154.119 → `RESTORE DATABASE [iLearnDB_New] FROM DISK='...' WITH MOVE ... , RECOVERY;`
- [ ] **(optional) ล้าง QA test data ปน (~3 record)** — เฉพาะที่ระบุตัวได้ชัด (courses 584 vs migrate 580, content 1409 vs 1406). ถ้าระบุไม่ชัด **ปล่อยไว้** (ไม่กระทบการใช้งาน) — อย่าเดาลบ
- [ ] verify restored DB = QA:
```sql
SELECT COUNT(*) FROM dbo.Categories;                                  -- 40
SELECT [Status],COUNT(*) FROM dbo.Courses GROUP BY [Status];          -- Open ~582
SELECT COUNT(*) FROM dbo.ContentItems WHERE IsActive=1 AND URL IS NOT NULL;  -- 1409
-- FK integrity spot check (course_bad_category / contentitem_bad_filestorage = 0)
```

### 4. เปลี่ยน connection string ของ prod → prod DB
prod app ใช้ `appsettings.Production.json` (env=Production). ปัจจุบัน `iLearn.API/appsettings.Production.json` **ไม่มี** ConnectionStrings → fallback base (QA). ต้อง override เป็น prod:
```jsonc
// iLearn.API/appsettings.Production.json  (เพิ่ม)
"ConnectionStrings": {
  "DefaultConnection": "Data Source=10.10.154.119;Database=iLearnDB_New;Persist Security Info=True;User ID=sa;Password=<prod-sa>;Trust Server Certificate=True"
}
```
- [ ] **Decision (secret):** เสนอใช้ **environment variable** `ConnectionStrings__DefaultConnection` ตั้งที่ app pool `iLearnService` (ไม่ commit secret ลง repo) — override config อัตโนมัติ · **หรือ** ใส่ในไฟล์ `appsettings.Production.json` ที่ deployed บน prod (deploy-safe แต่ถ้า commit = secret เข้า git; base appsettings.json ก็ commit QA sa อยู่แล้ว)
- [ ] apply ค่าไปที่ **prod Service** (deployed `Service/appsettings.Production.json` หรือ env var) → **restart app pool `iLearnService`**
- [ ] เฉพาะ **API** ที่ต่อ DB (User/Admin เรียกผ่าน API) → แก้ที่ API พอ

### 5. Verify (ปิดงาน)
- [ ] ยืนยัน prod ต่อ prod DB จริง (เช่น smoke `session/me` 200 + ตรวจว่าไม่ใช่ QA — เช็ค log/DB หรือ query `SELECT @@SERVERNAME`)
- [ ] **E2E prod:** catalog เห็นคอร์ส Open, **เล่น SCORM ได้ 2-3 คอร์ส** (content guid ตรง storage เดิม), learner completion บันทึกลง **prod DB**
- [ ] ยืนยัน **ระบบเร็วขึ้น** (หลุด contention จาก QA) — เทียบกับอาการค้างเดิม
- [ ] ตรวจว่า write (learner progress) ลง prod DB ไม่ใช่ QA แล้ว

### 6. Post-cutover
- [ ] เอา `app_offline.htm` ออก / เปิดระบบ
- [ ] **เก็บ QA `iLearnDB_New` ไว้เป็น fallback** จนกว่า prod DB นิ่ง (อย่าเพิ่งลบ/ใช้ทำ QA test) — ถ้ามีปัญหา rollback = ชี้ connection กลับ QA
- [ ] เมื่อนิ่งแล้ว: **un-freeze QA** (คืน iLearnDB_New ให้ QA ใช้เทสต์ได้ตามปกติ)
- [ ] เก็บ `.bak` archive + อัปเดต connection string documentation

## ⚠️ ความเสี่ยง / ข้อควรระวัง
- **Learner progress ระหว่าง backup→switch หาย** ถ้ามีคนเรียนอยู่ → ทำใน window/freeze write (step 2). ถ้าเลี่ยงไม่ได้ ทำช่วงคนน้อย + ทำเร็ว
- **ห้ามแตะ old `iLearn`** (10.10.154.119) — เป็น DB เก่าคนละตัว (prod ใหม่ = `iLearnDB_New`)
- **guid content ต้องตรง** — restore = copy DB เดิม → guid ตรง prod storage อยู่แล้ว (ไม่ต้อง copy ไฟล์); ถ้าเลือก re-ETL แทน restore จะ guid ไม่ตรง → **ต้องใช้ restore เท่านั้น** (อย่า re-run ETL)
- secret sa: อย่า commit ลง repo ถ้าเลี่ยงได้ (ใช้ env var)

## Constraints
- ❌ ห้าม re-run ETL/cleanup (จะเสีย guid mapping + fix ที่ทำไว้) — ใช้ backup/restore เท่านั้น
- ❌ ห้ามลบ QA `iLearnDB_New` / old `iLearn` จนกว่า prod DB นิ่ง
- ✅ งานหลัก = backup/restore + connection string + restart + verify

## Decision points (ให้ผู้ใช้)
1. ชื่อ prod DB = `iLearnDB_New` ใช่ไหม
2. Secret connection: env var (แนะนำ) หรือใส่ในไฟล์ config
3. Maintenance window (เวลา) สำหรับ backup→switch
4. ล้าง QA test data ~3 record ไหม (หรือปล่อย)

## Implementer Notes

- 2026-07-02: ผู้ใช้ทำ backup/restore ไปยัง production database ด้วยตนเองแล้ว และมีการตรวจยืนยันโดย Copilot ว่า QA ชี้ `AP-NTC2138-QADB` และ production ชี้ `AP-NTC2139-COSS` โดยใช้ `iLearnDB_New` ทั้งสองฝั่ง
- Verification หลัง cutover: production API, learner, admin และ SCORM content smoke tests ผ่าน; catalog และ published content counts ตรงกับข้อมูลที่ restore
- 2026-07-06: มีการยืนยันซ้ำว่า QA และ production แยก database กันแล้วหลังแก้ configuration contamination ใน PLAN-051
- ไม่มีงาน implementation หรือ cutover ค้างจากแผนนี้; checklist ด้านบนเก็บไว้เป็น historical runbook
