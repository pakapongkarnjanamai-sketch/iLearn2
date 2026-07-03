-- ============================================================================
-- PLAN-048 Step 3 (cont): Verify restored DB matches QA baseline
-- ============================================================================
-- Run on prod server (10.10.154.119) after restore completes
-- Compare outputs with expected values from QA:
--   Categories = 40, Open Courses ~582, Active Content w/ URL = 1409
-- ============================================================================

USE [iLearnDB_New];
GO

-- 1. Server identity (must show 10.10.154.119 / AP-NTC2139-COSS, not QA)
SELECT @@SERVERNAME AS [ServerName], DB_NAME() AS [DatabaseName];
GO

-- 2. Core counts
SELECT 'Categories' AS [Table], COUNT(*) AS [Count] FROM dbo.Categories;             -- expect 40
SELECT 'CourseTypes' AS [Table], COUNT(*) AS [Count] FROM dbo.CourseTypes;
SELECT 'Divisions' AS [Table], COUNT(*) AS [Count] FROM dbo.Divisions;
SELECT 'Roles' AS [Table], COUNT(*) AS [Count] FROM dbo.Roles;
GO

-- 3. Courses by status
SELECT [Status], COUNT(*) AS [Count]
FROM dbo.Courses
GROUP BY [Status]
ORDER BY [Status];
-- expect: Open (1) ~582
GO

-- 4. Active published content items with URL
SELECT COUNT(*) AS [ActiveContentWithURL]
FROM dbo.ContentItems
WHERE IsActive = 1 AND URL IS NOT NULL;
-- expect: 1409
GO

-- 5. FK integrity: courses with bad CategoryId
SELECT COUNT(*) AS [CoursesWithBadCategory]
FROM dbo.Courses c
WHERE c.CategoryId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.Categories cat WHERE cat.Id = c.CategoryId);
-- expect: 0
GO

-- 6. FK integrity: content items with bad FileStorageId
SELECT COUNT(*) AS [ContentItemsWithBadFileStorage]
FROM dbo.ContentItems ci
WHERE ci.FileStorageId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.FileStorages fs WHERE fs.Id = ci.FileStorageId);
-- expect: 0
GO

-- 7. EF Migrations history (should have all migrations)
SELECT TOP 10 MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId DESC;
GO

-- 8. Spot check: sample content GUIDs exist (these should match folders on D:\iLearnContent\Courses\)
SELECT TOP 5 Id, Title, URL
FROM dbo.ContentItems
WHERE IsActive = 1 AND URL IS NOT NULL
ORDER BY NEWID();
-- Manually verify these GUIDs exist as folders on prod storage
GO

PRINT '=== Verification complete. Compare counts with expected values above. ==='
