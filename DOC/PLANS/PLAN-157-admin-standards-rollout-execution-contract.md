# PLAN-157: Admin standards rollout execution contract

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** React Admin guardrails, primitives, targeted refactors, and one backend time-consistency fix
- **สร้างเมื่อ:** 2026-07-30
- **Supersedes:** Execution details in PLAN-156 (now `SUPERSEDED`); PLAN-156 remains the strategy/history document.

## Why this revision exists

PLAN-156 has the correct goal, but review found implementation gaps that would make the first phase either fail immediately or create unsafe exceptions:

1. A blanket lint ban on native `<button>` would fail on existing pages and on legitimate shared controls before their replacements exist.
2. `fetchWithAccessControl()` always consumes and JSON-parses the response, so it cannot replace binary Excel downloads. Health probing deliberately accepts JSON bodies from HTTP 503 and is another distinct case.
3. `AppButton` cannot render a router link; forcing all existing styled links into it would lose correct navigation semantics or reintroduce duplicated classes.
4. The proposed “60% reduction” has no computed baseline, exclusion list, or reliable automated measurement.
5. `CourseVersionService` already injects `IDateTime`, but a time-source change needs a regression assertion using that injected clock.

This plan converts the strategy into an ordered, verifiable implementation contract.

## Verified baseline (Claude Code, 2026-07-30 — re-measured against working tree)

Fixes gap 4 above: this is the computed baseline the “60% reduction” target lacked. Re-run the commands in Verification to refresh.

**Native `<button>` in `src/pages/**` — 19 occurrences across 10 files:**

| File | Count | Batch |
|---|---|---|
| `assignments/AssignmentDetailPage.tsx` | 4 | A |
| `learner-groups/LearnerGroupDetailPage.tsx` | 3 | B |
| `courses/CourseEditorPage.tsx` | 3 | B |
| `courses/VersionFormPage.tsx` | 2 | C |
| `courses/VersionDetailPage.tsx` | 2 | B |
| `assignments/BulkAssignPage.tsx` | 1 | A |
| `assignments/AssignmentGanttPage.tsx` | 1 | C |
| `learner-groups/LearnerGroupEditorPage.tsx` | 1 | C |
| `learners/LearnerListPage.tsx` | 1 | C |
| `reports/TranscriptReportPage.tsx` | 1 | C |

Batch A+B = 13/19 (68%). Batch C = 6/19 across 5 files. Note the batch file lists in §3 are stated as bare filenames; the real paths are nested under `src/pages/<area>/`, not `src/pages/` directly.

**Direct `fetch` outside `src/lib/` — 3 occurrences, all covered by §1/§4:** `reports/AssignmentSummaryReportPage.tsx:234`, `reports/LearnerGroupSummaryReportPage.tsx:214`, `system-config/HealthCheckPage.tsx:45`. No `window.fetch`/`globalThis.fetch` anywhere.

**Raw `DateTime.UtcNow`/`DateTime.Now` in `iLearn.Application/` — 2 occurrences, both in scope for §6:** `Services/CourseVersionService.cs:200` (`CourseVersion.CreatedAt`) and `:581` (`CourseContentItem.CreatedAt`). Fixing these two leaves the entire Application layer free of raw clock reads — a stronger outcome than §6 claims. `IDateTime _dateTime` is already injected (field line 32, ctor line 48).

**§6 test harness already exists:** `iLearn.Tests/CourseVersionLearnerPolicyTests.cs` has `FakeDateTime : IDateTime` (lines 375-384) with a fixed `Now = 2026-04-28 10:30:00` and already constructs `CourseVersionService` with it (lines 264-277). Adding the two assertions needs no new harness.

## Scope (ทำแค่นี้)

### 1. Establish enforcement in the existing ESLint config

Enforce both standards through `iLearn.Admin.React/eslint.config.js` (flat config, already using `typescript-eslint`). **Do not write a separate checker script** — no new tooling is needed, violations surface in the existing `npm run lint`, and per-site exceptions get the standard `eslint-disable` mechanism for free.

**Enforced scope is `src/pages/**` only.** That matches the migration batches in §3 and acceptance criterion 2. `src/components/**` is deliberately outside this contract's enforced scope — see "Known remaining debt" below.

