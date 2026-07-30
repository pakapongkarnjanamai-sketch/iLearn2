# PLAN-159: Admin standards Batch A native-button migration

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** 2 page files + verification + plan/log updates
- **สร้างเมื่อ:** 2026-07-30

## Problem

`PLAN-157` Delivery order ข้อ 2 กำหนดให้ migrate Batch A (`AssignmentDetailPage`, `BulkAssignPage`) ออกจาก native `<button>` ใน `src/pages/**` ไปใช้ shared primitives เพื่อค่อย ๆ ลด lint debt ภายใต้ guardrails ระดับ `warn`.

## Scope

1. แปลง native button ใน `src/pages/assignments/BulkAssignPage.tsx` ไปใช้ `AppButton` โดยคง action และ UX เดิม.
2. แปลง native button ใน `src/pages/assignments/AssignmentDetailPage.tsx` ไปใช้ `AppButton` โดยคง action/flow เดิม:
   - clear selected learners
   - remove not found
   - clear queue
   - remove queued learner row
3. ห้ามแตะ logic ธุรกิจ/API payload/lifecycle.

## Out of scope

- ไม่แตะ Batch B/C
- ไม่แตะ fetch/export helper migration
- ไม่เปลี่ยน severity จาก `warn` เป็น `error`

## Acceptance criteria

1. ไม่มี native `<button>` ค้างในสองไฟล์ Batch A.
2. `npm run lint` ผ่านโดยไม่มี errors.
3. `npm run build` ผ่าน.
4. `git diff --check` ผ่าน (ยอมรับได้หากมีเพียง line-ending warning).

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
rg -n "<button|</button>" src/pages/assignments/AssignmentDetailPage.tsx src/pages/assignments/BulkAssignPage.tsx
```

```powershell
cd ..
git diff --check
```

## Implementer Notes

- Replaced Batch A native buttons with `AppButton` primitives:
  - `BulkAssignPage`: selected-courses `Clear` action.
  - `AssignmentDetailPage`: `Clear` selected learners, `Remove not found`, `Clear queue`, and per-row `Remove` in queued learners.
- Preserved existing handlers and command semantics (no API/business logic changes).
- Verification results:
  - `npm run lint` ✓ (0 errors, warning count reduced from 21 to 16)
  - `npm run build` ✓
  - `rg` scan for native buttons in both Batch A files returns no matches ✓
  - `git diff --check` ✓ (only CRLF normalization warnings)

## Reviewer Notes (Claude Code, 2026-07-30)

- **VERIFIED.** Batch A ครบ · ไม่มี `type="submit"`/`disabled`/`loading` ถูกแตะในทั้ง diff ⇒ ไม่มีความเสี่ยง behavior หลุดตาม §3
- รายละเอียดการตรวจทั้ง rollout (AC 1-8 + mutation test + ข้อสังเกต 5 ข้อ) อยู่ใน `PLAN-157` หัวข้อ "Reviewer Notes — รอบ implement"
