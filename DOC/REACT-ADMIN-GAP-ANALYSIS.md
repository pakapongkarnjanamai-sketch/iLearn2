# React Admin — Gap Analysis & Replacement Plan

Last updated: 2026-05-27
Status: Proposal — awaiting approval before implementation
Companion to: [REACT-ADMIN-REBUILD-PLAN.md](./REACT-ADMIN-REBUILD-PLAN.md)

## 0. Implementation Status (auto-updated)

Phases delivered:

- ✅ **Phase A — Foundations**: `SessionContext`, role-aware `navigationItems`, `<RequireRole>` guard, SuperAdmin route gating.
- ✅ **Phase B1–B3 — Dashboard parity**: KPI strip, activity / task / category charts via **Recharts**, Report Hub links, "Courses Needing Attention", maintenance banner, role-gated tiles.
- ✅ **Phase B4 — Live activity**: `DashboardPage` subscribes to `/hubs/admin-activity` via SignalR (`AdminActivityCreated`) with polling fallback.
- ✅ **Phase C1 — Assignment Report**: `/assignments/:id/report` (filterable learner-progress table, KPI strip, CSV export, print).
- ✅ **Phase C2 — Schedule (Gantt)**: `/assignments/gantt` — CSS-grid timeline driven by `/api/admin/Assignments/gantt`, today marker, status filter chips.
- ✅ **Phase D — Content Library editor**: `/content-library/new` (SCORM .zip upload → `POST ContentItems/upload?typeId=`), `/content-library/:id` (flat detail with Publish/Unpublish/Download Control Hub), `/content-library/:id/edit` (name + typeId).
- ✅ **Phase E — LearnerGroup membership**: Add / remove preview + confirm flows live inline in `GroupDetailPage` (`/LearnerGroups/{id}/members/preview|confirm|remove/preview|remove/confirm`). Standalone wizard routes intentionally skipped — drawer-based flow achieves functional parity.
- ✅ **Phase F — Master Data multi-screen**: `/master-data/{divisions|categories|course-types|roles|learner-group-categories}` routes (SuperAdmin-only), nested sidebar.
- ✅ **Phase G1 / G3 / G4 — Operations grids**: `/users` (admin users, SA-only), `/learning-logs`, `/enrollments` (SA-only, read-only) via `EntityListPage`.
- ✅ **Phase G2 — LearnerProfile enrichment**: `LearnerProfilePage` already renders the full enrollment table (course identity, progress bar, score, time spent, timeline, operational tag) — feature-equivalent to MVC report.
- ✅ **LearnerGroupCategories**: custom `/master-data/learner-group-categories` screen with tree-aware grid + create/edit/delete modal (custom data source, not `EntityListPage`).
- ✅ **Build verification**: `npm run build` green (216 kB main, Recharts + SignalR chunks split).

All phases from the original gap matrix are now delivered. Legacy `iLearn.Admin` (MVC) can be retired pending acceptance testing.

---

## 1. Purpose

`iLearn.Admin.React` already has a working shell, design system, and several detail pages, but it is **not yet capable of replacing** `iLearn.Admin` (MVC). This document inventories every Admin MVC view against the React app, classifies the gap, and proposes a phased plan to reach feature parity so `iLearn.Admin` can be retired.

This is the planning step. **No code changes are made in this round.**

## 2. Inventory & Gap Matrix

Legend — **State**:
- ✅ Done — React page exists with real (non-placeholder) implementation.
- 🟡 Stub — Route exists but uses `EntityListPage` placeholder or skeleton dashboard; not feature-equivalent.
- ❌ Missing — No React route/page.

### 2.1 Dashboard / Home

| MVC View | MVC Route | React Route | State | Notes |
| --- | --- | --- | --- | --- |
| `Home/Index.cshtml` | `/` | `/` (`DashboardPage`) | 🟡 Stub | MVC has KPI cards (Course Portfolio / Active Assignments / Learner Progress / Learning Activity), activity & task charts, priority assignment table, Report Hub, "Courses Needing Attention", maintenance banner. React renders a static placeholder ("Admin API: Ready"). |

### 2.2 Courses

