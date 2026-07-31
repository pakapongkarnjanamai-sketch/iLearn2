# PLAN-178: Replace Assignment Gantt custom renderer with SVAR React Gantt

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

Route `/admin-react/assignments/gantt` currently uses a custom Gantt implementation split across:

- `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`
- `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`
- `iLearn.Admin.React/src/pages/assignments/gantt/GanttBar.tsx`
- `iLearn.Admin.React/src/pages/assignments/gantt/ganttScale.ts`
- `iLearn.Admin.React/src/pages/assignments/gantt/ganttStatus.ts`

The custom renderer has now needed repeated QA fixes for scrollbar placement, Month width, weekend shading, empty lower chart space, and header/body guide alignment (PLAN-172 through PLAN-177). The core issue is that this page is hand-building a timeline layout engine: scale math, sticky header/name column, horizontal/vertical scroll behavior, guide lines, weekend bands, Today marker, hover cards, and zoom behavior. Sub-pixel rounding and scroll/header synchronization defects are likely to keep recurring.

The recommended replacement is `@svar-ui/react-gantt`:

- npm package: `@svar-ui/react-gantt`
- Current checked version: `2.7.1`
- License: MIT for the core package
- Peer dependencies: `react >=18`, `react-dom >=18` (compatible with this React 19 app)
- Docs: `https://docs.svar.dev/react/gantt/`
- Product page states TypeScript support, React 18/19 compatibility, customizable timeline/grid/bars, zooming, and virtualization.

Other candidates considered:

| Package | Decision |
| --- | --- |
| `@progress/kendo-react-gantt` | Strong enterprise component, but commercial license and many Kendo peer dependencies. Use only if the organization already owns KendoReact licensing. |
| `@syncfusion/ej2-react-gantt` | Feature-rich, but commercial license and a separate large UI ecosystem. |
| `gantt-task-react` | MIT, but peer dependency targets React 18 only and package activity/API maturity are weaker for this React 19 + TS6 app. |
| `rc-gantt` | MIT, but older dependencies such as MobX 4 and older React-era assumptions. |
| `react-gantt-elastic` / `react-gantt-timeline` | Too old for new React 19 work. |
| `@pyraxi/gantt` | MIT and React 19-compatible, but it wraps/depends on SVAR plus extra scheduling/export dependencies; heavier than needed for an assignment schedule viewer. |

## Goals

1. Replace the fragile custom Gantt renderer with SVAR React Gantt for `/admin-react/assignments/gantt`.
2. Keep the current API contract from `GET Assignments/gantt` unchanged.
3. Preserve current user-facing behavior:
   - status filters (`All`, `InProgress`, `Upcoming`, `Completed`, `Expired`)
   - legend for statuses present in the loaded dataset
   - Day / Week / Month zoom controls
   - Today action
   - click-through to `/assignments/:id`
   - compact assignment number + description task labeling
   - status color mapping (`InProgress`, `Expired`, etc.)
4. Make the chart read-only. Assignment schedule changes must still go through existing assignment workflows, not drag/drop edits in the Gantt.
5. Remove the custom timeline math and guide-line rendering responsible for the repeated pixel alignment defects.

## Scope

### Package setup

1. Install the open-source core package:

   ```powershell
   cd iLearn.Admin.React
   npm install @svar-ui/react-gantt
   ```

2. Import SVAR CSS only from the Gantt route/component, not globally in unrelated pages, unless the library requires a global import.
3. If bundle size increases materially, lazy-load the SVAR chart component from the Gantt route with `React.lazy` / dynamic import so normal admin pages are not penalized.

### New component structure

Create a replacement component under the existing `assignments/gantt` folder, for example:

```text
iLearn.Admin.React/src/pages/assignments/gantt/AssignmentSvarGanttChart.tsx
iLearn.Admin.React/src/pages/assignments/gantt/svarGanttMapping.ts
```

Use the existing page shell in `AssignmentGanttPage.tsx` for loading, filters, toolbar controls, and empty states. The new SVAR component should own only the chart rendering and library-specific configuration.

### Data mapping

Keep the current frontend contract mirror of `AssignmentGanttTaskDto`:

```ts
type GanttTask = {
  id: number
  parentId: number
  assignmentNo: string
  title: string
  startDate: string
  dueDate: string
  progress: number
  color: string
  status: string
}
```

Map it to SVAR tasks with explicit helper functions instead of inline mapping in JSX:

- `id`: `task.id`
- `text`: use `task.title`, falling back to `task.assignmentNo`
- `start`: parsed local `Date` from `task.startDate`
- `duration`: inclusive day span from `startDate` through `dueDate`, minimum `1`
- custom fields: `assignmentNo`, `status`, `dueDate`, `title`
- parent/grouping: start flat; do not create summary/group rows unless SVAR requires it or QA asks for grouping