Three config blocks, in this order (later blocks override earlier ones):

1. `files: ['src/**/*.{ts,tsx}']` → `'no-restricted-globals': ['error', { name: 'fetch', message: 'Use fetchWithAccessControl / fetchResponseWithAccessControl from src/lib/apiClient.ts' }]`
2. `files: ['src/lib/apiClient.ts', 'src/lib/createDataSource.ts', 'src/lib/createRestDataSource.ts', 'src/pages/system-config/HealthCheckPage.tsx']` → `'no-restricted-globals': 'off'`
3. `files: ['src/pages/**/*.tsx']` → `'no-restricted-syntax': ['error', { selector: "JSXOpeningElement[name.name='button']", message: 'Use AppButton / IconButton / SegmentedToggle from src/components/ui' }]`

**This exact config was executed against the working tree during review (2026-07-30) — it is proven, not proposed.** Results, via a throwaway `eslint.probe.config.js` that was deleted afterwards (the real `eslint.config.js` was never modified):

- Both rules fire on a violating snippet through `--stdin --stdin-filename src/pages/__probe__.tsx` (exit 1, correct message and rule id each time).
- Block 2's override silences `no-restricted-globals` in all four allowlisted files.
- The button rule does **not** apply to `src/components/ui/**` (`AppButton.tsx`, `AppTable.tsx` both clean), confirming the `files:` scoping.
- Run across `src/pages/**`, the rules report **exactly 19 `no-restricted-syntax` + 2 `no-restricted-globals`**, with per-file counts identical to the Verified baseline table below. The measurement mechanism and the baseline agree.
- Caveat for the implementer: a minimal probe config errors with "Definition for rule 'react-hooks/exhaustive-deps' was not found" on files carrying an existing `eslint-disable` for that rule. That is an artifact of a stripped-down config only — it will not occur when these blocks are added to the real `eslint.config.js`, which loads `eslint-plugin-react-hooks`.

Implementation constraints:

- Use **two different rule names** (`no-restricted-globals` for fetch, `no-restricted-syntax` for the JSX element) as specified. Flat config *replaces* a rule's option array rather than merging it, so putting both concerns on `no-restricted-syntax` would force every later block to restate the earlier selectors. Do not consolidate them.
- `no-restricted-globals` matches the bare `fetch` global only, not `window.fetch`. That is sufficient today (verified: no `window.fetch`/`globalThis.fetch` anywhere in `src/`). If a `window.fetch` call ever appears, add a `no-restricted-syntax` selector for it rather than reworking the rule.
- Per-site exceptions use `// eslint-disable-next-line <rule> -- <reason>` in source. **Do not introduce a `data-standard-exception` DOM attribute** — React forwards `data-*` to the rendered HTML, so a lint marker would ship to production for every exception. A reason after `--` is mandatory.

### 1b. Known remaining debt (explicitly not in this contract)

Recorded so it is not mistaken for an oversight, and so the allowlist above stays honest:

- `src/components/shared/LearnerDirectorySelector.tsx` — 11 native buttons (clear-filters, clear-search, filter-chip removals, select-all-matching, review/clear commands, chip removal). These are real controls, **not** primitive-implementation boundaries, so they are genuine debt. Too large to fold into a page batch; open a dedicated follow-up plan.
- `src/components/shared/NotificationRow.tsx`, `src/components/layout/Header.tsx`, `src/components/layout/Sidebar.tsx` — assess in that same follow-up.
- `src/components/ui/**` native buttons are the primitive implementations themselves and are correct as-is. Note that after §2.3, `AppTable.tsx` contains **zero** native buttons (it has exactly one today, the action renderer at line 365), so no allowlist entry is needed for it.

### 2. Fill missing shared primitives before enforcing usage

1. Refactor the action renderer in `src/components/ui/AppTable.tsx` (the single native `<button>` at line 365) to use `IconButton`. It must preserve row click isolation (`e.stopPropagation()`), `title`/accessible name, and the existing primary/danger/success/neutral tone mapping — `IconButton` already exposes exactly those four tones, so no new tone vocabulary is needed.
2. Retain plain text links for inline navigation in prose and table cells. **No new link primitive in this contract** (see below).

