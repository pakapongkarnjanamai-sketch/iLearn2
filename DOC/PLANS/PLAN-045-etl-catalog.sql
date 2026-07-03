/* ============================================================================
   PLAN-045 — ETL: old iLearn (catalog only) -> new iLearn2 schema
   ----------------------------------------------------------------------------
   Topology (per user, 2026-07-01):
     - restore backup ของ prod เก่า (AP-NTC2139-COSS/iLearn) มาไว้ที่
       AP-NTC2138-QADB ชื่อ [iLearn]
     - รันสคริปต์นี้บน AP-NTC2138-QADB  (source [iLearn] + target [iLearnDB_New]
       อยู่ instance เดียวกัน -> ใช้ 3-part name ตรง ๆ, ไม่ต้อง linked server)
     - จากนั้นเปิดแอป QA (ที่ชี้ iLearnDB_New) ทดสอบ catalog
   ----------------------------------------------------------------------------
   ⚠️ ต้องยืนยันก่อน: iLearnDB_New ต้องอยู่ที่ HEAD (มีตาราง ContentItems/
      CourseContentItems, คอลัมน์ Status/CachedFileLength/LaunchHref).
      เช็ค:  SELECT name FROM sys.tables WHERE name IN ('ContentItems','Resources');
      ถ้าเจอ 'Resources' (ชื่อเก่า) = ยังไม่ถึง HEAD -> migrate ก่อน (สคริปต์นี้ใช้ชื่อ HEAD)
      *** สคริปต์ Gemini (Downloads) ใช้ชื่อเก่า Resources/CourseResources -> ห้ามรันกับ HEAD ***
   ----------------------------------------------------------------------------
   Mode: 1:1 LOSSLESS + Strategy B (Re-publish) — ContentItem เข้ามาเป็น IsActive=0
         หลังรัน ETL ต้อง **bulk publish** ให้แอป re-extract SCORM จาก byte[] -> share:
           POST {api}/ContentItems/Admin/BulkSetPublic         (publish ทั้งหมด, streaming)
           หรือ POST {api}/ContentItems/Admin/BatchPublishStream  (ตาม ids)
         (Script2 merge เป็น optimization ทีหลัง หลังดู profiling)
   Decisions: D0 division=identity(copy ตรง) · D1-rev CourseTypeId จาก suffix "(No Common)"
              ในชื่อ category (No Common->2, else->1); ตัด suffix ออกจากชื่อ category; Uncategorized ถ้า null
              D2 Status=Active&ไม่หมดอายุ->Open(1) else Closed(2) · D3 drop ExpiredDate
              D4 CourseVersion v1 ทุกคอร์ส · D5 ย้ายทั้งคลัง · D6 @IncludeTest
   ============================================================================ */

USE [iLearnDB_New];
GO
SET NOCOUNT ON;

DECLARE @IncludeTest bit = 0;   -- D6: 0 = ข้าม division Test(6)

-- D1-rev: CourseType จาก suffix ชื่อ category (ระบบเก่าไม่มี type -> ฝังใน Category.Name)
--   Category.Name ลงท้าย "(No Common)"  -> course = No-Common
--   นอกนั้น                             -> course = Common
--   ⚠️ ยืนยัน Id ด้วย: SELECT Id,Name FROM CourseTypes;
DECLARE @TypeCommon   int = 1;   -- CourseType "Common"
DECLARE @TypeNoCommon int = 2;   -- CourseType "No-Common"
DECLARE @StripCategorySuffix bit = 1;   -- 1 = ตัด "(No Common)" ออกจากชื่อ category ที่ย้าย (สะอาด, type ไปอยู่ CourseType แล้ว)

/* ============================ [0] CLEANUP (optional) ========================
   ล้าง content + downstream ให้เป็น slate สะอาดก่อน map (กัน IDENTITY_INSERT ชน).
   คงไว้: Divisions, CourseTypes, Users, Roles, UserRoles (master ที่มีอยู่แล้ว).
   คอมเมนต์ทั้งบล็อกออกได้ถ้าไม่อยากล้าง.
   ========================================================================== */