Preserve click-through by handling task click/select and navigating to `/assignments/${id}`. If SVAR does not provide a direct React Router link hook in bars, use the library event callback with `useNavigate()`.

### Read-only behavior

Disable or intercept all schedule-mutating interactions:

- drag task bar
- resize task bar
- create task
- delete task
- dependency creation/editing
- inline date editing
- built-in task editor / context menu if present

If the library cannot fully disable a mutation action through props, intercept the action callback and return without mutating local state. Do not call the API for any Gantt edits in this plan.

### Visual integration

1. Keep the chart inside the current `DataGridSurface` card area.
2. Preserve the existing filter chips and zoom toolbar primitives (`SegmentedToggle`, `AppButton`) in `AssignmentGanttPage.tsx`.
3. Adapt the SVAR grid/sidebar columns to the compact admin style:
   - Assignment No
   - Title / Description
   - optional Status if it does not clutter the chart
4. Apply status color mapping via the library's supported task/bar styling API:
   - `InProgress`: indigo/blue
   - `Expired`: red
   - `Upcoming`: neutral/info
   - `Completed`: green/neutral depending on existing app convention
5. Do not reintroduce weekend background shading for Week/Month. If Day weekend styling is required, use the library's supported calendar/timeline styling API only; do not add custom pixel grids over the library renderer.
6. Keep helper copy minimal. Do not add in-app instructions explaining the chart.

### Zoom / Today behavior

Map the existing Day / Week / Month segmented control to SVAR's scale/zoom API.

Expected behavior:

- Day: detailed date cells
- Week: weekly timeline labels
- Month: monthly timeline labels
- Today: scroll/focus the timeline to the current date if the library exposes a method; otherwise select a date range centered around today through the library-supported API

Avoid DOM queries like `document.getElementById` or manual `scrollLeft` math unless there is no library API available.

### Remove or retire custom code

Once the SVAR replacement passes QA:

- delete `GanttChart.tsx`
- delete `GanttBar.tsx`
- delete `ganttScale.ts`
- delete `ganttStatus.ts` only if no longer used by the legend/status mapping; otherwise keep a small status-color helper with a more general name
- remove any imports from the old custom renderer

Do not delete files in the first half of the implementation until the SVAR chart compiles and basic rendering works; keep the old files temporarily as fallback during the migration branch.

## Out of scope

- Backend/API changes.
- Database changes.
- Persisting edits from the Gantt chart.
- Dependency arrows between assignments.
- Resource planning, auto-scheduling, critical path, baselines, import/export, or PRO-only features.
- Deploying to PROD unless requested after QA review.

## Contract changes

- API shape: none.
- DB: none.
- Frontend dependencies: add `@svar-ui/react-gantt` and its transitive dependencies to `package.json` / lockfile.

## Acceptance Criteria