**`AppLinkButton` is deliberately deferred.** It was specified in an earlier revision, but the codebase has no consumer for it: the only `<Link>` in Batch A+B is `AssignmentDetailPage.tsx:997`, a learner name inside a table cell — inline navigation that rule 2 above says must stay a text link. A repo-wide scan found exactly one genuinely button-styled link, `CourseDetailPage.tsx:655` (an `<Eye>` icon `<Link>` carrying `p-1 … rounded-md`, sitting between two real `IconButton`s), which is outside every batch here. Building the primitive now would ship it with zero call sites and would drag in a shared `AppButton`/`AppLinkButton` style-map refactor that nothing yet needs.

When a batch actually contains command-links, open a follow-up plan that (a) names the call sites, (b) adds `AppLinkButton` rendering `react-router-dom` `Link` with `AppButton`'s `primary | secondary | danger | ghost` × `sm | md` vocabulary, and (c) extracts one shared variant/size style source at that point rather than pre-emptively. `CourseDetailPage.tsx:655` is the first known candidate.

### 3. Migrate native page actions in small verified batches

Migrate no more than five principal files per implementation plan/PR. Start with the known hotspot pages:

- Batch A: `AssignmentDetailPage.tsx`, `BulkAssignPage.tsx`.
- Batch B: `LearnerGroupDetailPage.tsx`, `CourseEditorPage.tsx`, `VersionDetailPage.tsx`.
- Batch C: remaining `src/pages/**` native action buttons reported by `standards:check`.

Rules for the migration:

- Text commands use `AppButton`.
- Icon-only actions use `IconButton` with mandatory `title`.
- Two-option mode/filter choices use `SegmentedToggle`.
- Clickable `div`/`li` selection rows become semantic buttons only when they are commands; selectable listbox/table patterns may remain native structure with keyboard behavior explicitly implemented and documented.
- Never replace a submit button with a navigation link, and preserve `type="submit"`, disabled, loading, focus, and confirmation behavior.

### 4. Centralize response download behavior without breaking binary/health contracts

1. Add `fetchResponseWithAccessControl(path, init)` to `src/lib/apiClient.ts`. It must build the configured API URL, merge headers, include Windows credentials, and throw the existing `ApiError` on non-success without consuming successful response bodies.
   - **Header merge is a hard invariant, not a detail.** Today's exports send `Accept: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` and survive *only* because `buildHeaders` sets `Accept: application/json` when the caller has not already set it. A helper that force-sets `Accept` would break both Excel downloads silently — server returns JSON, `blob()` succeeds, user gets a corrupt `.xlsx`. Reuse `buildHeaders` as-is and cover this with a caller-supplied-`Accept` assertion.
2. Keep `fetchWithAccessControl<T>()` for JSON APIs; implement it on top of the shared response path where practical.
3. Migrate these binary export handlers to the new response helper:
   - `src/pages/reports/AssignmentSummaryReportPage.tsx` (handler at ~line 234)
   - `src/pages/reports/LearnerGroupSummaryReportPage.tsx` (handler at ~line 214)
   - **Change the `fetch` call only.** Both pages already import `downloadBlob` and `filenameFromContentDisposition` from `src/lib/downloadBlob.ts`, which already handles the `filename*=UTF-8''` and quoted-`filename` forms plus a fallback. Do not write new filename-parsing or download code. The one behavioural change is error typing: the current `throw new Error(response.statusText)` becomes `ApiError`, so the server's error message reaches the toast instead of a bare status text.
4. Leave `HealthCheckPage` alone apart from the fetch exemption in §1. Its `probeHealth` is a local function in the page (not a shared helper), and the explanatory comment this plan originally called for **already exists** at `HealthCheckPage.tsx:39-42`. No edit required — it is listed in the §1 allowlist and that is the whole change.

### 5. Deduplicate only identical visual rules

1. Extract the identical report `KpiTile` implementation and semantic tone map from:
   - `AssignmentSummaryReportPage.tsx`
   - `LearnerGroupSummaryReportPage.tsx`
2. Put it in a shared UI component with semantic tones `neutral | info | success | danger`; do not globalize unrelated report-specific color maps.
3. Preserve the existing rendered labels, values, table layout, and report data contracts.