DELETE FROM dbo.AdminActivities;
DELETE FROM dbo.ScormRuntimeStates;
DELETE FROM dbo.LearningLogs;
DELETE FROM dbo.EnrollmentAssignments;
DELETE FROM dbo.AssignmentCourses;
DELETE FROM dbo.Assignments;
DELETE FROM dbo.Enrollments;
DELETE FROM dbo.CourseContentItems;
DELETE FROM dbo.CourseVersions;
DELETE FROM dbo.Courses;
DELETE FROM dbo.LearnerGroupMembers;
DELETE FROM dbo.LearnerGroups;
DELETE FROM dbo.LearnerGroupCategories;
DELETE FROM dbo.Categories;
DELETE FROM dbo.ContentItems;
DELETE FROM dbo.FileStorages;
DBCC CHECKIDENT('dbo.Categories',   RESEED, 0);
DBCC CHECKIDENT('dbo.FileStorages', RESEED, 0);
DBCC CHECKIDENT('dbo.ContentItems', RESEED, 0);
DBCC CHECKIDENT('dbo.Courses',      RESEED, 0);
DBCC CHECKIDENT('dbo.CourseVersions', RESEED, 0);
DBCC CHECKIDENT('dbo.CourseContentItems', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name='AssignmentNoSeq')
    ALTER SEQUENCE dbo.AssignmentNoSeq RESTART WITH 1;

/* ---------- [0b] Crosswalk: No-Common category -> main Common category (D1-rev / merge) ----------
   ระบบเก่าฝัง type ใน category (suffix "(No Common)"). ต้องการ: course ใน No-Common
   -> ย้ายไป category "หลัก" (ชื่อไม่มี suffix, division เดียวกัน, เลขนำหน้าเดียวกัน) + CourseType=No-Common
   จับคู่ด้วย (DivisionId + เลขนำหน้า) เพื่อทน "Part vs Parts"/ช่องว่างต่าง; main = ชื่อไม่มีวงเล็บ */
IF OBJECT_ID('tempdb..#NoCommonMerge') IS NOT NULL DROP TABLE #NoCommonMerge;
SELECT nc.Id AS OldCatId, nc.Name AS OldName, m.Id AS MainCatId, m.Name AS MainName
INTO #NoCommonMerge
FROM [iLearn].[dbo].[Categories] nc
JOIN [iLearn].[dbo].[Categories] m
  ON m.DivisionId = nc.DivisionId
 AND m.Name NOT LIKE N'%(%'                                   -- main = ไม่มีวงเล็บ (ไม่ใช่ No Common/LAS/CAS)
 AND TRY_CAST(LEFT(m.Name,  PATINDEX('%[^0-9]%', m.Name  + N'x') - 1) AS int)
   = TRY_CAST(LEFT(nc.Name, PATINDEX('%[^0-9]%', nc.Name + N'x') - 1) AS int)
WHERE nc.Name LIKE N'%(No Common)%'
  AND (@IncludeTest = 1 OR ISNULL(nc.DivisionId, 0) <> 6);
PRINT '>>> [0b] NoCommon crosswalk (ควรได้ 9 คู่, ตรวจก่อน!):';
SELECT * FROM #NoCommonMerge ORDER BY OldCatId;

