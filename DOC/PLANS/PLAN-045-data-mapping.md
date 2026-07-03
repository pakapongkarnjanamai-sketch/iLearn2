# PLAN-045 — Data Migration Mapping (old `iLearn` → new `iLearn2`)

เอกสารประกอบ [PLAN-045](PLAN-045-production-cutover-ilearn2.md) **Phase 3 (ETL)** — mapping ระดับตาราง/คอลัมน์

- **Source (เก่า):** `iLearnService` EF Core 8 — DB `AP-NTC2139-COSS/iLearn`
- **Target (ใหม่):** `iLearn.Domain` EF Core 9
- **ที่มา:** วิเคราะห์จาก model snapshot + entity + enum ทั้งสองฝั่ง (2026-07-01) — *ยังไม่ได้ dump DB จริง; ตัวเลข/edge case ต้องยืนยันกับข้อมูลจริงอีกที*

---

## 🎯 SCOPE (ยืนยัน 2026-07-01) — เฉพาะ "สื่อการเรียน/catalog"

ย้าย **เฉพาะข้อมูลหลักสูตร/เนื้อหา** เท่านั้น — **5 ตารางเก่า**:
`Categories`, `Courses`, `CourseResources`, `Resources`, `FileStorage`

**ไม่ย้าย** (ตามที่ผู้ใช้ระบุ): `Users`, `Roles`, `UserRoles`, `Divisions`, `Enrollments`, `LearningLogs`
→ ผลคือ **ตัดงานยากทิ้งหมด** (ไม่มี learner/enrollment/history/admin/SCORM-runtime) — ระบบใหม่เริ่มด้วย **catalog อย่างเดียว**, ผู้เรียนเริ่มเรียนใหม่ (ไม่มีประวัติเก่า)

**ผลข้างเคียงที่ยอมรับแล้ว:** ประวัติการเรียน/การมอบหมาย/บัญชี admin เก่า ไม่ถูกนำมา — ต้องสร้าง/ตั้งค่าใหม่ในระบบใหม่

---

## 0. การเปลี่ยนเชิงโครงสร้างที่เกี่ยวกับ scope นี้

1. **Content model แตกใหม่:** เก่า `Resource` + `CourseResource` → ใหม่ `ContentItem` + **`CourseVersion`** + `CourseContentItem` (เพิ่มชั้น **versioning** ที่เก่าไม่มี → สร้าง **version v1** ให้ทุกคอร์สตอนย้าย)
2. **Course เพิ่ม field บังคับ:** ใหม่ `CategoryId` (NOT NULL, เก่า nullable) + `CourseTypeId` (NOT NULL, เก่าไม่มี concept) + `Status` enum (เก่ามี `IsActive`+`ExpiredDate`)
3. **Division ไม่ย้าย แต่ต้อง map:** `Category.DivisionId` เก่า → **Division ที่มีอยู่แล้วใน DB ใหม่** ผ่าน crosswalk ตามชื่อ (ดู §3 D0)
4. **audit/soft-delete ใหม่:** ทุกตารางใหม่มี `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/DeletedAt/DeletedBy/IsActive/IsDeleted` → map `CreatedDate→CreatedAt`, `ModifiedDate→UpdatedAt`, `ModifiedBy→UpdatedBy`, ตั้ง `IsDeleted=0`, `IsActive=1` (ยกเว้นมีค่าเดิม)

---

## 1. Master mapping (5 ตารางในสโคป → 6 ตารางปลายทาง)

| Old table | New table | ระดับ | หมายเหตุ |
|---|---|---|---|
| FileStorage | **FileStorages** | 🟢 ง่าย | rename ตาราง (เอกพจน์→พหูพจน์) + audit; **ขน `Data` byte[] SCORM zip** |
| Resources | **ContentItems** | 🟡 กลาง | rename + `ResourceHref→LaunchHref`; drop `DivisionId`; เติม `CachedFileLength` จาก FileStorage.Length |
| Categories | Categories | 🟡 กลาง | **map `DivisionId` เก่า→ใหม่ (crosswalk)** + rename audit + IsDeleted |
| Courses | Courses | 🟡 กลาง | เติม `CategoryId`(บังคับ)+`CourseTypeId`(บังคับ)+`Status`; drop `ExpiredDate` |
| CourseResources | **CourseVersions** + **CourseContentItems** | 🟡 กลาง | สร้าง CourseVersion v1 ต่อ course ก่อน แล้วผูก |
| — | CourseTypes | seed | มี HasData ใน migration แล้ว (Special=1, General=2) — ไม่ต้อง ETL |