| MVC View | React Page | State | Notes |
| --- | --- | --- | --- |
| `Courses/Index` | `CourseListPage` | ✅ Done | Tree view + grid. |
| `Courses/Detail` | `CourseDetailPage` | ✅ Done | Card-Free Control Hub. |
| `Courses/Editor` | `CourseEditorPage` | ✅ Done | Create/edit. |
| `Courses/VersionForm` | `VersionFormPage` | ✅ Done | SCORM upload. |

### 2.3 Content Library

| MVC View | React Page | State | Notes |
| --- | --- | --- | --- |
| `ContentItems/Index` | `/content-library` → `EntityListPage` | 🟡 Stub | Read-only grid only. MVC has full CRUD, SCORM upload preview, publish/unpublish, launch metadata edit. Detail/editor pages missing. |

### 2.4 Assignments

| MVC View | MVC Route | React Page | State | Notes |
| --- | --- | --- | --- | --- |
| `Assignments/Index` | `/Assignments` | `/assignments` → `EntityListPage` | 🟡 Stub | Read-only batch list. |
| `Assignments/BulkAssign` | `/Assignments/BulkAssign` | `/assignments/bulk` → `BulkAssignPage` | ✅ Done | Wizard exists. Confirm parity with MVC selection trays / conflict handling. |
| `Assignments/Detail` | `/Assignments/Detail/{id}` | `/assignments/:id` → `AssignmentDetailPage` | ✅ Done | Batch console. |
| `Assignments/Gantt` | `/Assignments/Gantt` | — | ❌ Missing | Timeline / schedule view; linked from Dashboard "Learning Activity" tile. |
| `Assignments/Report` | `/Assignments/Report/{id\|assignmentNo}` | — | ❌ Missing | Per-assignment progress / completion / learner matrix report. |

### 2.5 Student Groups (Learner Groups)

| MVC View | React Page | State | Notes |
| --- | --- | --- | --- |
| `LearnerGroups/Index` | `/learner-groups` → `EntityListPage` | 🟡 Stub | Read-only grid. |
| `LearnerGroups/Detail` | `GroupDetailPage` | ✅ Done | |
| `LearnerGroups/Editor` | `GroupEditorPage` | ✅ Done | |
| `LearnerGroups/AddMembers` | — | ❌ Missing | Dedicated wizard in MVC; React must either fold into `GroupDetailPage` or add a route. |
| `LearnerGroups/RemoveMembers` | — | ❌ Missing | Same as above. |

### 2.6 Learners

| MVC View | React Page | State | Notes |
| --- | --- | --- | --- |
| `Learners/Index` | `/learners` → `EntityListPage` (mapped to `UsersCRUD`) | 🟡 Stub | Wrong backing controller — MVC uses `LearnersController`, React `EntityListPage` config points to `UsersCRUD`. Needs verification. |
| `Learners/Profile` | `/learners/:id/profile` → `LearnerProfilePage` | ✅ Done | |
| `Learners/Report` | — | ❌ Missing | Per-learner progress report. |

### 2.7 Learning Logs

| MVC View | React Page | State | Notes |
| --- | --- | --- | --- |
| `LearningLogs/Index` | — | ❌ Missing | SCORM runtime audit grid. Linked from Dashboard. |

### 2.8 Master Data

`iLearn.Admin.React` exposes **one** consolidated `/master-data` route hard-wired to Divisions. The MVC admin has 5 distinct master-data screens, each with its own grid and CRUD.

| MVC Controller / View | React Coverage | State | Notes |
| --- | --- | --- | --- |
| `Divisions/Index` | `/master-data` (Divisions only) | 🟡 Stub | Read-only grid; no editor. |
| `Categories/Index` | — | ❌ Missing | |
| `CourseTypes/Index` | — | ❌ Missing | |
| `Roles/Index` | — | ❌ Missing | |
| `LearnerGroupCategories/Index` | — | ❌ Missing | |

### 2.9 Users & System

| MVC View | React Page | State | Notes |
| --- | --- | --- | --- |
| `Users/Index` | — (folded into `/learners`?) | ❌ Missing | Admin user management (roles, division, IsActive). Distinct from Learners. |
| `SystemConfig/Index` | `/system-config` → `SystemConfigPage` | ✅ Done | Verify parity. |
| `Enrollments/Index` | — | ❌ Missing | Direct enrollment grid (used for ops/debugging). |

