# PLAN-161: Admin standards Delivery #4 — response helper + report export migration

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** 3 files + verification + plan/log updates
- **สร้างเมื่อ:** 2026-07-30

## Problem

ตาม PLAN-157 Delivery order ข้อ 4 ต้องแยก helper สำหรับ response แบบ binary ออกจาก `fetchWithAccessControl<T>` ที่ parse JSON โดยตรง และย้าย export Excel ของ 2 report หน้าให้ใช้ helper ใหม่ โดยคงพฤติกรรม header merge และ filename parsing เดิม.

## Scope

1. เพิ่ม `fetchResponseWithAccessControl(path, init)` ใน `src/lib/apiClient.ts`:
   - ใช้ `buildApiUrl`, `credentials: 'include'`, และ `buildHeaders(init.headers)` เหมือนเดิม
   - โยน `ApiError` ด้วย message/body เดิมเมื่อ response ไม่สำเร็จ
   - คืน `Response` เมื่อสำเร็จ โดยไม่ consume success body
2. ปรับ `fetchWithAccessControl<T>` ให้เรียก helper กลางดังกล่าว แล้ว parse JSON ตามเดิม.
3. Migrate `handleExportExcel` ของ:
   - `src/pages/reports/AssignmentSummaryReportPage.tsx`
   - `src/pages/reports/LearnerGroupSummaryReportPage.tsx`
   ให้ใช้ `fetchResponseWithAccessControl` พร้อม `Accept: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
4. คง `downloadBlob` + `filenameFromContentDisposition` เดิม และปรับ catch ให้ใช้ `ApiError.message` เมื่อมี.

## Out of scope

- ไม่แตะ HealthCheckPage
- ไม่แตะ KpiTile dedup (Delivery #5)
- ไม่แตะ backend CourseVersionService/_dateTime.Now (Delivery #5)

## Acceptance criteria

1. มี `fetchResponseWithAccessControl` ใน `apiClient.ts` และ `fetchWithAccessControl` ยังทำงานกับ JSON เหมือนเดิม.
2. report export 2 หน้าไม่เรียก `fetch` ตรงอีกต่อไป.
3. Excel export ยังใช้ filename จาก `Content-Disposition` fallback ได้เหมือนเดิม.
4. `npm run lint` ผ่านโดยไม่มี errors.
5. `npm run build` ผ่าน.
6. `git diff --check` ผ่าน (ยอมรับได้หากมีเพียง CRLF warning).

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
rg -n "fetch\(" src/pages/reports/AssignmentSummaryReportPage.tsx src/pages/reports/LearnerGroupSummaryReportPage.tsx
```

```powershell
cd ..
git diff --check
```

## Implementer Notes

- Added `fetchResponseWithAccessControl` to `src/lib/apiClient.ts` and moved `fetchWithAccessControl<T>` to consume it for shared auth/header/error handling.
- Migrated both report Excel export handlers to `fetchResponseWithAccessControl` while preserving request `Accept` for xlsx and existing `downloadBlob` / `filenameFromContentDisposition` flow.
- Updated export error handling to show server-side `ApiError.message` when available; fallback remains localized generic message.
- Verification results:
  - `npm run lint` ✓ (0 errors, warnings reduced from 8 → 6)
  - `npm run build` ✓
  - `rg -n "fetch(" ...` for the two report files returns no matches (command exit code 1 because no match) ✓
  - `git diff --check` ✓ (only CRLF normalization warnings)

## Reviewer Notes (Claude Code, 2026-07-30)

- **VERIFIED.** `fetchResponseWithAccessControl` คืน `Response` โดยไม่ consume body ✓ · `fetchWithAccessControl` สร้างทับบนตัวใหม่ ✓ · **`Accept` ของ Excel ยังอยู่ครบ** (invariant §4.1) · `downloadBlob`/`filenameFromContentDisposition` ไม่ถูกแตะ · `HealthCheckPage` ไม่อยู่ใน diff เลย
- รายละเอียดการตรวจทั้ง rollout (AC 1-8 + mutation test + ข้อสังเกต 5 ข้อ) อยู่ใน `PLAN-157` หัวข้อ "Reviewer Notes — รอบ implement"
