-- ============================================================================
-- PLAN-048 Step 3: Restore iLearnDB_New on prod server (10.10.154.119)
-- ============================================================================
-- Run this on the PROD SQL Server (10.10.154.119 / AP-NTC2139-COSS)
--
-- IMPORTANT:
--   - Old DB "iLearn" on this server = legacy backup — DO NOT TOUCH
--   - New DB name = "iLearnDB_New" (same name as QA for config simplicity)
--   - Adjust file paths in WITH MOVE to match prod server's data directories
-- ============================================================================

-- Step 1: Check the logical file names inside the backup
RESTORE FILELISTONLY
FROM DISK = N'D:\SQLBackups\iLearnDB_New_toProd_20260702.bak';
GO
-- Note the LogicalName values (typically "iLearnDB_New" and "iLearnDB_New_log")
-- Adjust the MOVE clauses below to match.

-- Step 2: Restore (adjust paths to prod server's data/log directories)
RESTORE DATABASE [iLearnDB_New]
FROM DISK = N'D:\SQLBackups\iLearnDB_New_toProd_20260702.bak'
WITH
    MOVE N'iLearnDB_New'     TO N'D:\SQLData\iLearnDB_New.mdf',
    MOVE N'iLearnDB_New_log' TO N'D:\SQLData\iLearnDB_New_log.ldf',
    RECOVERY,
    REPLACE,       -- Use REPLACE only if DB doesn't exist yet or you want to overwrite
    STATS = 10;
GO

-- Step 3: Set recovery model (SIMPLE for LMS workload, or FULL if you want log backups)
ALTER DATABASE [iLearnDB_New] SET RECOVERY SIMPLE;
GO

-- Step 4: Verify the restore
USE [iLearnDB_New];
GO

SELECT @@SERVERNAME AS [ServerName], DB_NAME() AS [DatabaseName];
GO