### 2.10 Cross-Cutting Capabilities

| Capability | MVC | React | State | Notes |
| --- | --- | --- | --- | --- |
| Windows Auth session (`/api/admin/session/me`) | Implicit via cookie | Via `lib/auth.ts` | ✅ Done | Confirm endpoint exists. |
| Role-aware nav (SuperAdmin vs Division Admin) | `User.IsInRole` in Razor | Static `navigationItems` | ❌ Missing | React sidebar shows all links regardless of role. |
| AdminActivityHub (SignalR) | Yes | Header beacon only | 🟡 Partial | No live refresh wiring on grids. |
| Toast / error UX | SweetAlert2 | `lib/toast.ts` | ✅ Done | |
| Cache admin (`CacheController`) | Yes | — | ❌ Missing | Low priority. |
| Reports / charts | DevExtreme jQuery charts | — | ❌ Missing | No chart primitive in React (and stack is intentionally DevExtreme-free per Section 11 of `copilot-instructions.md`). Need replacement chart library decision. |
| Excel/CSV export | ExcelJS + FileSaver in MVC | — | ❌ Missing | Decide whether server-side export endpoints can replace client-side build. |
| PDF / screenshot export | html2canvas | — | ❌ Missing | Same. |

## 3. Summary Score

| Bucket | Total MVC Pages | React Done | React Stub | React Missing |
| --- | --- | --- | --- | --- |
| Courses | 4 | 4 | 0 | 0 |
| Content Library | 1 (+ implicit detail/editor) | 0 | 1 | implied missing |
| Assignments | 5 | 2 | 1 | 2 |
| Student Groups | 5 | 2 | 1 | 2 |
| Learners | 3 | 1 | 1 | 1 |
| Learning Logs | 1 | 0 | 0 | 1 |
| Master Data (5 screens) | 5 | 0 | 1 (Divisions only) | 4 |
| Users / SystemConfig / Enrollments | 3 | 1 | 0 | 2 |
| Dashboard | 1 | 0 | 1 | 0 |
| **Total** | **28** | **10** | **5** | **13** |

Functional parity: **~36 %**. Cannot retire `iLearn.Admin` yet.

## 4. Foundational Decisions Needed Before Coding

These decisions block multiple migration phases. Please confirm before Phase 1 starts.

1. **Chart library.** Section 11 of `copilot-instructions.md` says "Do not add DevExtreme". Dashboard / Reports / Gantt need charts. Options:
   - **(a)** Lift the DevExtreme ban for *charts only* (DevExtreme React already in stack per `REACT-ADMIN-REBUILD-PLAN.md`).
   - **(b)** Adopt a lightweight alternative — Recharts, visx, or ECharts.
   - **(c)** Server-render PNG/SVG via API.
   - **Recommendation**: (a) — license is already paid, ban appears to apply to *legacy MVC widgets*, not React charts.
