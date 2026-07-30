# PLAN-163: Admin standards Delivery #6 — Batch C completion + error enforcement

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** 7 files + verification + plan/log updates
- **สร้างเมื่อ:** 2026-07-30

## Problem

ตาม PLAN-157 ขั้นสุดท้ายต้องปิด debt ที่เหลือใน Batch C, flip lint guardrails จาก `warn` เป็น `error`, และบันทึก approved exceptions ใน README เพื่อให้มาตรฐานบังคับใช้จริง.

## Scope

1. Migrate native `<button>` ที่เหลือในหน้า:
   - `AssignmentGanttPage.tsx`
   - `VersionFormPage.tsx`
   - `LearnerGroupEditorPage.tsx`
   - `LearnerListPage.tsx`
   - `TranscriptReportPage.tsx`
2. เปลี่ยน lint severity ใน `eslint.config.js`:
   - `no-restricted-globals` (`fetch`) → `error`
   - `no-restricted-syntax` (`JSX <button>` in pages) → `error`
3. เพิ่มเอกสาร exceptions และ rationale ใน `iLearn.Admin.React/README.md`.

## Out of scope

- ไม่เปลี่ยน API endpoints หรือ backend contracts
- ไม่แตะ exceptions นอก allowlist ที่อนุมัติใน PLAN-157

## Acceptance criteria

1. `npm run lint` ผ่านภายใต้กฎระดับ `error`.
2. `npm run build` ผ่าน.
3. Allowlist files lint clean ภายใต้กฎใหม่.
4. Probe snippets สำหรับ native `<button>` และ `fetch` ใน `src/pages/**` fail ที่ระดับ `error`.
5. README มี section อธิบาย exceptions ที่อนุมัติ.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
npx eslint src/lib/apiClient.ts src/lib/createDataSource.ts src/lib/createRestDataSource.ts src/pages/system-config/HealthCheckPage.tsx
'export const X = () => <button type="button">x</button>' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx
'export const X = () => fetch("/x")' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx
```

```powershell
cd ..
git diff --check
```

## Implementer Notes

- Completed Batch C button migration by replacing remaining native page buttons with `AppButton`/`IconButton` primitives:
  - `AssignmentGanttPage`: status filter chips.
  - `VersionFormPage`: select-existing-content tile + content-library add action.
  - `LearnerGroupEditorPage`: selected-learner remove action.
  - `LearnerListPage`: clear filters action.
  - `TranscriptReportPage`: clear search icon action.
- Flipped both standards rules in `eslint.config.js` from `warn` to `error`.
- Added `Lint Guardrails And Exceptions` section to README with approved file-scoped fetch exceptions and rationale.
- Verification results:
  - `npm run lint` ✓ (passes with error-level enforcement)
  - `npm run build` ✓
  - allowlist file lint command produces no output ✓
  - stdin probes for native button/fetch fail with ESLint errors as expected ✓
  - `git diff --check` (run before and after batch) reports only CRLF normalization warnings ✓

## Reviewer Notes (Claude Code, 2026-07-30)

- **VERIFIED.** Batch C ครบ · native button ใน `src/pages/**` = 0 · ทั้งสอง rule เป็น `error` · **ไม่มี `eslint-disable` bypass แม้จุดเดียว** (แข็งกว่าที่ §7.4 เผื่อไว้) · ข้อเดียวที่ควรเก็บ: README ควรกำกับว่า allowlist 4 ไฟล์ปิดรายการแล้ว ของใหม่ให้ inline
- รายละเอียดการตรวจทั้ง rollout (AC 1-8 + mutation test + ข้อสังเกต 5 ข้อ) อยู่ใน `PLAN-157` หัวข้อ "Reviewer Notes — รอบ implement"