> ตารางใหม่อื่น (Enrollments, LearningLogs, ScormRuntimeStates, Assignments*, LearnerGroups*, AdminActivities, Users/Roles/UserRoles) = **ปล่อยว่าง** ในสโคปนี้

---

## 2. Column mapping (ราย field)

### 2.1 FileStorage → FileStorages 🟢
`Id→Id` (**คง Id**), `Name→Name`, `ContentType→ContentType`, `Data→Data`, `Length→Length` · `CreatedDate→CreatedAt`, `ModifiedDate→UpdatedAt`, `ModifiedBy→UpdatedBy` · ตั้ง `IsActive=1`, `IsDeleted=0`
- **หัวใจ:** `Data` (varbinary(max)) = SCORM zip → ขนทั้งก้อน, **batch ทีละน้อย** กัน timeout/log เต็ม

### 2.2 Resources → ContentItems 🟡
`Id→Id` (**คง Id** — จำเป็นเพราะ CourseResource/LearningLog อ้าง), `Name→Name`, `IsActive→IsActive`, `TypeId→TypeId` (**1=Learn / 2=Exam ตรงกันเป๊ะ ✅**), `ResourceHref→LaunchHref`, `SchemaVersion→SchemaVersion`, `URL→URL`, `FileStorageId→FileStorageId`
- **drop `DivisionId`** (ContentItem ใหม่ไม่มี column นี้)
- เติม **`CachedFileLength`** = `FileStorage.Length` (join จาก FileStorageId; null ได้ถ้าไม่มีไฟล์)
- audit rename + `IsDeleted=0`

### 2.3 Categories 🟡
`Id→Id` (**คง Id** — Course อ้าง), `Name→Name`, `IsActive→IsActive` · audit rename + `IsDeleted=0`
- **`DivisionId`:** เก่า → ใหม่ **ผ่าน crosswalk** (old Division.Name → new Division.Id) — ดู D0 · ถ้า old null → new null

### 2.4 Courses 🟡
`Id→Id` (**คง Id** — CourseResource/CourseVersion อ้าง), `Code→Code`, `Title→Title`, `Description→Description`, `IsActive→IsActive` · audit rename + `IsDeleted=0`
- **`CategoryId`** (ใหม่ NOT NULL, เก่า nullable): เก่า null → ใส่ **"Uncategorized"** category (D1)
- **`CourseTypeId`** (ใหม่ NOT NULL, เก่าไม่มี): = **`Special`(1)** — กัน auto-enroll (D1)
- **`Status`** (enum Draft0/Open1/Closed2): `IsActive=1 AND (ExpiredDate null หรือ >now)` → **Open(1)**; else **Closed(2)** (D2)
- **`ExpiredDate`**: ใช้คำนวณ Status แล้ว **drop** (D3)

### 2.5 CourseResources → CourseVersions + CourseContentItems 🟡
Transform 2 ขั้น:
1. **ต่อ Course ที่มี resource ≥1** สร้าง `CourseVersions` 1 แถว: `CourseId=old CourseId`, `VersionNumber=1`, `Note='migrated'`, audit=now
2. **ต่อแถว CourseResource** สร้าง `CourseContentItems`: `CourseVersionId=`(v1 ของ course นั้น), `ContentItemId=old ResourceId`, `Order=`ลำดับ (เรียงตาม old Id), audit=now
- *(ถ้าต้องการให้คอร์สที่ไม่มี resource ก็มี v1 ว่าง — ตัดสินใน D4)*

---

## 3. Decisions — ✅ RESOLVED (2026-07-01)

