# PLAN-186: Course detail version action placement

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

ผู้ใช้ขอปรับ `Apply Active Version` ไม่ให้เป็นปุ่มในหน้า Overview / Controls sidebar แต่ให้ action นี้อยู่กับฟังก์ชันของ version โดยตรง และขอเปลี่ยนชื่อ `Add Version Package` เป็น `Add Version`

## Changes

- Renamed Course detail sidebar action `Add Version Package` to `Add Version`
- Removed standalone `Apply Active Version` from Course detail Controls sidebar
- Localized the new active-version labels, modal copy, impact labels, policy choices, and fallback toast text through `COURSE_LABELS`
- Moved learner version policy modal into version table actions:
  - inactive version check action opens `Set Active Version` modal and calls `set-active`
  - active version refresh action opens `Apply Active Version` modal and calls `apply-learner-policy`
- Kept the same learner impact and policy choices (`MoveNotStarted`, `ResetInProgress`)

## Contract Changes

No backend/API/DB changes in this plan. UI only; uses endpoints added by PLAN-183.

## Verification

- `npm run lint` ✓
- `npm run build` ✓ (existing Vite chunk-size warning)
- QA React deploy: `index-4zpVvMiX.js`, robocopy 3
- PROD React deploy: `index-4zpVvMiX.js`, robocopy 3
- PROD smoke `/iLearn/admin-react/courses/893` = 200 and serves `index-4zpVvMiX.js`
- Playwright assertions: Thai `เพิ่มเวอร์ชัน` visible, English `Add Version` visible after locale switch, old `Add Version Package` label absent

## Implementer Notes

- Existing active-version endpoint smoke from PLAN-183 remains valid; this plan only changes action placement and wording