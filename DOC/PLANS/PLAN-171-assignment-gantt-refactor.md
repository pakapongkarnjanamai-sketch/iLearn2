# PLAN-171: Assignment Gantt refactor

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

Route `/admin-react/assignments/gantt` renders `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx` — a single 262-line component with a hand-rolled timeline grid. Data comes from `GET Assignments/gantt` → `AssignmentsController.GetGanttTasks` → `AssignmentService.BuildGanttTasksAsync` / `MapGanttTask`.

QA review of the page (screenshot + code read) found rendering defects, three CLAUDE.md / React-README rule violations, and two backend data bugs. The endpoint has exactly **one** consumer (this page), so its response shape can be corrected in the same delivery.

## Defects (evidence)

### Backend — `iLearn.Application/Services/AssignmentService.cs`

| # | Defect |
|---|---|
| B1 | `MapGanttTask` builds `Title = $"{assignmentNo} - {first.Description ?? "No Description"}"` while the DTO **also** carries `AssignmentNo`. The page renders both fields ⇒ every row reads `AS-20260721-002  AS-20260721-002 - …` (visible in QA). An empty-but-not-null `Description` also leaves a dangling `" - "`. |
| B2 | Batch dates come from `first = group.OrderBy(item => item.Id).First()` only. A batch (`AssignmentNo` group) whose rows carry different `StartDate`/`DueDate` renders the span of one arbitrary row instead of the batch's real span. |

### Frontend — `AssignmentGanttPage.tsx`

| # | Defect |
|---|---|
| F1 | **Shadowing of the label function.** `filtered.map((t) => …)` (lines 163, 220) and `const t = new Date()` (line 84) shadow the imported `t()` from `lib/labels`. Any label call added inside those scopes silently breaks — a live footgun. |
| F2 | **Header height drift.** Left pane header is `h-14` (56px); right pane header is month row (~26px) + day row (28px) ≈ 54px. Two independent magic numbers, no shared constant ⇒ the two panes are already ~2px out of alignment and will drift again on any style change. |
| F3 | **Nothing is sticky.** Vertical scroll hides the month/day scale; horizontal scroll hides the assignment-name column. On a 12-row × 3-month chart both are already reachable. |
| F4 | **One fixed zoom** (`DAY_PX = 18`). 12 batches over ~3 months ≈ 1,800px of horizontal scrolling; a year of data is unusable. No day/week/month scale. |
| F5 | **Range recomputed from `filtered`** (line 67), so switching the status filter rescales the whole chart and every bar jumps. Scale must be stable. |
| F6 | `scrollToToday` uses `document.getElementById('gantt-today-marker')` + `scrollIntoView` — a DOM query instead of a ref, and `scrollIntoView` can scroll ancestor scrollers / the page, not just the timeline. |
| F7 | **Rule violations** — filter chips hand-rolled from `AppButton` instead of `SegmentedToggle variant="filter"`; dates formatted through a local `Intl.DateTimeFormat` and progress through `{t.progress}%` instead of `src/lib/format.ts`; bar colours are backend hex literals (`#1890ff`, `#52c41a`, …) that bypass the app's `STATUS_TONES` palette; `STATUS_FILTERS` re-declares status strings that already exist as `LEARNER_STATUS_KEYS` / `STATUS_LABELS`. |
| F8 | **Bars are inert** — no click-through to `/assignments/:id`, no keyboard focus, tooltip is a bare `title` attribute. Left-column titles use `truncate` with no `title` at all, so long batch names are unreadable. |
| F9 | Body rows have no weekend/grid banding (only the header row does), so tracing a bar back to a date across ~1,800px is guesswork. The header renders one `<div>` per day with no upper bound. |
| F10 | `counts` makes four full passes over `tasks` (line 108). |

## Decisions (requester, 2026-07-30)

- **D1 — drop the progress overlay.** Bars show the time span + status only; the `%` label and the white progress fill are removed. This matches PLAN-168/169/170, which removed completion metrics from assignment reporting. `Progress` stays on the DTO (no contract removal); the Gantt page simply stops rendering it.
- **D2 — scope = frontend rewrite + B1/B2 only.** Effective-date rework and server-side filtering/paging are explicitly deferred.