### 6. Complete the narrow backend time-source consistency fix

1. In `iLearn.Application/Services/CourseVersionService.cs`, replace the two direct `DateTime.UtcNow` assignments to `CreatedAt` with `_dateTime.Now`.
2. Add or update a focused regression test in `iLearn.Tests/CourseVersionLearnerPolicyTests.cs` proving both a created `CourseVersion` and its created `CourseContentItem` use the deterministic injected clock.
3. Do not change fields whose names/contracts explicitly require UTC and do not broaden this PR into an all-project timestamp sweep.

### 7. Enable enforcement only after migration coverage is complete

The severity value is the phase switch — no separate reporting mode is needed:

1. Land both rules at severity `'warn'` in the first plan. `npm run lint` stays green (it has no `--max-warnings` cap today, verify this before landing), and every migration PR shows its remaining count in the lint output.
2. Once Batch C is complete and the count reaches zero, flip both rules to `'error'` in the same PR that removes the last violation.
3. Enforcement runs through `npm run lint` only. **There is no CI in this repo** — `.github/workflows/` is empty and there is no husky/lint-staged/pipeline config — so do not write acceptance criteria that assume a CI gate. Adding a workflow is out of scope here; if the team wants one, open a separate plan.
4. Add the approved exceptions and their rationale to `iLearn.Admin.React/README.md`. Every exception is an `eslint-disable-next-line … -- <reason>` at its call site; no file-level or config-level bypasses.

## Out of scope (ห้ามแตะ)

- Do not alter API response DTOs/routes, authorization, database schema, or migrations.
- Do not replace all native controls inside shared primitives; their own native elements are the implementation boundary.
- Do not force binary downloads through JSON parsing.
- Do not migrate Legacy MVC `alert(...)` in this initiative; create a dedicated legacy plan if required.
- Do not turn every colored KPI/status into one generic component when its domain meaning differs.
- Do not change SCORM/course-version lifecycle behavior beyond the injected-clock source for audit timestamps.

## Acceptance criteria

1. Both ESLint rules report file/line/message, and each is proven to fire on a violating snippet fed through `--stdin-filename` (see Verification) — no fixture files committed.
2. After rollout, no unannotated native `<button>` exists in `iLearn.Admin.React/src/pages/**`, and both rules sit at severity `'error'`.
3. `AppTable` row actions render through `IconButton`, preserving row-click isolation, `title`, and the four existing tones; `AppTable.tsx` contains no native `<button>`.
4. The two report Excel downloads use `fetchResponseWithAccessControl` and still honor the server `Content-Disposition` filename fallback.
5. `HealthCheckPage` remains able to display structured health JSON returned with HTTP 503.
6. The two report pages share one KPI tile/tone implementation without changing visible output.
7. Course-version creation paths set `CreatedAt` from injected `IDateTime.Now`, covered by a deterministic focused test.
8. React lint/build, focused backend tests, and `git diff --check` pass.

## Verification

React Admin:

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Rule negative checks — `--stdin-filename` makes flat config resolve as if the snippet lived at that path, so neither check commits a fixture file. Run from `iLearn.Admin.React`:

```powershell
'export const X = () => <button type="button">x</button>' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx
if ($LASTEXITCODE -eq 0) { throw 'Expected no-restricted-syntax to reject a native button in src/pages.' }
```

```powershell
'export const X = () => fetch("/x")' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx
if ($LASTEXITCODE -eq 0) { throw 'Expected no-restricted-globals to reject direct fetch in src/pages.' }
```

Both probes only return non-zero once §7 step 2 has flipped severity to `'error'`. While the rules are at `'warn'`, assert on the message text instead:

```powershell
'export const X = () => <button type="button">x</button>' | npx eslint --stdin --stdin-filename src/pages/__probe__.tsx --format compact
```

Also confirm the allowlist still exempts the intended files (expect no output):

```powershell
npx eslint src/lib/apiClient.ts src/lib/createDataSource.ts src/lib/createRestDataSource.ts src/pages/system-config/HealthCheckPage.tsx
```

Backend:

```powershell
dotnet build iLearn.Tests -o artifacts\verify-plan157
dotnet test artifacts\verify-plan157\iLearn.Tests.dll --filter FullyQualifiedName~CourseVersionLearnerPolicyTests
Remove-Item -Recurse -Force artifacts\verify-plan157
```

Repository:

```powershell
git diff --check
```

## Delivery order

Work order, **not reserved plan numbers.** Claim the number with `pwsh tools/plan-status.ps1 -Next` at the moment each plan file is created — with 40 plans in review debt and other agents creating files in parallel, pre-allocating a block here would collide.

1. ESLint rules at `'warn'` (§1) + `AppTable` action → `IconButton` (§2).
2. Batch A native-action migration.
3. Batch B native-action migration.
4. `fetchResponseWithAccessControl` + two report export migrations (§4).
5. Shared KPI tile (§5) + `CourseVersionService` injected-clock fix and test (§6).
6. Batch C completion, flip both rules to `'error'` (§7), README exceptions.

## Implementer Notes

- Completed through child implementation plans:
   - `PLAN-158`: guardrails at `warn` + `AppTable` action → `IconButton`.
   - `PLAN-159`: Batch A native button migration.
   - `PLAN-160`: Batch B native button migration.
   - `PLAN-161`: `fetchResponseWithAccessControl` + report Excel export migration.
   - `PLAN-162`: shared report KPI tile + `_dateTime.Now` timestamp fix + deterministic test.
   - `PLAN-163`: Batch C migration complete, guardrails flipped to `error`, README exceptions documented.
- Final verification snapshots were captured in each child plan (lint/build/focused tests/probes).

## Reviewer Notes (Claude Code, 2026-07-30)

ตรวจทุก claim ทางเทคนิคกับโค้ดจริงแบบ independent แล้ว — **ถูกหมด**: `fetchWithAccessControl` ทำ `response.text()` + `JSON.parse` จริง (`apiClient.ts:80-85`) ⇒ ใช้กับ binary ไม่ได้ · direct `fetch` นอก `lib/` มี 3 จุดเป๊ะ ๆ · whitelist 3 ไฟล์ตรงกับ fetch จริงใน `lib/` ครบ · `AppButton` = `primary|secondary|danger|ghost` × `sm|md` ตรง · `IconButton` tone = `neutral|primary|danger|success` ตรง · `AppTable.tsx:365-373` มี `stopPropagation`/`title`/`colorClass` ตามที่บรรยาย · `CourseVersionService` UtcNow 2 จุดบรรทัด 200/581 บน `CourseVersion`+`CourseContentItem` และ inject `IDateTime` อยู่แล้ว · KpiTile สองตัว byte-identical · TypeScript 6.0.2 ติดตั้งแล้ว. ตัวเลข baseline ที่วัดเองตรงกับที่ GPT log ไว้เป๊ะ (19/10, 3, 2) — ย้ายลงหัวข้อ Verified baseline แล้ว.

**Finding 1-4 ผู้ใช้สั่งให้ reviewer แก้เอง — แก้แล้วในไฟล์นี้ (2026-07-30):**