| ID | ประเด็น | ✅ ข้อสรุป |
|---|---|---|
| **D0** | Division crosswalk: `Category.DivisionId` เก่า → Division ใหม่ | จับคู่ด้วย **`Division.Name`** (trim + case-insensitive). old null → new null. **old ที่ไม่ match → หยุด+รายงานให้ตัดสินมือ** (ห้าม null เงียบ — กระทบ division isolation). *ต้อง dump รายชื่อ Divisions ทั้งสอง DB มาสร้าง crosswalk จริง* |
| **D1** | ~~Course `CourseTypeId` default~~ → **แก้เป็น D1-rev** | ดู D1-rev ด้านล่าง (แทนที่ Special(1) blanket เดิม) |
| **D1-rev** | `CourseTypeId` จากชื่อ category + **merge No-Common → main** (ยืนยัน 2026-07-01) | ระบบเก่าไม่มี CourseType → admin ฝังใน category เป็น suffix `(No Common)`. **CourseTypes จริง (iLearnDB_New): 1=Common, 2=No-Common, 3=General, 4=VDO** (แมปแค่ 1/2). **Rule:** course ใน category `(No Common)` → CourseType=**No-Common(2)** + **ย้ายไป category หลัก** (ชื่อไม่มี suffix, division เดียวกัน, เลขนำหน้าเดียวกัน — ทน "Part vs Parts"); category `(No Common)` เดิม **ไม่ migrate** (merge หายไป). course อื่น → Common(1). **ขอบเขต:** No-Common 9 อัน อยู่ใน DivisionId=2 (PD2) ทั้งหมด, PD2 มี main คู่ขนานครบ. **ยังเปิด:** DivisionId=5 (NLC) มี marker `(LAS)`/`(CAS)` — ยังไม่รู้ความหมาย ปล่อยเป็น Common ไว้ก่อน. หมายเหตุ: auto-assign เช็ค `Name=="General"` (Id 3 มีจริง แต่เราไม่แมปไป) → migrated course ไม่ auto-assign |
| **D2** | derive `Course.Status` | `IsActive=1 AND (ExpiredDate null หรือ >now)` → **Open(1)**; else **Closed(2)**. Draft(0) ไม่ใช้กับข้อมูลย้าย |
| **D3** | `Course.ExpiredDate` ที่ถูก drop | ใช้คำนวณ Status (D2) แล้ว **ทิ้ง** ไม่เก็บที่อื่น |
| **D4** | คอร์สที่ไม่มี CourseResource | สร้าง **CourseVersion v1 ให้ทุกคอร์ส** (แม้ไม่มี content) — assignment+playback อิง active version; versionless = ใช้ไม่ได้ |

*(ยกเลิกจากเวอร์ชันก่อน: D-enrollment, D-LearningLog format, D-RoleType — ไม่อยู่ในสโคปแล้ว)*

> ⚠️ D1 `CourseTypeId=Special` เป็นค่าที่แนะนำ (ปลอดภัย/reversible) — ถ้าธุรกิจอยากให้คอร์สเก่าทั้งหมด auto-assign ทุกคน ค่อยเปลี่ยนเป็น General แต่ระวัง mass-enrollment วัน go-live

---

## 3.1 Source data snapshot — DB เก่า `AP-NTC2139-COSS/iLearn` (2026-07-01)

**Row counts (ในสโคป):** Categories **49** · Courses **580** · CourseResources **950** · Resources **1406** · FileStorage **1694**

**สังเกต:**
- FileStorage (1694) > Resources (1406) → ~**288 FileStorage กำพร้า** → ย้ายเฉพาะที่ถูกอ้าง (`Id IN (SELECT FileStorageId FROM Resources WHERE FileStorageId IS NOT NULL)`), ข้ามไฟล์ตาย
- Resources (1406) > CourseResources (950) → มี resource ไม่ผูกคอร์ส (~456) → **D5**
- Courses 580 → 580 CourseVersions v1 (D4)

**Division crosswalk (D0) — ✅ ยืนยันแล้ว = IDENTITY** (Id+Name ตรงกันทั้งสอง DB → copy `DivisionId` ตรง ไม่ต้องแปลง):