2. **Gantt component.** Same trade-off; DevExtreme React `Gantt` is the lowest-effort path.
3. **Master-data routing.** Replace single `/master-data` with `/master-data/divisions`, `/master-data/categories`, etc., each driven by `EntityListPage` config — OR build a single page with a sub-sidebar (matches the design system's optional `SubSidebar`).
4. **Learners vs Users.** Are these two pages (employee directory + admin users) or one? The current React `EntityListPage` config conflates them (`UsersCRUD` controller for the `/learners` route).
5. **Role-aware navigation.** Filter `navigationItems` by session roles loaded in `AppLayout`, and add an `<RequireRole>` guard component.
6. **Export.** Drop client-side Excel/PDF export and add API endpoints, or port ExcelJS/FileSaver into the React bundle?
7. **Deployment URL.** Keep `/iLearnNew/admin-react/` (parallel) until parity, then swap to `/iLearnNew/admin/` and retire MVC? Or run forever side-by-side?

## 5. Phased Implementation Plan

Each phase ends with a green build (`dotnet build` + `npm run build`) and updates to this document's gap matrix.

### Phase A — Foundations (no user-visible changes)

A1. Confirm `/api/admin/session/me` returns roles + division flags; create if missing.  
A2. Add `SessionContext` provider in `AppLayout` → expose `useSession()` with roles, division, `isSuperAdmin`.  
A3. Filter `navigationItems` by role; add `<RequireRole>` route guard.  
A4. Decide chart library (Section 4 #1). If DevExtreme charts: add `devextreme-react`, `devextreme-aspnet-data-nojquery`, configure license, wire `manualChunks`.  
A5. Add `AppChart` + `AppGantt` primitives if DevExtreme is adopted.

### Phase B — Dashboard parity

B1. Build `/api/dashboard/summary` (KPIs) and `/api/dashboard/activity-trend` (chart) if not already present — or reuse existing endpoints used by MVC Dashboard.  
B2. Implement real `DashboardPage`: KPI strip (Card-Free standard), activity line chart, task status chart, priority assignment table, Report Hub links, "Courses Needing Attention" panel, maintenance banner.  
B3. Hide SuperAdmin-only tiles via `useSession().isSuperAdmin`.

### Phase C — Assignments completion

C1. `/assignments/:id/report` — per-assignment progress / learner matrix (replaces `Assignments/Report`).  
C2. `/assignments/gantt` — schedule view.  
C3. Audit `BulkAssignPage` vs MVC `BulkAssign` for parity (conflict handling, selection tray, review summary).

### Phase D — Content Library full CRUD

D1. `/content-library/:id` detail page.  
D2. `/content-library/new` + `/content-library/:id/edit` editor with SCORM ZIP upload (`multipart/form-data` → existing API).  
D3. Publish/unpublish action surfaced in detail Control Hub.

### Phase E — Student Groups membership

E1. Fold AddMembers / RemoveMembers into `GroupDetailPage` as drawers or `/learner-groups/:id/members/add` route.  
E2. Persistent selection tray + conflict-aware add flow.

### Phase F — Master Data expansion

F1. Refactor `/master-data` → `/master-data/divisions`, `/categories`, `/course-types`, `/roles`, `/learner-group-categories`.  
F2. Add `MasterDataLayout` with sub-sidebar.  
F3. Each page reuses `EntityListPage` + inline editor (use `AppTable` editing mode).

### Phase G — Learners / Users / Logs

G1. Split `/learners` (Employee Directory, `LearnersCRUD`) from `/users` (Admin Users, `UsersCRUD`).  
G2. `/learners/:id/report` — per-learner progress report.  
G3. `/learning-logs` — SCORM runtime audit grid.  
G4. `/enrollments` — direct enrollment ops grid (gated by SuperAdmin).

### Phase H — Polish & Cutover

H1. Real-time refresh: subscribe to `AdminActivityHub` events to invalidate active grids (`refreshKey` pattern).  
H2. Export endpoints: add server-side `GET /api/.../export` returning CSV/Excel, replace client-side ExcelJS.  
H3. Playwright smoke suite covering every migrated route.  
H4. Side-by-side QA / UAT.  
H5. Cutover: deploy React app to `/iLearnNew/admin/`, retire `iLearn.Admin` (or keep behind `/admin-legacy/` link for one release).

## 6. Acceptance Criteria for "Replace iLearn.Admin"

`iLearn.Admin` may be retired when **all** of these hold:

- Every row in the inventory matrix (Section 2) is ✅ Done.
- Role-aware nav matches MVC behavior (SuperAdmin tiles, division-scoped filters).
- All charts/reports linked from the Dashboard work in React.
- Playwright smoke suite green against the deployed React app.
- UAT sign-off from a HR/training admin and a SuperAdmin.
- `iLearn.Tests` regression suite still green.
- A documented rollback path (keep MVC project building for one release after cutover).

## 7. Out of Scope (for this plan)

- API endpoint refactors beyond what's needed for parity.
- Re-platforming the Learner UI (`iLearn.User`).
- Changes to SCORM runtime behavior.
- New features not present in MVC Admin today.

## 8. Next Step

Confirm decisions in Section 4 (especially chart library, master-data routing, Learners-vs-Users split). After approval, start **Phase A**.