1. `/admin-react/assignments/gantt` renders using SVAR React Gantt, not the old custom CSS grid renderer.
2. The chart renders all rows from `GET Assignments/gantt` and respects the existing status filters.
3. Day / Week / Month controls change the library timeline scale without header/body drift.
4. The horizontal scrollbar/header/body alignment is handled by the library, with no custom percentage/pixel guide-line math remaining.
5. Bars display status colors and labels compactly enough for the admin console.
6. Clicking a task opens `/assignments/:id`.
7. Dragging/resizing/editing/creating/deleting in the Gantt cannot mutate schedules.
8. Empty/no-match/loading states still work.
9. `npm run lint` and `npm run build` pass.
10. QA smoke on `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` confirms route 200 and basic visual alignment at Day / Week / Month.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Recommended QA deploy after local validation:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-admin-react.ps1
```

Manual QA checklist:

- Load `/iLearn/admin-react/assignments/gantt`.
- Confirm all assignment rows render.
- Switch Day / Week / Month and verify header/body alignment stays correct while horizontally scrolling.
- Confirm Week and Month do not show weekend background shading unless SVAR's default theme exposes non-disruptive styling that has been explicitly accepted.
- Click at least one assignment bar/row and confirm navigation to detail page.
- Try dragging/resizing a bar and confirm no schedule mutation occurs.
- Confirm status filters still work and do not rescale unexpectedly unless the library requires a natural viewport recalculation.

## Implementation Notes

- Installed `@svar-ui/react-gantt@2.7.1` (MIT) and imported the route CSS through the lazy-loaded chart component.
- Added `AssignmentSvarGanttChart.tsx` and `svarGanttMapping.ts`. The adapter maps the existing `AssignmentGanttTaskDto` mirror to SVAR `ITask` values with local-calendar `start`/`end`, inclusive `duration`, status color, and custom assignment fields.
- Kept `AssignmentGanttPage.tsx` responsible for API loading, filters, legend, loading/empty states, zoom toolbar, and Today action. Today uses the SVAR API `select-task` action with `show: 'xy'`; programmatic selection is prevented from navigating to detail.
- Configured SVAR `readonly`, custom columns, task template, status bar colors, and `onSelectTask` navigation to `/assignments/:id`.
- Lazy-loaded the SVAR chart to keep the main bundle at about `608KB`; the SVAR chunk is about `252KB` plus `32KB` CSS. The custom `GanttChart.tsx`, `GanttBar.tsx`, and `ganttScale.ts` files were removed. `ganttStatus.ts` remains for the page legend.
- Avoided duplicate assignment descriptions: when `title === assignmentNo`, the SVAR Description cell is blank.

## Verification Notes

- `npm run lint` passed.
- `npm run build` passed. Vite retained its existing chunk-size warning for the main bundle; SVAR is route-split.
- QA deploy via `tools/deploy-admin-react.ps1`: `CopySucceeded=True`, `RobocopyExitCode=3`.
- QA smoke: `/iLearn/admin-react/` = HTTP 200; `/iLearn/admin-react/assignments/gantt` = HTTP 200.
- Browser QA confirmed SVAR grid rows, Day/Week/Month controls, readonly behavior, click-through from `aaaa` to `/iLearn/admin-react/assignments/314`, and no duplicate assignment description.
- `npm install` reported 7 dependency-tree vulnerabilities (1 low, 1 moderate, 5 high). `npm audit fix` was not run because it could introduce unrelated dependency changes; follow-up security review is recommended.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED.** Acceptance Criteria ครบทั้ง 10 ข้อ

ตรวจซ้ำเอง:

- ข้อ 1/4: renderer เดิมถูกลบจริง — `src/pages/assignments/gantt/` เหลือ `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts` (legend) ไม่มี percentage/pixel guide math หลงเหลือ
- ข้อ 7: `readonly` ถูกส่งให้ `<Gantt>` จริง (`AssignmentSvarGanttChart.tsx:79`)
- ข้อ 8: loading / no-data / no-match states ยังอยู่ครบ (`AssignmentGanttPage.tsx:125-135`)
- ข้อ 9: `npm run lint` ✓, `npm run build` ✓ — reproduce hash `index-CHu7nUEk.js` ตรงกับ asset ที่ deploy บน QA
- ข้อ 10: smoke QA root/gantt/asset = 200 ทั้งสาม
- Bundle: SVAR ถูก route-split จริง (`AssignmentSvarGanttChart-*.js` 252 KB + CSS 32 KB แยกจาก main 608 KB) ⇒ หน้าอื่นไม่โดนคิดน้ำหนัก

**แก้ความเข้าใจใน Verification Notes ข้อสุดท้าย:** vulnerabilities 7 รายการ **ไม่ได้มาจาก `@svar-ui/react-gantt`** — `npm audit` รายงานทั้ง tree ไม่ใช่เฉพาะ package ใหม่ ตัวที่ติดจริงคือ `react-router`/`react-router-dom` (5 advisories, high), `vite` 8.0.0-8.0.15 (2 รายการ รวม NTLMv2 hash disclosure ผ่าน UNC path บน Windows), และ `ws` 7.x (DoS) — ทั้งหมดมีอยู่ก่อน PLAN-178 ⇒ ไม่ใช่ regression ของแผนนี้ แต่เป็นหนี้ที่ควรมีแผนแยก (ยังไม่ได้ทำ)

ประเด็นค้างที่ตกทอดมาถึงโค้ดปัจจุบัน — ส่งต่อให้ [PLAN-180](./PLAN-180-assignment-gantt-visual-redesign.md) ทั้งหมด:

1. หัวคอลัมน์ `'Assignment'`/`'Description'` hardcode อังกฤษ ไม่ผ่าน `t()` (`AssignmentSvarGanttChart.tsx:81-82`) — ผิดกติกา labels ของ repo
2. `statusBarColor` (`svarGanttMapping.ts:43-49`) ซ้ำกับ `STATUS_BAR_CLASS` ใน `ganttStatus.ts` ทั้งที่คอมเมนต์ไฟล์หลังระบุว่าเป็น single source of truth
3. cell ในตารางซ้ายไม่ truncate ⇒ ข้อความ wrap ทับกันเมื่อ description ยาว
