-- ============================================================================
-- PLAN-048 Step 5: Post-cutover verification (run on prod DB via app or SSMS)
-- ============================================================================
-- After switching the connection string and restarting the app pool,
-- run these queries to confirm the prod app is talking to the RIGHT database.
-- ============================================================================

-- 1. Confirm we're on the prod server (NOT QA 10.10.143.37)
SELECT
    @@SERVERNAME  AS [SQLServerName],
    DB_NAME()     AS [DatabaseName],
    GETDATE()     AS [CurrentTime];
-- Expected: AP-NTC2139-COSS (or the hostname for 10.10.154.119)
-- NOT: anything with 143.37 or QA hostname
GO

-- 2. Quick data sanity
SELECT 'Courses'      AS [Entity], COUNT(*) AS [Total] FROM dbo.Courses
UNION ALL
SELECT 'Open Courses', COUNT(*) FROM dbo.Courses WHERE [Status] = 1
UNION ALL
SELECT 'ContentItems (active+URL)', COUNT(*) FROM dbo.ContentItems WHERE IsActive = 1 AND URL IS NOT NULL
UNION ALL
SELECT 'Categories', COUNT(*) FROM dbo.Categories
UNION ALL
SELECT 'Enrollments', COUNT(*) FROM dbo.Enrollments
UNION ALL
SELECT 'AdminUsers', COUNT(*) FROM dbo.AdminUsers;
GO

-- 3. Check for any recent writes (learner progress after cutover)
--    If rows appear here with timestamps AFTER cutover, the app is writing to prod DB correctly.
SELECT TOP 10
    Id,
    CreatedAt,
    'LearningLog' AS [Source]
FROM dbo.LearningLogs
ORDER BY CreatedAt DESC;
GO

-- 4. Verify no orphaned content references
SELECT COUNT(*) AS [ContentMissingFileStorage]
FROM dbo.ContentItems ci
WHERE ci.FileStorageId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM dbo.FileStorages fs WHERE fs.Id = ci.FileStorageId);
-- expect: 0
GO

PRINT '=== Post-cutover verification complete ==='
PRINT '  If @@SERVERNAME shows the prod server and counts match, cutover is successful.'
PRINT '  Monitor for 24h, then unfreeze QA DB for QA testing use.'
