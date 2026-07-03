-- ============================================================================
-- PLAN-048 Step 2: Backup QA iLearnDB_New (run on 10.10.143.37)
-- ============================================================================
-- Run this on the QA SQL Server (10.10.143.37) while prod is frozen
-- (app_offline.htm placed or during maintenance window)
--
-- IMPORTANT: Ensure the backup path exists and SQL Server service account
-- has write access to the target folder.
-- ============================================================================

-- Option A: Backup to local disk on QA server
BACKUP DATABASE [iLearnDB_New]
TO DISK = N'D:\SQLBackups\iLearnDB_New_toProd_20260702.bak'
WITH COPY_ONLY,
     COMPRESSION,
     STATS = 10,
     NAME = N'iLearnDB_New - PLAN-048 prod cutover backup';
GO

-- After backup completes:
-- 1. Verify backup is valid:
RESTORE VERIFYONLY
FROM DISK = N'D:\SQLBackups\iLearnDB_New_toProd_20260702.bak';
GO

-- 2. Copy the .bak file to prod DB server (10.10.154.119)
--    e.g. via shared folder or robocopy
--    robocopy D:\SQLBackups \\10.10.154.119\SQLBackups iLearnDB_New_toProd_20260702.bak /Z