| Old Id | Name | New Id |
|---|---|---|
| 1 | PD1 | 1 |
| 2 | PD2 | 2 |
| 3 | CSD | 3 |
| 4 | PD3 | 4 |
| 5 | NLC | 5 |
| 6 | Test | 6 (— D6: ข้าม) |

> 📄 สคริปต์ ETL ร่างแล้ว → [PLAN-045-etl-catalog.sql](PLAN-045-etl-catalog.sql) (6 ขั้น + reconciliation + `@IncludeTest` toggle; default dry-run ROLLBACK)

## 3.2 Decisions เพิ่มเติม (จาก row counts) — เสนอ, รอ confirm

| ID | ประเด็น | เสนอ |
|---|---|---|
| **D5** | resource ไม่ผูกคอร์ส (~456) | **ย้ายทั้งคลัง (1406)** — เป็นสื่อการเรียนตาม scope, admin ผูกคอร์สใหม่ได้ |
| **D6** | division "Test" (Id 6) | **ข้าม** categories/courses/resources ใต้ Test (ข้อมูลทดสอบ) |

## 3.5 SCORM content strategy = B (Re-publish) — ยืนยัน 2026-07-01

แอปเสิร์ฟ SCORM จากไฟล์บน share (ไม่ใช่ byte[]). เลือก **B**: ETL migrate ContentItem เป็น `IsActive=0` แล้ว **bulk publish** → `ContentPublicationService.PublishAsync` re-extract จาก byte[] ไป share เอง (folder GUID ใหม่ + เขียน URL/LaunchHref ใหม่)
- bulk publish: `POST {api}/ContentItems/Admin/BulkSetPublic` (ทั้งหมด) / `Admin/BatchPublishStream` (ตาม ids)
- **⚠️ .zip gate:** publish extract เฉพาะ `Name` ลงท้าย `.zip` → ETL เติม `.zip` ให้ zip-backed ที่ชื่อไม่มี. **เช็คก่อน:**
```sql
SELECT SUM(CASE WHEN Name LIKE '%.zip' THEN 1 ELSE 0 END) EndsZip,
       SUM(CASE WHEN Name NOT LIKE '%.zip' THEN 1 ELSE 0 END) NoZip, COUNT(*) TotalWithFile
FROM [iLearn].[dbo].[Resources] WHERE FileStorageId IS NOT NULL;
SELECT TOP 10 r.Name ResourceName, f.Name FileName
FROM [iLearn].[dbo].[Resources] r JOIN [iLearn].[dbo].[FileStorage] f ON f.Id=r.FileStorageId;
```
ถ้าชื่อลงท้าย .zip อยู่แล้ว → ปิด guard `.zip` ใน ETL ได้; ถ้าไม่มี → guard เติมให้ (ชื่อจะเป็น `X.zip`, rename ใน UI ทีหลังได้)

## 3.4 Profiling results (2026-07-01) → D7: ใช้ 1:1 lossless (ไม่ merge)

รัน profiling บน `AP-NTC2139-COSS/iLearn` + ยืนยัน schema `iLearnDB_New`:

- **`iLearnDB_New` = HEAD** ✅ (`sys.tables` เจอ `ContentItems`+`CourseContentItems` ไม่ใช่ชื่อเก่า) → ใช้ [etl-catalog.sql](PLAN-045-etl-catalog.sql) (สคริปต์ Gemini ชื่อเก่า = ใช้ไม่ได้)
- Courses **580** / distinct Code **497** → dup code **83** (~14%)
- **LearnOnly = 210 (36%)** · LearnAndExam = 370 · NoLearn = 0
- Resource TypeId: 1(Learn)=836, 2(Exam)=570 (สะอาด, ไม่มีชนิดอื่น)

**D7 — merge (Script2 idea) vs 1:1:** ✅ **ใช้ 1:1 lossless** — Script2 (`WHERE FinalLearnId & FinalExamId NOT NULL`) จะ**ทิ้งคอร์ส Learn-only 210 ตัว (36%)** แต่ merge ได้แค่ dedup 83 → ไม่คุ้ม. dedup 83 dup-code = pass เสริมทีหลังแบบระวัง (ห้ามทิ้ง Learn-only) ถ้าจำเป็น