1. **Scope ของกฎ `<button>` ขัดกันเอง + allowlist ไม่ครบ** → เขียน §1 ใหม่: enforce **`src/pages/**` เท่านั้น** (ตรงกับ AC#2 และ batch ใน §3) แล้ว**ลบ allowlist ของ shared components ออกทั้งก้อน** เพราะไฟล์เหล่านั้นอยู่นอก scope อยู่แล้ว = ไม่มีลิสต์ให้ผิดได้อีก. ของที่ลิสต์ไว้ผิดจริงย้ายไป §1b "Known remaining debt" พร้อมตัวเลขที่วัดเอง — `LearnerDirectorySelector.tsx` มี native button **11 จุด** และเป็น control จริงทั้งหมด (ไม่ใช่ primitive boundary) จึงเป็นหนี้ที่ต้องเปิดแผนแยก ไม่ใช่ของที่ allowlist ได้. ส่วน `AppTable.tsx` มี native button **1 จุดเดียว** (บรรทัด 365) ซึ่ง §2 refactor ไปแล้ว ⇒ เหลือศูนย์ ไม่ต้อง allowlist.
2. **`data-standard-exception` ship ขึ้น DOM** → ตัดออก ใช้ `// eslint-disable-next-line <rule> -- <reason>` และเขียนกำกับเหตุผล (React forward `data-*` ไป HTML จริง) ไว้ใน §1 กัน implementer เอากลับมา.
3. **custom TS-API script** → เปลี่ยน §1 เป็น ESLint flat-config ล้วน 3 บล็อก พร้อมสเปกที่ implement ได้ตรง ๆ. จุดที่ต้องระวังและเขียนเตือนไว้แล้ว: (ก) ใช้ **rule ต่างชื่อกัน** — `no-restricted-globals` สำหรับ fetch, `no-restricted-syntax` สำหรับ JSX `button` — เพราะ flat config **แทนที่** option array ของ rule เดิมไม่ใช่ merge ถ้าเอาสองเรื่องไปกอง `no-restricted-syntax` อันเดียว บล็อกหลังต้อง restate selector ก่อนหน้าทั้งหมด (บั๊กที่รอเกิด) (ข) `no-restricted-globals` จับ bare `fetch` ไม่จับ `window.fetch` — ตรวจแล้วว่า `src/` ทั้งหมดไม่มี `window.fetch`/`globalThis.fetch` จึงพอ. negative check เปลี่ยนไปใช้ `npx eslint --stdin --stdin-filename` = ไม่ต้อง commit fixture เลย. ลบ `npm run standards:check` ออกจาก scope.
4. **`AppLinkButton` ไม่มี consumer** → ตัดออกจาก contract นี้ พร้อม §2.2 shared style map ที่พึ่งพากัน. เหตุผลบันทึกไว้ใน §2: `<Link>` ใน Batch A+B มีจุดเดียว (`AssignmentDetailPage.tsx:997`) และเป็น inline text link ที่กฎในแผนเองบอกให้คงไว้; สแกนทั้ง repo เจอ link ที่ style เป็นปุ่มจริง 1 จุดคือ `CourseDetailPage.tsx:655` (icon `<Link>` `p-1 rounded-md` นั่งระหว่าง `IconButton` สองตัว) ซึ่งอยู่นอกทุก batch — จดไว้เป็น candidate แรกของแผนต่อไปแล้ว.

**Minor 5-10 แก้ไปพร้อมกันด้วย** (อยู่ในไฟล์เดียวกัน ต้นทุนแทบไม่มี): §4.4 เขียนใหม่ว่า HealthCheckPage **ไม่ต้องแก้อะไร** เพราะคอมเมนต์เรื่อง 503 มีอยู่แล้วที่บรรทัด 39-42 และ `probeHealth` เป็น local function ไม่ใช่ helper กลาง (5) · §4.3 สั่งชัดว่าเปลี่ยนแค่ตัว `fetch` ห้ามเขียน filename parser ใหม่ เพราะ `downloadBlob.ts` มี `downloadBlob`+`filenameFromContentDisposition` และสองหน้านั้นใช้อยู่แล้ว (6) · §4.1 ยก header merge เป็น hard invariant พร้อมอธิบายว่าทำไม `Accept` ของ Excel รอดอยู่ทุกวันนี้ และพังเงียบแบบไหนถ้า helper force ทับ (7) · §7 ตัดคำว่า CI ออกและระบุว่า repo **ไม่มี CI** (`.github/workflows/` ว่าง ไม่มี husky/lint-staged) gate จริงคือ `npm run lint` — เปลี่ยนกลไก phase มาใช้ severity `'warn'`→`'error'` แทน reporting mode แยก (8) · Delivery order เลิกจองเลข 158-163 เขียนเป็นลำดับงาน + สั่งให้เรียก `-Next` ตอนสร้างไฟล์จริง (9) · หัวไฟล์เปลี่ยนเป็น `Supersedes:` และตั้ง PLAN-156 เป็น `SUPERSEDED` แล้ว (10).

**ไม่ได้แค่เสนอกลไกใหม่แล้วโยนให้ implementer เดา — รัน config ที่เขียนใน §1 กับ working tree จริงแล้ว** ผ่านไฟล์ `eslint.probe.config.js` ชั่วคราว (ลบทิ้งแล้ว, `eslint.config.js` ตัวจริงไม่ถูกแตะ): กฎทั้งสองยิงถูกผ่าน `--stdin-filename` ✓ · override บล็อก 2 ปิด fetch rule ให้ 4 ไฟล์ allowlist ได้จริง ✓ · button rule ไม่รั่วไป `components/ui` ✓ · และรันทั้ง `src/pages/**` ได้ **19 + 2 เป๊ะ ตรงกับตาราง baseline ทุกไฟล์ทุกจำนวน** ⇒ ทั้ง baseline และกลไกวัดยืนยันกันเอง. ผลและ caveat เรื่อง react-hooks plugin บันทึกไว้ใน §1 แล้ว.

**§3, §5, §6 และ AC#5-8 ผ่านตามที่เขียนเดิม ไม่ได้แตะ.** Status → `READY` — ของที่ยังไม่ได้ทำคือตัว implement เอง ไม่มี finding ค้าง.

## Reviewer Notes — รอบ implement (Claude Code, 2026-07-30) → VERIFIED

รีวิว PLAN-158..163 (commit `68593ca`, `c7fe2e5`, `49d57da`, `3284bf1`) เทียบ AC ทั้ง 8 ข้อ — **ผ่านทั้งหมด ไม่มี finding ที่ต้องตีกลับ**

ยืนยันด้วยการรันเอง ไม่ใช่อ่านโค้ดเฉย ๆ:

- `npm run lint` ✓ exit 0 · `npm run build` ✓ exit 0 (`tsc -b && vite build`)
- `dotnet test --filter ~CourseVersionLearnerPolicyTests` ✓ **8/8 ผ่าน**
- **mutation test พิสูจน์ว่า test ใหม่กันของจริง** — revert `_dateTime.Now` → `DateTime.UtcNow` ทีละบรรทัด: บรรทัด 200 (`CourseVersion`) ทำ assertion บรรทัด 78 แดง, บรรทัด 581 (`CourseContentItem`) ทำ assertion บรรทัด 79 แดง (`Expected 2026-04-28T10:30 / Actual 2026-07-30T05:37`) ⇒ **assertion มีชีวิตทั้งสองตัว** ไม่ใช่ test ที่ผ่านลอย ๆ. ตรวจ `InMemoryGenericRepository.AddEntity` แล้วด้วยว่า**ไม่ได้ stamp `CreatedAt` เอง** (แตะแค่ `Id`) จึงไม่มี false positive. คืนไฟล์ด้วย `git checkout` แล้ว ยืนยันบรรทัด 200/581 กลับเป็น `_dateTime.Now`
- AC#2 ✓ native `<button>` ใน `src/pages/**` = **0** · AC#3 ✓ `AppTable.tsx` = **0** และใช้ `IconButton` โดยคง `stopPropagation`/`title`/tone ครบ
- **ไม่มี `eslint-disable` bypass แม้แต่จุดเดียว** — แข็งกว่าที่ §7.4 เรียกร้อง (เผื่อไว้ว่าจะมี exception แต่ไม่ต้องใช้เลย)
- AC#4 ✓ export สองหน้าใช้ `fetchResponseWithAccessControl` และ **`Accept: application/vnd.openxmlformats-…` ยังอยู่ครบ** (invariant ที่ §4.1 เตือน) · `downloadBlob`/`filenameFromContentDisposition` ไม่ถูกแตะ ⇒ filename fallback เดิม · error path อัปเกรดเป็น `ApiError` ส่ง server message เข้า toast ตามที่ §4.3 คาดไว้
- AC#5 ✓ `HealthCheckPage.tsx` **ไม่อยู่ใน diff เลย** ⇒ พฤติกรรม 503 เดิมปลอดภัยโดยโครงสร้าง
- AC#6 ✓ `ReportKpiTile` map tone ได้ class **ตรงกับของเดิมทุกค่า** (`neutral→text-slate-900`, `info→text-indigo-700`, `success→text-emerald-700`, `danger→text-rose-600`) และ markup wrapper/label/value เหมือนเดิมทุกตัวอักษร ⇒ visible output ไม่เปลี่ยนจริง
- AC#7 ✓ ตามข้อ mutation test ข้างบน · AC#8 ✓ `git diff --check` ✓
- **§3 กฎที่เสี่ยงสุดไม่ได้ถูกใช้เลย** — grep diff ของ `src/pages` (617 บรรทัด) ไม่มี `type="submit"`/`disabled`/`loading` ถูกเพิ่มหรือลบแม้แต่บรรทัดเดียว ⇒ ปุ่มทั้ง 19 จุดเป็น plain click handler ทั้งหมด ความเสี่ยงเรื่อง submit/loading หลุดจึงเป็นศูนย์ (ยืนยันด้วยหลักฐาน ไม่ใช่สันนิษฐาน)

ข้อสังเกต (ไม่ใช่ finding — ไม่ต้องแก้ในรอบนี้):

1. **`apiClient.ts` มี CRLF→LF ทั้งไฟล์** ทำให้ diff บวมเป็น 366 บรรทัดทั้งที่ของจริง 16+/10- (`git diff -w` ยืนยัน). `.gitattributes` บรรทัด 4 คือ `* text=auto` ⇒ blob เดิมที่เป็น CRLF เป็นตัวผิดปกติ การ normalize ครั้งนี้**ถูกตามนโยบาย** เสียแค่ blame ของไฟล์นั้นเลอะ. PLAN-158 Implementer Notes disclose เรื่องนี้ไว้แล้ว = ไม่ได้ปิด
2. **การ์ดสี hover เปลี่ยนเล็กน้อยในคอลัมน์ action ทุกตาราง** — `IconButton` ให้สี resting **ตรงกับของเดิมทั้ง 4 tone** แต่ hover ต่าง (`hover:bg-rose-50`→`red-50`, ghost `hover:text-slate-700`→`slate-600`) และเพิ่ม `active:`/`focus-visible:` ring + `aria-label` (ได้ a11y ดีขึ้น). อีกจุด: `size="sm"` ใส่กรอบ `h-7 w-7` และ `[&_svg]:h-4` ซึ่ง specificity ชนะ `h-3.5` ของไอคอน `Info` ⇒ ไอคอนโตขึ้น ~2px. ควรเหลือบตาดูจริงครั้งหนึ่ง แต่ AC ไม่ได้เรียกร้อง pixel parity สำหรับปุ่ม (ต่างจาก KPI tile ที่ AC#6 บังคับ)
3. **class ซ้ำ 3 ไฟล์** — dropzone "เลือก content" (`w-full min-h-[104px] … border-dashed …` ยาว ~130 ตัวอักษร) ถูก copy เหมือนกันใน `VersionDetailPage`/`VersionFormPage`/`CourseEditorPage`. เป็นของที่ซ้ำอยู่**ก่อนแล้ว** (migration แค่ย้ายมาไว้บน `AppButton`) ไม่ใช่ของใหม่ แต่เป็นเป้า dedup ตามเจตนา §5 — เอาไปแผนต่อไปได้
4. **`variant="ghost"` + `className="px-2 text-amber-600 hover:text-amber-800"`** (1 จุดใน `AssignmentDetailPage`) เป็นสีนอก vocabulary 4 variant. สีเดิมก็ amber จึงไม่ใช่ regression แต่ถ้าจะให้ครบมาตรฐานควรมี tone `warning` ใน `AppButton` แทนการ override
5. **README ใช้คำที่อาจถูกอ้างผิดในอนาคต** — เขียนว่า approved exceptions เป็น "file-scoped in `eslint.config.js` (not inline)" ซึ่งตรงกับ allowlist 4 ไฟล์ที่ §1 ออกแบบไว้ แต่ §7.4 เจตนาให้ exception **ใหม่** เป็น inline `eslint-disable … -- reason` เท่านั้น. ควรกำกับใน README ว่า allowlist 4 ไฟล์นี้ **ปิดรายการแล้ว** ของใหม่ให้ใช้ inline

**สรุป: PLAN-157 + PLAN-158..163 → `VERIFIED`** หนี้ที่ยังเปิดอยู่คือ §1b (`LearnerDirectorySelector` 11 ปุ่ม + `NotificationRow`/`Header`/`Sidebar`) และข้อสังเกต 3-5 ข้างบน — ทั้งหมดอยู่นอก contract นี้โดยเจตนา ให้เปิดแผนใหม่ถ้าจะทำ
