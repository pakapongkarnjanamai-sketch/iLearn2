# PLAN-158: Admin standards phase 0 — warn rules + AppTable primitive action

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** 2 code files + verification + plan/log updates
- **สร้างเมื่อ:** 2026-07-30

## Problem

`PLAN-157` ระบุ Delivery order ข้อ 1 ให้เริ่มจาก guardrails แบบ `warn` และลดจุดผิดมาตรฐานใน shared surface ที่คุ้มที่สุดก่อน โดยไม่ทำให้ทีมติด fail gate ทันที. ขณะนี้ยังไม่มี lint rules สำหรับ native `<button>` ใน `src/pages/**` และ direct `fetch` ใน `src/**`, และ `AppTable` ยัง render row action ผ่าน native `<button>` โดยตรง.

## Scope (ทำแค่นี้)

1. ปรับ `iLearn.Admin.React/eslint.config.js` เพิ่มกฎตาม `PLAN-157` ที่ severity = `warn`:
   - `no-restricted-globals` สำหรับ `fetch` ใน `src/**/*.{ts,tsx}`
   - override ปิดกฎนี้สำหรับ `src/lib/apiClient.ts`, `src/lib/createDataSource.ts`, `src/lib/createRestDataSource.ts`, `src/pages/system-config/HealthCheckPage.tsx`
   - `no-restricted-syntax` สำหรับ JSX `<button>` ใน `src/pages/**/*.tsx`
2. ปรับ `iLearn.Admin.React/src/components/ui/AppTable.tsx` ให้ row action ใช้ `IconButton` แทน native `<button>` โดยคงพฤติกรรมเดิม:
   - คง `e.stopPropagation()`
   - คง accessible title
   - คง mapping โทน `primary|danger|success|neutral`
3. รัน verify เฉพาะที่เกี่ยวข้องและบันทึกผล.

## Out of scope (ห้ามแตะ)

- ไม่ migrate ปุ่มในหน้า Batch A/B/C รอบนี้
- ไม่เพิ่ม `AppLinkButton`
- ไม่เปลี่ยน helper export/download
- ไม่แตะ backend `CourseVersionService`

## Acceptance criteria

1. Lint config มี 3 blocks ตาม scope และทำงานที่ระดับ `warn`.
2. `AppTable.tsx` ไม่มี native `<button>` ใน action renderer; ใช้ `IconButton` แทน.
3. `npm run lint` ผ่าน.
4. `npm run build` ผ่าน.
5. `git diff --check` ผ่าน.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

```powershell
'export const X = () => <button type="button">x</button>' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx
'export const X = () => fetch("/x")' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx
npx eslint src/lib/apiClient.ts src/lib/createDataSource.ts src/lib/createRestDataSource.ts src/pages/system-config/HealthCheckPage.tsx
```

```powershell
git diff --check
```

## Implementer Notes

- Completed scope item 1: added three ESLint config blocks in `eslint.config.js` for fetch restriction, explicit allowlist override, and native button restriction in `src/pages/**` with `'warn'` severity.
- Completed scope item 2: refactored `AppTable` action renderer to `IconButton` and preserved click propagation stop, title, and tone mapping semantics.
- Verification results:
   - `npm run lint` ✓ (0 errors, 21 warnings; warning counts match phased rollout expectations)
   - `npm run build` ✓ (successful `tsc -b && vite build`)
   - Probe warnings for both rules via `--stdin-filename` ✓
   - Allowlist exemption lint command produced no output ✓
   - `git diff --check` ✓ (no diff errors; only CRLF normalization warnings)

## Reviewer Notes (Claude Code, 2026-07-30)

- **VERIFIED.** ESLint 3 บล็อกตรงสเปค §1 เป๊ะ (rule ต่างชื่อกันตามที่กำหนด) · `AppTable` เหลือ native button 0 จุด ใช้ `IconButton` โดยคง `stopPropagation`/`title`/tone ครบ · lint+build ✓ รันเอง
- รายละเอียดการตรวจทั้ง rollout (AC 1-8 + mutation test + ข้อสังเกต 5 ข้อ) อยู่ใน `PLAN-157` หัวข้อ "Reviewer Notes — รอบ implement"