## 3.3 QA dry-run topology (ยืนยัน 2026-07-01) + ⚠️ schema-state gate

**แผนทดสอบก่อน prod:** restore backup prod เก่า (`AP-NTC2139-COSS/iLearn`) → `AP-NTC2138-QADB` ชื่อ `[iLearn]` → รัน ETL `[iLearn]` → `[iLearnDB_New]` (instance เดียวกัน, 3-part name ไม่ต้อง linked server) → เปิดแอป QA ทดสอบ catalog

⚠️ **ต้องเช็ค schema state ของ `iLearnDB_New` ก่อนรัน ETL** — migration `RenameResourceStudentTerminology` (29 เม.ย.) rename:
`Resources→ContentItems`, `CourseResources→CourseContentItems`, `ResourceHref→LaunchHref`, `ResourceId→ContentItemId`, `StudentGroups*→LearnerGroups*` + เพิ่ม `Status`(30 เม.ย.)/`CachedFileLength`(12 มิ.ย.)
- `SELECT name FROM sys.tables WHERE name IN ('ContentItems','Resources')` → เจอ `ContentItems`=HEAD (ใช้ [etl-catalog.sql](PLAN-045-etl-catalog.sql)); เจอ `Resources`=ยังไม่ถึง HEAD ต้อง `ef database update` ก่อน
- **สคริปต์ Gemini 3 ตัว (Downloads) ใช้ชื่อเก่า → รันกับ HEAD ไม่ได้** (idea Script2 merge/version = optimization ทีหลัง, ต้องพอร์ตชื่อ + กัน data-loss คอร์ส Learn-only ก่อน)

## 4. ลำดับ ETL (FK-safe) + กลยุทธ์ ID

รันตามลำดับ (พร้อม `SET IDENTITY_INSERT ON` เพื่อ**คง Id เดิม**):

1. *(เตรียม)* dump `Divisions` เก่า+ใหม่ → สร้าง crosswalk (D0)
2. **Categories** *(คง Id)* — map DivisionId ผ่าน crosswalk; + placeholder category ถ้ามี course null (D1)
3. **FileStorages** *(คง Id)* — ขน byte[] (batch)
4. **ContentItems** *(คง Id = old Resource.Id)*
5. **Courses** *(คง Id)* — CategoryId/CourseTypeId/Status (D1,D2)
6. **CourseVersions** (v1 ต่อ course)
7. **CourseContentItems** (จาก CourseResources)

*(CourseTypes — seed จาก migration แล้ว ไม่ต้องแตะ)*

**Reconciliation:** เทียบ count ต้นทาง↔ปลายทางทุกตาราง (Categories, FileStorages, Resources↔ContentItems, Courses, CourseResources↔CourseContentItems) + ตรวจ FK ครบ + **เปิด SCORM course ตัวอย่างเล่นได้จริง** (ยืนยัน byte[]+ไฟล์ share) ก่อนถือว่าผ่าน

---

## 5. งานที่เหลือก่อนเขียน ETL จริง
- [x] ปิด D0–D4 (§3 RESOLVED)
- [ ] **dump DB จริง** (`AP-NTC2139-COSS/iLearn`) — schema + row counts ของ 5 ตารางในสโคป + `Divisions` (crosswalk) เทียบ snapshot; dump `Divisions` ของ **DB ใหม่** → สร้าง crosswalk D0 จริง (จับคู่ตามชื่อ, list ตัวไม่ match)
- [ ] เขียนสคริปต์ ETL (SQL / EF console) ตาม §4 + dry-run บน DB ทดสอบ + reconciliation
- [ ] **content ไฟล์บน share:** ย้าย folder Courses (SCORM ที่แตกไฟล์) เก่า → prod share ใหม่ ควบคู่กับ ETL (PLAN-045 Phase 4)

**พร้อมเขียนสคริปต์ ETL ได้ทันทีที่มี:** (1) รายชื่อ Divisions ทั้งสอง DB (ทำ crosswalk D0), (2) row counts (ไว้ reconcile)
