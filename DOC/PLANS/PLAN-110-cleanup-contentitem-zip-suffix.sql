-- PLAN-110 §2.3 — one-time data cleanup
-- Run ONLY after §2.1 (decouple bulk-process extension check off ContentItem.Name)
-- has been deployed to PROD. Run on QA first, verify, then PROD.
-- Backup affected rows before running the UPDATE (uncomment the SELECT below to export first).

-- SELECT Id, Name FROM ContentItems WHERE Name LIKE '%.zip' AND IsDeleted = 0;

UPDATE ContentItems
SET Name = LEFT(Name, LEN(Name) - 4)
WHERE Name LIKE '%.zip' AND IsDeleted = 0;

-- Verification after run (expected 0):
-- SELECT COUNT(*) FROM ContentItems WHERE Name LIKE '%.zip' AND IsDeleted = 0;