/* ---------- [1] Categories (คง Id, copy DivisionId ตรง [D0], D6 filter; ข้าม No-Common ที่ merge แล้ว) ---------- */
PRINT '>>> [1/6] Categories';
SET IDENTITY_INSERT dbo.Categories ON;
INSERT dbo.Categories (Id, Name, DivisionId, IsActive, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
SELECT c.Id,
       -- ตัด suffix "(No Common)" ออก (type ไปเก็บที่ Course.CourseTypeId แล้ว)
       CASE WHEN @StripCategorySuffix = 1
            THEN LTRIM(RTRIM(REPLACE(REPLACE(c.Name, N' (No Common)', N''), N'(No Common)', N'')))
            ELSE c.Name END,
       c.DivisionId, c.IsActive, 0,
       ISNULL(c.CreatedDate, SYSUTCDATETIME()), c.CreatedBy, c.ModifiedDate, c.ModifiedBy
FROM [iLearn].[dbo].[Categories] c
WHERE (@IncludeTest = 1 OR ISNULL(c.DivisionId, 0) <> 6)
  AND c.Id NOT IN (SELECT OldCatId FROM #NoCommonMerge);   -- ข้าม No-Common ที่ merge เข้า main แล้ว
SET IDENTITY_INSERT dbo.Categories OFF;
DBCC CHECKIDENT('dbo.Categories', RESEED);

/* Uncategorized (D1) — เฉพาะเมื่อมีคอร์ส category=null */
DECLARE @UncategorizedId int = NULL;
IF EXISTS (SELECT 1 FROM [iLearn].[dbo].[Courses] WHERE CategoryId IS NULL)
BEGIN
    INSERT dbo.Categories (Name, DivisionId, IsActive, IsDeleted, CreatedAt, CreatedBy)
    VALUES (N'Uncategorized', NULL, 1, 0, SYSUTCDATETIME(), 'etl');
    SET @UncategorizedId = SCOPE_IDENTITY();
END

/* ---------- [2] FileStorages (คง Id, เฉพาะที่ Resource ในสโคปอ้าง = ข้ามไฟล์ตาย) ---------- */
PRINT '>>> [2/6] FileStorages (byte[] SCORM)';
SET IDENTITY_INSERT dbo.FileStorages ON;
INSERT dbo.FileStorages (Id, Name, ContentType, Data, Length, IsActive, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
SELECT f.Id, f.Name, f.ContentType, f.Data, f.Length, 1, 0,
       ISNULL(f.CreatedDate, SYSUTCDATETIME()), f.CreatedBy, f.ModifiedDate, f.ModifiedBy
FROM [iLearn].[dbo].[FileStorage] f
WHERE f.Id IN (
    SELECT r.FileStorageId FROM [iLearn].[dbo].[Resources] r
    WHERE r.FileStorageId IS NOT NULL
      AND (@IncludeTest = 1 OR ISNULL(r.DivisionId, 0) <> 6)
);
SET IDENTITY_INSERT dbo.FileStorages OFF;
DBCC CHECKIDENT('dbo.FileStorages', RESEED);

/* ---------- [3] Resources -> ContentItems (คง Id, ResourceHref->LaunchHref,
               +CachedFileLength, drop DivisionId; D5 = ทั้งคลัง)
   *** Strategy B (Re-publish): IsActive=0 (unpublished) เพื่อให้ bulk publish ได้;
       Name ต้องลงท้าย .zip สำหรับ zip-backed (PublishAsync extract เฉพาะ .zip) ***
   --------------------------------------------------------------------------- */
PRINT '>>> [3/6] ContentItems (Strategy B: IsActive=0)';
SET IDENTITY_INSERT dbo.ContentItems ON;
INSERT dbo.ContentItems (Id, Name, TypeId, LaunchHref, SchemaVersion, URL, FileStorageId,
                         CachedFileLength, IsActive, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
SELECT r.Id,
       -- .zip guard: zip-backed แต่ชื่อไม่ลงท้าย .zip -> เติมให้ (ปรับ/ปิดได้ตามผล query เช็คชื่อ)
       CASE WHEN r.FileStorageId IS NOT NULL AND r.Name NOT LIKE '%.zip'
            THEN r.Name + N'.zip' ELSE r.Name END,
       r.TypeId, r.ResourceHref, r.SchemaVersion, r.URL, r.FileStorageId,
       f.Length,
       0,                       -- Strategy B: unpublished -> bulk publish ทีหลัง
       0,
       ISNULL(r.CreatedDate, SYSUTCDATETIME()), r.CreatedBy, r.ModifiedDate, r.ModifiedBy
FROM [iLearn].[dbo].[Resources] r
LEFT JOIN [iLearn].[dbo].[FileStorage] f ON f.Id = r.FileStorageId
WHERE (@IncludeTest = 1 OR ISNULL(r.DivisionId, 0) <> 6);
SET IDENTITY_INSERT dbo.ContentItems OFF;
DBCC CHECKIDENT('dbo.ContentItems', RESEED);

/* ---------- [4] Courses (คง Id; CategoryId->Uncategorized ถ้า null;
               CourseTypeId=Special(1); Status derive; drop ExpiredDate) ---------- */
PRINT '>>> [4/6] Courses';
SET IDENTITY_INSERT dbo.Courses ON;
INSERT dbo.Courses (Id, Code, Title, Description, CategoryId, CourseTypeId, [Status],
                    IsActive, IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
SELECT co.Id, co.Code, co.Title, co.Description,
       ISNULL(nm.MainCatId, ISNULL(co.CategoryId, @UncategorizedId)),   -- No-Common -> ย้ายไป main category
       -- D1-rev: type = No-Common ถ้า mapped หรือชื่อ category เก่ามี suffix; null/อื่น -> Common
       CASE WHEN nm.OldCatId IS NOT NULL OR oc.Name LIKE N'%(No Common)%'
            THEN @TypeNoCommon ELSE @TypeCommon END,
       CASE WHEN co.IsActive = 1
                 AND (co.ExpiredDate IS NULL OR co.ExpiredDate > SYSUTCDATETIME())
            THEN 1 ELSE 2 END,
       co.IsActive, 0,
       ISNULL(co.CreatedDate, SYSUTCDATETIME()), co.CreatedBy, co.ModifiedDate, co.ModifiedBy
FROM [iLearn].[dbo].[Courses] co
LEFT JOIN [iLearn].[dbo].[Categories] oc ON oc.Id = co.CategoryId   -- อ่านชื่อ category เก่าเพื่อดู type
LEFT JOIN #NoCommonMerge nm ON nm.OldCatId = co.CategoryId          -- No-Common -> main
WHERE co.CategoryId IS NULL
   OR nm.MainCatId IS NOT NULL                                       -- course ที่ remap ไป main (migrated แล้ว)
   OR EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Id = co.CategoryId);   -- คอร์สใต้ Test ตกไปเอง
SET IDENTITY_INSERT dbo.Courses OFF;
DBCC CHECKIDENT('dbo.Courses', RESEED);

/* ---------- [5] CourseVersions v1 ต่อ "ทุก" course (D4) ---------- */
PRINT '>>> [5/6] CourseVersions v1';
INSERT dbo.CourseVersions (CourseId, VersionNumber, Note, IsActive, IsDeleted, CreatedAt, CreatedBy)
SELECT c.Id, 1, N'migrated', 1, 0, SYSUTCDATETIME(), 'etl'
FROM dbo.Courses c;

/* ---------- [6] CourseResources -> CourseContentItems (ผูก v1 + Order, guard-join) ---------- */
PRINT '>>> [6/6] CourseContentItems';
INSERT dbo.CourseContentItems (CourseVersionId, ContentItemId, [Order], IsActive, IsDeleted, CreatedAt, CreatedBy)
SELECT v.Id, cr.ResourceId,
       ROW_NUMBER() OVER (PARTITION BY cr.CourseId ORDER BY cr.Id),
       1, 0, SYSUTCDATETIME(), 'etl'
FROM [iLearn].[dbo].[CourseResources] cr
JOIN dbo.CourseVersions v ON v.CourseId = cr.CourseId AND v.VersionNumber = 1
JOIN dbo.ContentItems  ci ON ci.Id = cr.ResourceId;

/* ============================ RECONCILIATION ============================ */
PRINT '>>> RECONCILE';
SELECT 'Categories' T, COUNT(*) N FROM dbo.Categories
UNION ALL SELECT 'FileStorages',       COUNT(*) FROM dbo.FileStorages
UNION ALL SELECT 'ContentItems',       COUNT(*) FROM dbo.ContentItems
UNION ALL SELECT 'Courses',            COUNT(*) FROM dbo.Courses
UNION ALL SELECT 'CourseVersions',     COUNT(*) FROM dbo.CourseVersions
UNION ALL SELECT 'CourseContentItems', COUNT(*) FROM dbo.CourseContentItems;
-- คาดหวัง: Categories ~40 (49 - 9 No-Common ที่ merge, + Uncategorized ถ้ามี);
--   ContentItems<=1406  Courses<=580  CourseContentItems<=950  CourseVersions = จำนวน Courses

-- แยกตาม CourseType (ควรเห็น Common(1) + No-Common(2); No-Common = คอร์สใต้ 9 category เดิม)
SELECT ct.Id, ct.Name, COUNT(c.Id) AS Courses
FROM dbo.CourseTypes ct LEFT JOIN dbo.Courses c ON c.CourseTypeId = ct.Id
GROUP BY ct.Id, ct.Name ORDER BY ct.Id;

SELECT 'course_bad_category' K, COUNT(*) N FROM dbo.Courses c
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories x WHERE x.Id=c.CategoryId)
UNION ALL SELECT 'contentitem_bad_filestorage', COUNT(*) FROM dbo.ContentItems ci
    WHERE ci.FileStorageId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.FileStorages f WHERE f.Id=ci.FileStorageId)
UNION ALL SELECT 'course_without_version', COUNT(*) FROM dbo.Courses c
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CourseVersions v WHERE v.CourseId=c.Id);
-- FK integrity ต้องได้ 0 ทุกแถว
GO