## Scope

### Part A — Backend (`AssignmentService.MapGanttTask`)

1. `Title` = `first.Description` trimmed; when blank fall back to `assignmentNo` (never prefix `AssignmentNo`, never emit `"AssignmentNo - "`).
2. Dates computed over the whole group, not `first`:
   - `startDate` = `Min(row.StartDate ?? row.CreatedAt)` across the group
   - `dueDate` = `Max(row.DueDate)` across the group; when every row has a null `DueDate`, keep the existing `startDate.AddDays(7)` fallback
   - keep the existing `dueDate <= startDate ⇒ startDate.AddDays(1)` guard, applied **after** the Min/Max
3. No DTO field added or removed, no migration, no other endpoint or service touched.

### Part B — Frontend

Split the page into a shell plus pure helpers (keep the page file at its current path so `App.tsx` needs no change):

```
src/pages/assignments/AssignmentGanttPage.tsx   ← shell: fetch, filters, states, legend
src/pages/assignments/gantt/ganttScale.ts       ← pure scale math + shared constants
src/pages/assignments/gantt/GanttChart.tsx      ← grid: sticky name column, sticky header, rows
src/pages/assignments/gantt/GanttBar.tsx        ← one bar: link, focus, hover card
```

`ganttScale.ts` owns **every** layout number (`ROW_H`, `HEADER_MONTH_H`, `HEADER_TICK_H`, `ZOOM_LEVELS`) and exports `buildTimeline(tasks, zoom)` → `{ rangeStart, totalDays, pxPerDay, months, ticks }`. Both panes read the same constants — fixes F2 structurally.

Behaviour to implement:

1. **Zoom** — `SegmentedToggle` (`variant="segment"`) with Day / Week / Month. Suggested `pxPerDay`: 22 / 8 / 3. Tick row shows day numbers at Day, week-start dates at Week, nothing (month row only) at Month. Default zoom = Day when the full range ≤ 60 days, otherwise Week.
2. **Sticky** — name column `sticky left-0`, header rows `sticky top-0`, corner cell rendered last in DOM. All of them `z-10` per the CLAUDE.md ladder (content / sticky thead / sticky column = `z-10`); **nothing inside this card may exceed `z-10`**.
3. **Stable scale** — `buildTimeline` runs over **all** tasks, never the filtered subset (fixes F5). Keep the ±3-day pad and the 14-day minimum.
4. **Today** — `useRef` on the timeline scroller; the Today button sets `scrollLeft` so the marker lands centred (no `scrollIntoView`). Centre once on first successful load.
5. **Bars** — `<Link to={`/assignments/${task.id}`}>` (matches `CourseDetailPage.tsx:767`), keyboard-focusable with a `focus-visible` ring, `aria-label` = title + status + date range. Colour from `status` via a local map onto the app's tone palette; the DTO's `color` field stays in the TS type (contract mirror) with a comment saying the UI ignores it.
6. **Hover card** — replaces the `title` attribute: `formatDate(startDate)` – `formatDate(dueDate)`, duration in days via `formatNumber`, and `learnerStatusLabel(status)`. Must stay inside the chart's bounds (flip alignment within ~200px of the right edge). No `%` (D1).
7. **Left column** — `assignmentNo` in mono + batch title on one line at `ROW_H`, with a native `title` on the truncated text (fixes F8's second half). After B1 the number appears exactly once.
8. **Grid/weekend banding** — a `repeating-linear-gradient` background on the rows container instead of per-day `<div>`s in the body; day cells are rendered in the header only, and only at Day zoom (fixes F9).
9. **Filters** — `SegmentedToggle variant="filter"`, options derived from `STATUS_LABELS` (`All`, `InProgress`, `Upcoming`, `Completed`, `Expired`), counts from a **single** reduce pass (fixes F7/F10).
10. **Naming** — map params renamed `task`, the today value renamed `today`; the imported `t()` is never shadowed (fixes F1).
11. **States** — keep `LoadingState`; distinguish "no assignments at all" from "no assignments match this filter" (reuse `ASSIGNMENT_LABELS.noAssignments` for the former; add a filtered-empty label pair if none fits).
12. **No progress fill, no `%` text on bars** (D1).

Any new user-facing string goes into `ASSIGNMENT_LABELS` as a `{ th, en }` pair — no literals in JSX.

## Out of scope

- Effective dates for Gantt rows (the `GetEffectiveSchedule` / PLAN-086 pattern) — the Gantt still reads `Assignment.StartDate/DueDate`.
- Server-side filtering, paging, or a date window on `Assignments/gantt`.
- Dependency arrows, drag-to-reschedule, export/print of the Gantt, dark mode.
- Any change to `AssignmentDetailPage` / `AssignmentReportPage`.

## Contract changes

- `AssignmentGanttTaskDto.Title` semantics change: **description only**, no `AssignmentNo` prefix. Sole consumer is `AssignmentGanttPage`; update the React type's `// Mirrors AssignmentGanttTaskDto (iLearn.Application/DTOs/AssignmentApiResponseDtos.cs)` comment to match.
- `StartDate` / `DueDate` now describe the whole `AssignmentNo` group instead of its first row. No field added or removed; no DB or migration impact.

## Verification

```powershell
# Frontend (from iLearn.Admin.React)
npm run lint
npm run build

# Backend
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Manual QA on `/admin-react/assignments/gantt`:

- row label shows the AS number exactly once, and a batch with no description shows no dangling `-`
- switching the status filter does **not** rescale or shift the timeline
- Today centres the marker inside the timeline only (page does not scroll)
- Day / Week / Month zoom each render a readable scale
- header stays visible on vertical scroll; name column stays visible on horizontal scroll
- clicking a bar opens the assignment; Tab reaches bars and shows a focus ring
- bars show no `%` and no progress fill
- filtered-empty state differs from the no-data state

## Implementer Notes

- Backend: updated `MapGanttTask` to compute batch span from grouped rows (`Min(StartDate ?? CreatedAt)`, `Max(DueDate)` fallback + existing guard) and changed `Title` to trimmed description with assignment-no fallback.
- Frontend: split the page into `AssignmentGanttPage.tsx` + `ganttScale.ts` + `GanttChart.tsx` + `GanttBar.tsx`; moved all timeline constants into `ganttScale.ts` and kept API contract mirror comment on `GanttTask` (UI ignores backend `color`).
- Implemented Day/Week/Month zoom (`SegmentedToggle`), sticky header + sticky name column (`z-10` only), stable timeline scale from all tasks, ref-based Today centering, linked/focusable bars with hover card, filtered-empty state, and dropped progress overlay/% text.
- Labels: added Gantt-specific localized keys in `ASSIGNMENT_LABELS` (`zoomDay|zoomWeek|zoomMonth`, filtered-empty text, `durationDays`).
- Verification run: `npm run lint`, `npm run build`, `dotnet build iLearn.Tests -o artifacts\verify-test`, `dotnet test artifacts\verify-test\iLearn.Tests.dll`, cleanup `Remove-Item -Recurse -Force artifacts\verify-test`.

## Reviewer Notes

**Claude Code, 2026-07-30 — first pass NOT PASSED.** Two blockers (G1, G2) plus G3–G4. **All eight findings (G1–G4, then the four minors) were fixed by Claude Code on request and verified against the running component — see "Fix applied" at the end. Status is `VERIFIED`.**

Independently verified: `npm run lint` clean, `npm run build` OK (only the pre-existing chunk-size warning). Backend B1/B2 are correct as written — `group.Min(...)` / `Max()` over `IEnumerable<DateTime?>` returns `null` for an all-null group, so the `AddDays(7)` fallback and the `dueDate <= startDate` guard still apply in the right order. F1, F5, F6, F7, F10 and D1 are all genuinely resolved.

### G1 — BLOCKER — every name cell renders one row below its bar

`GanttChart.tsx:46-114`. The grid emits `col-start-2` items before `col-start-1` items (timeline header before corner; inside each `.contents` row, bar before name). CSS grid sparse auto-placement moves the cursor to the next row whenever an auto-placed item's definite column is *before* the cursor's column, so **every column-1 item drops one grid row**.

Measured in Chrome on a reduced repro of this exact structure (`grid-template-columns: 340px 800px`, sticky cells, `display: contents` row groups):

```
hdrTime top=0   left=340      corner  top=54  left=0     ← corner should be top=0
bar1    top=54  left=340      name1   top=108 left=0     ← 34px = one full row off
bar2    top=108               name2   top=142
bar3    top=142               name3   top=176
MISALIGNED ROWS: row1 | row2 | row3
```

So the corner header lands inside the first data row and every assignment name sits against the wrong bar — the exact failure F2 was raised to prevent, one row instead of 2px. This is not visible in `lint`/`build`; it needs the page open.

Verified fix (same repro, `MISALIGNED ROWS: none`): put an explicit `grid-row` on **every** item (header items row 1, task *i* row *i+2*) so auto-placement is out of the picture, then order the DOM by paint priority — name cells → bar rows → timeline header → corner. With explicit rows the DOM order is free, and that order gives the correct freeze-pane stacking with nothing above `z-10`. Checked with `elementFromPoint` after scrolling both axes: corner wins the top-left region, timeline header wins the header strip, name cells win over bars. Note this means dropping the per-row `.contents` grouping.

### G2 — BLOCKER — a blank description prints the AS number twice

`AssignmentService.cs:1333-1335` falls back to `Title = assignmentNo` when `Description` is blank, and `GanttChart.tsx:106-110` renders `{assignmentNo}` + a hardcoded `-` + `{title}` ⇒ `AS-20260721-002 - AS-20260721-002`.

The QA screenshot rows that read `AS-20260721-002 -` had an empty-string (not null) description, so this hits **exactly the rows B1 was written to fix**, and fails this plan's own acceptance criterion ("row label shows the AS number exactly once"). Fix on either side: give the backend a neutral fallback instead of echoing `assignmentNo`, or have the row hide the separator and title when `title === assignmentNo`.

### G3 — today marker is drawn even when today is outside the range

`ganttScale.ts:192` clamps `todayOffsetDays` into `[0, totalDays - 1]`, which makes the guard at `GanttChart.tsx:117` (`>= 0 && < totalDays`) permanently true. When every batch is in the past or future, an indigo "today" line is pinned to the chart edge and the Today button scrolls to it — the pre-refactor code tested the unclamped offset and hid the line. Keep the raw offset (or add an explicit `isTodayInRange`) and clamp only for the scroll target.

### G4 — today line paints over the frozen name column

`GanttChart.tsx:117-125`. The marker is a sibling *after* `.grid` at `z-10`; the sticky name cells are also `z-10` but earlier in the DOM, and at equal z the later element wins (the same mechanism measured in the G1 repro). Scrolled right, the line draws across the frozen column. Render it before the sticky cells, scope it to the timeline column, or leave it at `z-auto`.

### Minor (all four fixed in the second pass below)

- **`ZOOM_OPTIONS` is frozen at module load** — `AssignmentGanttPage.tsx:17-21` calls `t()` at module scope. `t()` reads `currentLang` at call time and `setLang` re-renders subscribers, but a module-level const evaluates once, so Day/Week/Month labels stop following the ไทย/EN toggle. Build it inside the component.
- **Hover card clipping** — `GanttBar.tsx:60-67` opens `top-full` inside the `overflow-auto` scroller, so bottom rows clip it. Mirror the existing horizontal flip vertically.
- **No legend** — the Part B file map gives the shell a legend and none is rendered. Status is reachable via the hover card, so either add a compact legend or strike it from the plan.
- **`STATUS_BAR_COLORS` hex literals** — `GanttBar.tsx:17-25` swaps the backend's hex palette for a frontend one. Better than the DTO colors, but F7's point was to stop hardcoding; a Tailwind class map or shared token keeps one source of truth.

### Re-verification for the next pass

`lint` and `build` do not cover any of G1–G4. Open `/admin-react/assignments/gantt` and walk the Manual QA list in this plan — specifically: name cells line up with their bars, the corner header sits above row 1, scroll right and confirm neither bars nor the today line cross the frozen name column, and load a filter whose batches are all in the past to confirm the today line disappears.

## Fix applied (Claude Code, 2026-07-30)

G1–G4 only. The four minors above are untouched and still open.

- **G1** — `GanttChart.tsx`: every cell now pins its own `gridRow` (header row 1, task *i* row *i+2*), so auto-placement can no longer offset a column. The `.contents` per-row wrapper is gone; cells are emitted in paint-priority order (name cells → bar rows → timeline header → corner), which is what gives the freeze-pane its stacking with everything still at `z-10`. Both header cells are now exactly `HEADER_TOTAL_H` tall — the corner previously computed to 62px against the timeline header's 54px, which would have re-introduced an 8px drift and skewed the today-line height.
- **G2** — the row label renders the separator and title only when `title !== assignmentNo`, so a blank description shows the AS number once with no dangling `-`. Fixed in the UI rather than the backend so it also covers the `AssignmentNo`-blank path, where `MapGanttTask` falls back to `Assignment {id}` for both fields. No backend change, no contract change.
- **G3** — `ganttScale.ts` returns the raw `todayOffsetDays` plus an `isTodayInRange` flag (the `clamp` helper is gone); `GanttChart` gates the marker on the flag. The page's own `clamp` on the scroll target already handled out-of-range offsets, so `centerToday` is unchanged.
- **G4** — the today line drops its `z-10`. As a later sibling it still paints above the bars (both `auto`), and the `z-10` header and name column now correctly cover it.

Verified by mounting the real `GanttChart` with fixture data on the Vite dev server (temporary harness, deleted afterwards — `git status` clean) and measuring in Chrome:

- alignment: name/bar tops match on every row (54/54, 88/88, 122/122, 156/156); corner and timeline header both `top=0, height=54`
- row 1 fixture reproduces the QA blank-description case (`title === assignmentNo`) and renders `AS-20260721-002` once
- `buildTimeline` on a past-only batch → `isTodayInRange: false` (offset 403 vs 107 total days); marker absent
- `elementFromPoint` after scrolling both axes (`scrollTop=70, scrollLeft=900`): corner wins the top-left region, the date header wins the header strip, name cells win over bars, and at the marker's x inside the frozen column the topmost element is the name cell — the line no longer bleeds across it

`npm run lint` clean and `npm run build` OK after the fix (pre-existing chunk-size warning only). Backend untouched by this pass, so the `dotnet test` run from the implementation stands.

### Second pass — minors G5–G8 (Claude Code, 2026-07-30)

- **G5** — `zoomOptions` moved into the component body. `AppLayout` remounts the tree with `key={lang}`, which re-runs component bodies but never module initialisers, so the old module-scope const could not follow the toggle.
- **G7** — the "Showing X of Y batches" strip now carries a legend on the right: one swatch + `learnerStatusLabel` per status **present in the data** (derived from the existing `counts` memo, so no extra pass and no entries for statuses nobody has).
- **G8** — new `gantt/ganttStatus.ts` owns the solid fills as a Tailwind class map (`bg-indigo-600` / `bg-amber-600` / `bg-emerald-600` / `bg-red-600` / `bg-slate-400`, `bg-slate-500` fallback); `GanttBar` and the legend both call `ganttStatusBarClass`, so no hex literals remain and the two cannot drift. Tones still follow `STATUS_TONES`; `Badge`'s own classes are soft fills and unreadable on a bar, which is why this is a separate map rather than a reuse.
- **G6** — `GanttBar` takes `flipHoverCardUp`; `GanttChart` sets it for the last two rows when there are more than three (fewer than that, an upward card would hide under the sticky header instead).

Verified by mounting the real `AssignmentGanttPage` with a stubbed `fetch` and an `AppLayout`-style `key={lang}` wrapper (temporary harness, deleted — `git status` clean):

- bar fills resolve to the real palette (`oklch(0.511 0.262 276.966)` = indigo-600, amber-600, emerald-600, red-600) and every legend swatch reports the **same** computed colour as its bars
- legend lists exactly the four statuses in the fixture; filter chips read `ทั้งหมด (5) / กำลังเรียน (2) / …`
- language toggle: zoom labels `รายวัน|รายสัปดาห์|รายเดือน` → `Day|Week|Month` → back to Thai, legend labels following in step
- hover card: row 0 opens downward (+30px), rows 3 and 4 open upward (−90px), and `clippedByScroller: false` for all three

`npm run lint` clean, `npm run build` OK. Backend still untouched.

### Third pass — header label overflow (Claude Code, 2026-07-30)

A month or tick segment narrower than its own label used to spill across the cell border. `overflow-hidden` added to **both** header rows in `GanttChart.tsx` — the tick row has the same defect on the partial first/last week (`27 Jun` in a 15px cell), so fixing only the month row would have left half of it.

Measured per zoom with the real page (temporary harness, deleted). Every cell whose content exceeds its width now reports `overflowX: hidden`, at all three zooms:

| zoom | month cells | tick cells | overflowing cells | all clipped |
|---|---|---|---|---|
| Day | 4 | 67 | `Sept 26` (21px) | yes |
| Week | 4 | 11 | `Jun 26` (31px), `Sept 26` (16px), `27 Jun` (15px), `31 Aug` (15px) | yes |
| Month | 4 | 1 (placeholder) | `Jun 26`, `Sept 26` (16px) | yes |

Consequence worth knowing: a very narrow edge segment now shows a truncated label (`Sept 26` → `S` at 16px) instead of bleeding into its neighbour. `npm run lint` clean, `npm run build` OK.

### Fourth pass — QA feedback: tooltip sliced, chart not fitting (Claude Code, 2026-07-30)

Reported from QA with screenshots: the hover card rendered in fragments, and the chart ran past the card with no visible way to scroll.

- **Tooltip** — the card sat at `z-auto` inside its bar row, so the **bars of the rows below** (later siblings, also `z-auto`) painted straight over it; only the slivers between them stayed visible. The card now takes `z-10`, and the grid emits **bar rows before name cells** (was the reverse) so the frozen name column, which is also `z-10`, still covers a card that reaches it. Both layers remain at `z-10` — the app's z-ladder is untouched.
- **Chart not fitting / no scrollbar** — root cause was a missing `min-w-0` on the page's flex wrapper in `AssignmentGanttPage.tsx`. As a flex item with visible overflow its automatic minimum size is min-content, which the timeline's fixed px columns inflate to the full chart width, so the layout stretched past the card instead of letting the scroller clip. Measured at Day zoom: the wrapper reported `clientWidth 3752` inside a parent of `1198`. With `min-w-0` the scroller is capped at the card width and scrolls.
- **Scrollbar placement** — the scroller no longer takes `flex-1`. Stretched, it parked the horizontal scrollbar at the bottom of the card, ~200px below the last row; content-sized, the bar sits directly under the rows (measured gap 14px including the bar).
- **Dead space at Month zoom** — the timeline column is now `minmax(widthPx, 1fr)`, so row and header borders reach the card's right edge when a zoom level is narrower than the viewport instead of stopping mid-card.

Measured after the fix (12-batch fixture including one 140-day bar, viewport 1280×900):

| zoom | scroller clientW | content scrollW | h-scrollbar | grid fills width | page overflows X |
|---|---|---|---|---|---|
| Day | 1196 | 3750 | 10px | yes | no |
| Week | 1196 | 1580 | 10px | yes | no |
| Month | 1196 | 1196 | none needed | yes | no |

Paint order re-checked after the reorder (`scrollTop 60`, `scrollLeft 900`): corner wins the top-left region, the date header wins the header strip, name cells win over bars, and a card reaching the frozen column loses to the name cell. Hover cards for rows 0, 10 and 11 are topmost across their whole area, open down / up / up respectively, and none is clipped by the scroller. `npm run lint` clean, `npm run build` OK.

Three measurement traps hit while verifying these passes, noted so the next one does not repeat them:

- `elementFromPoint` is **hit testing, not paint order** — the hover card is `pointer-events-none`, so it is skipped and the probe reports whatever sits beneath it. Set `pointerEvents: 'auto'` for the duration of the measurement.
- the Browser pane viewport was 961×415, so rows below the fold returned `outside-viewport` and looked like failures; resize before measuring. `scrollWidth` still exceeds `clientWidth` after `overflow-hidden` (it reports content size, not painted size — assert computed `overflow` instead), and measuring straight after `button.click()` returns the **pre-render** DOM, so all three zooms looked identical until an `await` was added between the click and the measurement.
