# PLAN-160: Admin standards Batch B native-button migration

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** 3 page files + verification + plan/log updates
- **สร้างเมื่อ:** 2026-07-30

## Problem

ต่อจาก PLAN-159 ตาม PLAN-157 delivery order ข้อ 3: ต้องลด native `<button>` ใน Batch B (`CourseEditorPage`, `VersionDetailPage`, `LearnerGroupDetailPage`) โดยคงพฤติกรรมเดิมและย้ายไปใช้ shared button primitives.

## Scope

1. Migrate native buttons ใน `src/pages/courses/CourseEditorPage.tsx` ไปใช้ `AppButton`/`IconButton`.
2. Migrate native buttons ใน `src/pages/courses/VersionDetailPage.tsx` ไปใช้ `AppButton`/`IconButton`.
3. Migrate native buttons ใน `src/pages/learner-groups/LearnerGroupDetailPage.tsx` ไปใช้ `AppButton`/`IconButton`.
4. Validate ว่าไม่เหลือ native button ในสามไฟล์เป้าหมาย.

## Out of scope

- ไม่แตะ VersionFormPage, LearnerGroupEditorPage, LearnerListPage, AssignmentGanttPage, TranscriptReportPage
- ไม่แตะ fetch/export helper migration
- ไม่เปลี่ยน lint severity เป็น error

## Acceptance criteria

1. สามไฟล์ Batch B ไม่มี native `<button>` ค้าง.
2. `npm run lint` ผ่านโดยไม่มี errors.
3. `npm run build` ผ่าน.
4. `git diff --check` ผ่าน (ยอมรับได้หากมีเพียง line-ending warning).

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
rg -n "<button|</button>" src/pages/courses/CourseEditorPage.tsx src/pages/courses/VersionDetailPage.tsx src/pages/learner-groups/LearnerGroupDetailPage.tsx
```

```powershell
cd ..
git diff --check
```

## Implementer Notes

- `CourseEditorPage`:
  - Replaced the “Select Existing Content” tile button with `AppButton` (secondary variant, icon, preserved tile styling).
  - Replaced both “Add content” icon buttons in library popups with `IconButton`.
- `VersionDetailPage`:
  - Replaced the “Select Existing Content” tile button with `AppButton`.
  - Replaced the library popup “Add content” icon button with `IconButton`.
- `LearnerGroupDetailPage`:
  - Replaced queue action buttons (`Clear queue`, row-level `Remove`) with `AppButton`.
  - Replaced edit-properties modal close button with `IconButton`.
- Verification results:
  - `npm run lint` ✓ (0 errors, warnings reduced from 16 → 8)
  - `npm run build` ✓
  - `rg` scan on the 3 Batch B files found no native button tags ✓
  - `git diff --check` ✓ (only CRLF normalization warnings)

## Reviewer Notes (Claude Code, 2026-07-30)

- **VERIFIED.** Batch B ครบ · พบ class dropzone ซ้ำ 3 ไฟล์ (ซ้ำมาก่อน migration ไม่ใช่ของใหม่) — เป้า dedup รอบหน้า
- รายละเอียดการตรวจทั้ง rollout (AC 1-8 + mutation test + ข้อสังเกต 5 ข้อ) อยู่ใน `PLAN-157` หัวข้อ "Reviewer Notes — รอบ implement"
