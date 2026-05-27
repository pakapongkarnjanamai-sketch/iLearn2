# Copilot Instructions — iLearn2

## 1. Project Overview & Business Context

**iLearn2** คือระบบ Internal e-Learning / LMS สำหรับองค์กร รองรับการจัดการหลักสูตร การมอบหมายการเรียน การติดตามความคืบหน้า และการเล่น SCORM runtime สำหรับ SCORM 1.2 และ SCORM 2004

ระบบนี้เป็น **enterprise internal training platform** ไม่ใช่ consumer learning app:

- ผู้ใช้งานหลักมีประมาณหลักร้อยถึงหลักพันภายในองค์กร
- ข้อมูลมีความสำคัญด้าน audit, compliance, assignment history, learner progress และ reporting
- Business rule สำคัญกว่าความสวยงามของ feature เฉพาะหน้า
- การแก้ไขต้องรักษาประวัติการเรียนและผลกระทบต่อผู้เรียนเดิมเสมอ

## 2. Solution Map

| Project | Responsibility |
| --- | --- |
| `iLearn.Domain` | Domain entities, enums, constants, shared domain policies, and domain-level state concepts. Must remain independent from presentation, EF Core, UI, and infrastructure details. |
| `iLearn.Application` | Use cases, DTOs, service interfaces, application services, validation rules, and contracts consumed by presentation layers. |
| `iLearn.Infrastructure` | EF Core SQL Server persistence, repository/unit-of-work implementations, storage/file handling, SCORM package processing support, caching, and external integration implementations. |
| `iLearn.API` | ASP.NET Core Web API presentation layer for Admin/User clients. Controllers should contain HTTP concerns only and delegate business rules to Application services. |
| `iLearn.Admin` | ASP.NET Core MVC Admin UI (legacy) for HR/training/admin workflows. Uses DevExtreme, Bootstrap, jQuery, and the Admin design system. Being replaced by `iLearn.Admin.React`. |
| `iLearn.Admin.React` | **React 19 + TypeScript SPA** — next-generation Admin UI. Vite 8, Tailwind CSS 4, lucide-react. Consumes `iLearn.API` endpoints via Windows Auth. See sections 9–17 for full details. |
| `iLearn.User` | ASP.NET Core MVC/Razor learner UI for assigned learning, course launch, progress display, and SCORM player flows. |
| `iLearn.Tests` | xUnit regression tests for lifecycle rules, services, controllers, policies, and critical bug fixes. |

The legacy `iLearn.Admin` (MVC + DevExtreme) and `iLearn.Admin.React` (React 19 SPA) coexist during migration. New admin features should target `iLearn.Admin.React`.

## 3. Architecture & Tech Stack

The project follows **Clean Architecture in a modular monolith style**: code is logically separated into projects/layers, but the system is deployed as a single unit. Do **not** describe it as microservices unless the repository is actually split into independently deployed services.

### Architecture Rules

- Dependency direction is `Domain -> Application -> Infrastructure/Presentation` conceptually, but source references must keep Domain independent.
- Domain must not reference EF Core, ASP.NET Core MVC/Web API, DevExtreme, file system APIs, or infrastructure services.
- Application can define interfaces/ports and DTO contracts, but should not depend on concrete infrastructure implementations.
- Infrastructure implements Application interfaces and owns EF Core, SQL Server, file storage, caching, and package-processing implementation details.
- Presentation projects (`iLearn.API`, `iLearn.Admin`, `iLearn.Admin.React`, `iLearn.User`) compose dependencies and handle HTTP/UI concerns.
- Controllers must not inject `AppDbContext` directly. Use Application services, repository abstractions, or `IUnitOfWork` according to existing patterns.
- Transactions belong in service/application workflows or `IUnitOfWork`, not ad-hoc controller database calls.

### Current Stack

- **Runtime:** .NET 9 / C# 13
- **Backend:** ASP.NET Core Web API (`iLearn.API`)
- **Admin UI (Legacy):** ASP.NET Core MVC + DevExtreme 25.2 + Bootstrap 5 + jQuery (`iLearn.Admin`)
- **Admin UI (Next-gen):** React 19 + TypeScript 6 + Vite 8 + Tailwind CSS 4 (`iLearn.Admin.React`)
- **Learner UI:** ASP.NET Core MVC/Razor + JavaScript SCORM player integration
- **Database:** SQL Server via EF Core 9
- **Authentication:** Windows Authentication / Negotiate with role and division claims
- **Testing:** xUnit in `iLearn.Tests`

## 4. Required Role For AI Assistance

When working in this repository, act as a **Senior Software Engineer and SCORM-aware LMS reviewer**. Your responsibility is not only to make code compile, but to verify:

1. The code structure follows Clean Architecture boundaries.
2. Business logic matches the lifecycle documents.
3. API/UI behavior preserves learner history, assignment history, and auditability.
4. Security, authorization, and division-based data isolation are enforced server-side.
5. The change is covered by focused validation or tests when it changes behavior.

If requirements are ambiguous or conflict with lifecycle rules, stop and ask clarifying questions instead of inventing behavior.

## 5. Source-Of-Truth Documents

Before implementing business behavior, read the relevant source-of-truth document:

| Area | Required Document |
| --- | --- |
| Lifecycle overview and cross-cutting recommendations | `DOC/LIFECYCLE-OVERVIEW.md` |
| Course status and course availability | `DOC/COURSE-LIFECYCLE-RULES.md` |
| Content item, SCORM package readiness, and course version activation | `DOC/CONTENT-LIFECYCLE-RULES.md` |
| Assignment batches, enrollments, reassignment conflicts, progress rollup | `DOC/ASSIGNMENT-ENROLLMENT-LIFECYCLE-RULES.md` |
| SCORM import, runtime commit, status normalization, player status | `DOC/SCORM-RUNTIME-LIFECYCLE-RULES.md` |
| Division, category, course type, role, user, learner group, file storage, audit | `DOC/MASTER-DATA-LIFECYCLE-RULES.md` |
| Canonical status names and aliases | `DOC/STATUS-DEFINITIONS.md` |
| Product terminology and naming rules | `DOC/SYSTEM-DICTIONARY.md` |
| CRUD, lookup, and audit conventions | `DOC/GENERIC-CRUD-LOOKUP-AUDIT.md` |
| Contributor architecture/security rules | `CONTRIBUTING.md` |

## 6. Core Business Rules

### Course Management

- Course lifecycle uses `Draft`, `Open`, `Closed`, and `Retired`.
- Only `Open` courses can be assigned to learners.
- `Closed` stops new assignments but allows existing assigned learners to continue when their enrolled version remains ready.
- `Retired` blocks active learner launch access but must preserve reports, logs, enrollments, and learning history.
- `Course.IsActive` is legacy compatibility. New business decisions should use `Course.Status` and derived capability properties such as `CanAssign` and `CanLearnerAccess`.
- Opening a course requires a ready active course version.

### Content And Course Version Management

- Content readiness is derived; it is not only `IsActive`.
- A ready content item must be published and have valid launch metadata.
- SCORM packages must validate ZIP safety, manifest discovery, SCORM version, and launch resource before becoming launchable.
- A course version must contain at least one ready content item before it can safely become active/open.
- Activating a version must not silently invalidate learner history or move learners unless the selected learner version policy explicitly allows it.

### Assignment And Enrollment

- Assignment batches are represented by rows sharing an `AssignmentNo`.
- Assignment status is computed, not manually edited: `Completed`, `Upcoming`, `Expired`, then `InProgress` by priority.
- New assignments require Open courses, ready active versions, valid learners/groups, valid date ranges, and transaction-safe enrollment creation.
- Existing in-progress or completed enrollments require explicit conflict handling before reset/reassign.
- Reassignment must preserve completed history through `EnrollmentAssignment` snapshot fields.
- Enrollment progress is rolled up from content item learning logs/runtime state for the enrolled version.
- Logs before `Enrollment.ResetAt` are ignored for active progress.

### SCORM Runtime

- Support SCORM 1.2 and SCORM 2004 data model differences explicitly.
- Runtime commits must validate learner identity, enrollment ownership, and content-item membership in the enrolled course version.
- `ScormContentStatusPolicy` is the shared owner for Learn vs Exam completion behavior.
- Exam content with `completed` but without `passed` remains incomplete for player display and enrollment rollup.
- Preserve terminal runtime state: placeholder incomplete/not attempted commits must not overwrite meaningful passed/completed/failed data.
- Persist diagnostic CMI snapshots carefully; do not expose sensitive or unnecessary raw runtime payloads to player responses.

### Master Data, Security, And Audit

- Generic `IsActive`/`IsDeleted` flags are acceptable for master data but must not replace richer business statuses.
- Soft-deleted records may still be required for history, reports, and audit.
- `SuperAdmin` can access all divisions; normal admins must be division-scoped.
- Authorization must be enforced in controllers/endpoints, not only by hiding UI elements.
- Do not commit live secrets. Use user-secrets or environment variables for connection strings and shared secrets.

## 7. Implementation Rules For AI

1. **Investigate first.** Read existing services, DTOs, controllers, views, tests, and lifecycle docs before changing behavior.
2. **Make the smallest complete change.** Fix the requested problem fully without unrelated refactors.
3. **Preserve architecture boundaries.** Do not move infrastructure concerns into Domain/Application or add controller-level business logic when a service/policy should own it.
4. **Use existing patterns.** Follow current naming, DTO, service, EF Core, DevExtreme, and Razor patterns instead of inventing parallel abstractions.
5. **No placeholders for core logic.** Do not leave TODO comments, stubbed branches, fake data, or partial lifecycle implementations.
6. **Prefer explicit business names.** Use canonical terms from `SYSTEM-DICTIONARY.md` and statuses from `STATUS-DEFINITIONS.md`.
7. **Validate behavior.** Run the narrowest relevant tests/builds after behavior changes; add or update tests for lifecycle, authorization, or SCORM rule changes.
8. **Review security impact.** Check authorization, data isolation, file upload safety, path traversal, secrets, and over-broad data exposure.
9. **Keep UI and API contracts aligned.** If a DTO/status changes, update Admin/User rendering, shared helpers, and tests together.
10. **Ask when uncertain.** Especially for retired-course policy, completed learner retention, reassignment/reset semantics, and destructive content operations.

## 8. Review Checklist For Changes

Before finishing a task, confirm:

- The relevant lifecycle document was considered.
- Domain/Application/Infrastructure/Presentation boundaries were not weakened.
- Learner progress, assignment snapshots, audit history, and reports remain consistent.
- Authorization and division isolation still apply server-side.
- Status names and user-facing terminology match the dictionary/status docs.
- Existing tests pass or any known unrelated failures are documented.
- No secrets, generated artifacts, or unrelated formatting churn were introduced.

## DevExpress MCP Server: Configure Your AI-powered Coding Assistant

---
description: 'Answer questions about DevExpress UI Components and their API using the dxdocs server'
---

You are a .NET/JavaScript programmer and DevExpress product expert.

Your task is to answer questions about DevExpress components and their APIs using dxdocs MCP server tools.

When replying to **ANY** question about DevExpress components, use the dxdocs server to construct your answer.

## Workflow:

1. **Call devexpress_docs_search** to obtain help topics related to the user's question
2. **Call devexpress_docs_get_content** to fetch and read the most relevant help topics
3. **Reflect on the obtained content** and how it relates to the question
4. **Provide a comprehensive answer** based solely on retrieved information

## Constraints:

- **Use devexpress_docs_search only once** per question to avoid redundant queries
- **Answer questions based solely** on information obtained from MCP server tools
- If relevant code examples are available in documentation, **include those code examples**
- **Reference specific DevExpress controls and properties** mentioned in the docs
- If a user specifies a version (such as v24.2 or 24.2), invoke MCP server tools corresponding to that version (for example, "dxdocs24_2")


# iLearn — Design Context

> This file provides design context for both iLearn.Admin and iLearn.User. Scope-specific sections are marked.

---

## Design Context

### Users

The product serves two distinct audiences.

**Admin UI** users are HR and training managers, plus division-level administrators, working primarily on desktop during office hours. Their main jobs are managing course catalogs, assigning learning, monitoring progress, and making fast, confident decisions from large datasets. Sessions are task-driven: open the tool, find the data, make a decision, move on. They care about speed and correctness, not exploration.

**Learner UI** users are employees and staff accessing assigned learning content. Their main jobs are finding required courses quickly, understanding progress clearly, and completing learning tasks with minimal friction.

### Brand Personality

**Admin UI voice**: Structured, decisive, trustworthy.

Direct and concise — short labels, terse helper text, no motivational filler. The interface is a tool, not a companion. Copy should tell the admin exactly what happened and what to do next, then get out of the way.

**Emotional target (Admin)**: When an admin finishes a task, they should feel **confident** — "I trust this data and the decision I just made."

**Learner UI voice**: Accessible, encouraging, clear.

The Learner experience should feel welcoming, approachable, and motivating, with lower cognitive load and stronger mobile friendliness.

### Aesthetic Direction

**Admin UI** follows a minimal, flat, data-dense, utilitarian visual system based on brand blue `#0050b3`. Inspired by the clarity and restraint of **Google Admin Console** — clean surfaces, predictable layouts, dense grids that scale. Not flashy, not decorative, just fast and legible. Light theme: white and near-white surfaces (#fff, #fafafa, #f4f8ff) with brand blue as the single dominant accent. The palette is deliberately narrow: one hue for primary actions, semantic colors for status only.

**Typography (Admin)**: Currently Segoe UI (system stack). Open to a custom web font that reinforces brand identity without hurting load time. Any replacement should be highly legible at 12–14px (grid/table scale), support tabular numerals (`tnum`, `lnum`), feel neutral enough for dense data views but distinctive enough to avoid generic, and be available via Google Fonts or self-hosted.

**Learner UI** follows a soft, human-friendly visual system based on brand teal `#027d83`. It should feel calm, readable, and supportive, using rounded shapes, gentle contrast, and mobile-friendly interaction patterns.

**Anti-references** (what the Admin UI must NOT look like):
- Overly colorful or playful dashboards (Notion-style, consumer SaaS)
- Dark-mode developer/tech tools (terminal aesthetics, neon accents)
- Heavily animated SaaS products (transitions for everything, bouncing elements)
- Generic Bootstrap admin templates (AdminLTE, CoreUI out-of-the-box)

**Accessibility**: WCAG AA minimum, with attention to keyboard navigation, high readability, clear contrast, and reduced-motion friendliness.

### Design Principles

1. **Structure over decoration.** Every element earns its space through function. No ornamental shadows, no decorative gradients, no accent stripes. If it doesn't communicate data or state, remove it.
2. **Grids are the interface.** The primary interaction surface is the DevExtreme DataGrid. Optimize for scan speed: tight rows (34px), small type (13px), tabular numerals, sticky headers. Everything else supports getting to and from the grid.
3. **Predictable patterns, zero relearning.** Wizard flows, filter+grid layouts, page headers, and detail views must look and behave identically across every module. A user who learns Courses should already know Assignments.
4. **One accent, used sparingly.** Brand blue (#0050b3) is the only accent color. It marks the single most important action or state on any screen. Semantic colors (success, warning, danger) appear only for status. If everything is blue, nothing is.
5. **Desktop density, not mobile adaptation.** Optimize for 1440px+ viewports. The layout may degrade gracefully on tablets but is not designed for phones. Density is a feature, not a problem.
6. **Minimal motion.** No non-essential animation. Transitions (0.2s ease) only for state changes (active/complete steps, hover feedback). Respect `prefers-reduced-motion`.
7. **Accessibility as baseline.** WCAG AA is the quality bar, not a follow-up task. High contrast, keyboard navigation, focus rings, semantic markup.

### Current UX Priorities (April 2026)

- **Primary redesign scope**: `Assignments/BulkAssign` (Course Selection + Learner Selection), `StudentGroups/AddMembers`, and `StudentGroups/Editor` (Initial Members).
- **Critical pain point #1**: Users lose confidence about what is currently selected when moving across paged DataGrid results.
- **Critical pain point #2**: Cascading filters are not clear enough in behavior and state, especially after value changes and resets.
- **Selection policy**: Keep `allPages` selection behavior, but always expose a visible, immediately scannable list of selected items.
- **Data scale target**: Typical usage is mid-size datasets (~500 to 5,000 rows), so configuration should balance density, scan speed, and responsiveness.
- **Layout success criteria**: Keep key task context within one screen whenever possible and avoid stacked/nested scroll areas that force users to track state in multiple places.
- **Outcome metric**: Users should not lose track of current selections during assignment workflows.

---

## Existing Design System (Admin)

### Tokens (`admin-tokens.css`)
- **Colors**: 45 primitive + semantic tokens. Brand blue (#0050b3), 9-step neutral scale (#fafafa → #262626), semantic status palette (success #52c41a, warning #faad14, danger #ff4d4f, purple #722ed1).
- **Typography**: 5-step size scale (caption 12px → display 28px), 3 weights (500/600/700), Segoe UI system stack. Tabular numerals via `font-feature-settings: "tnum" 1, "lnum" 1`.
- **Spacing**: 12-step 4px-base scale (4px → 96px), semantic names (`--space-xs` through `--space-6xl`).
- **Radius**: 4 sizes (3–8px) + pill (999px).
- **Shadows**: Functional only — dropdown (4px 12px) and overlay (2px 8px), no decorative shadows.

### Component Patterns (`admin-minimal.css`, `admin-wizard.css`)
- **Page shell**: lg 1400px, md 960px. Page header with title + subtitle + actions.
- **Filter+grid layout**: 240px filter sidebar | 1fr grid. Responsive to single column at 992px.
- **Wizard step cards**: Active (blue border, info-subtle bg) / complete (green border, success-subtle bg) states.
- **Grid presets**: default / compact / selection via `buildAdminGridOptions()`. 34px row height, 38px header.
- **Status pills**: Semantic color mapping with uppercase micro-labels.
- **Icon+background pattern**: `.qa-icon-*` classes (accent, warning, success, danger, purple, muted).
- **Summary cards**: Review-step stats with large value + uppercase label.
- **Uppercase micro-labels**: Caption size, 600 weight, 0.4px letter-spacing — used for section titles, column headers, filter labels.
- **Empty states**: Centered, large icon, secondary text.

### Libraries
- DevExtreme 25.2 (light theme), Bootstrap 5.3.8, jQuery, Font Awesome 7.0.1, SweetAlert2.
- Export: ExcelJS 4.4, FileSaver 2.0, html2canvas 1.4.

---

## Wizard Page Pattern (Admin)

All multi-step wizard pages (`Assignments/BulkAssign`, `StudentGroups/AddMembers`, `StudentGroups/RemoveMembers`, `StudentGroups/Editor`, etc.) **must** use the shared classes from `admin-wizard.css`. Do **not** re-implement these as inline `<style>` blocks or with Bootstrap utility classes.

### Page Skeleton
```html
@section WizardStyles {
    <link href="~/css/admin-wizard.css" rel="stylesheet" asp-append-version="true" />
}

<div class="page-header admin-page-header--wizard d-flex flex-wrap gap-3 mb-0">
    <h1 class="page-title">…</h1>
    <div class="admin-wizard-steps admin-wizard-steps--inline" role="group" aria-label="Progress steps">
        <div class="admin-step-card active" data-step-card="1" aria-current="step">
            <div class="admin-step-title"><span class="admin-step-index">1</span>Step Name</div>
        </div>
        …
    </div>
    <div class="admin-page-header-actions">
        <a href="…" class="action-link">Cancel</a>
        <button type="button" id="btn-prev-step" class="action-link d-none">Previous</button>
        <button type="button" id="btn-next-step" class="action-link primary">Continue</button>
        <!-- Final-step action: .action-link.primary OR .action-link.danger -->
    </div>
</div>

<div class="container-fluid px-4 py-3 d-flex flex-column gap-3 admin-wizard-shell admin-wizard-shell--fill admin-review-flow admin-viewport-layout">
    <div id="load-panel"></div>
    <div class="admin-review-main-col admin-responsive-content">
        <div class="border rounded bg-white p-3 admin-viewport-fill" id="wizard-main-card">
            <div id="step-panel-1" class="admin-wizard-panel admin-wizard-panel--selection-fill active" data-step-layout="wide">…</div>
            <div id="step-panel-2" class="admin-wizard-panel" data-step-layout="form">…</div>
            <div id="step-panel-3" class="admin-wizard-panel" data-step-layout="review">…</div>
        </div>
    </div>
</div>
```

### Selection Step (Filter + Grid + Tray)
```html
<div class="admin-selection-layout">
    <div class="admin-filter-shell d-flex flex-column gap-3">
        <h6 class="admin-filter-section-title"><i class="fas fa-filter" aria-hidden="true"></i>Filters</h6>
        <div>
            <label class="admin-filter-label">Division</label>
            <div id="filter-div"></div>
        </div>
        …
        <button type="button" id="btn-clear-filter" class="admin-filter-clear-btn">
            <i class="fas fa-times me-1" aria-hidden="true"></i> Clear Filters
        </button>
    </div>
    <div class="admin-grid-shell">
        <div class="admin-grid-head">
            <div class="admin-grid-title">Directory Title</div>
            <span class="tag-pill pill-default" id="x-selection-note">0 item</span>
        </div>
        <div id="grid-x" class="admin-grid-fill"></div>
    </div>
</div>
<div class="admin-selection-tray">
    <div class="admin-selection-tray-label">Selected</div>
    <div class="admin-selection-tray-chips" id="x-chips"></div>
    <button type="button" class="admin-selection-tray-clear is-disabled" id="x-clear">
        <i class="fas fa-eraser me-1" aria-hidden="true"></i>Clear
    </button>
</div>
```

### Options Step — Status Option Cards
```html
<div class="admin-status-options">
    <div class="admin-status-option" data-status-option="InProgress">
        <label for="status-inprogress">
            <div>
                <div class="admin-status-option-title">In Progress</div>
                <div class="admin-status-option-note">Helper text.</div>
            </div>
            <input class="form-check-input" type="checkbox" id="status-inprogress" value="InProgress">
        </label>
    </div>
    …
</div>
```

For an embedded picker grid inside an option card, wrap it in `.admin-assignment-grid-shell`.

### Review Step — Scrollable Tables
```html
<div class="admin-table-shell">
    <div class="admin-table-head">
        <div class="admin-table-title">Section Title</div>
        <span class="tag-pill pill-default"></span>
    </div>
    <div class="admin-table-wrap admin-review-table-wrap" id="x-summary-table"></div>
</div>
```

### Standard Class Vocabulary (do not invent alternatives)
| Concern | Class |
| --- | --- |
| Page header (wizard) | `.page-header.admin-page-header--wizard` |
| Inline step cards | `.admin-wizard-steps.admin-wizard-steps--inline` + `.admin-step-card` |
| Header actions row | `.admin-page-header-actions` |
| Action button | `.action-link` (`.primary` / `.danger` modifiers) |
| Wizard shell | `.admin-wizard-shell.admin-wizard-shell--fill.admin-review-flow.admin-viewport-layout` |
| Selection panel | `.admin-wizard-panel.admin-wizard-panel--selection-fill` (`data-step-layout="wide"`) |
| Form/options panel | `.admin-wizard-panel` (`data-step-layout="form"`) — wider canvas via `.admin-review-flow.is-form-step` (auto-applied) |
| Review panel | `.admin-wizard-panel` (`data-step-layout="review"`) |
| Filter sidebar | `.admin-filter-shell` |
| Filter heading | `.admin-filter-section-title` |
| Filter field label | `.admin-filter-label` |
| Filter clear button | `.admin-filter-clear-btn` |
| Grid card | `.admin-grid-shell` + `.admin-grid-head` + `.admin-grid-title` + `.admin-grid-fill` |
| Persistent selection tray | `.admin-selection-tray` + `.admin-selection-tray-label` + `.admin-selection-tray-chips` + `.admin-selection-tray-clear` |
| Selection chip | `.admin-selection-chip` (+ `.is-overflow`) |
| Option card | `.admin-option-card` (+ `.is-disabled`) |
| Status option grid | `.admin-status-options` |
| Status option card | `.admin-status-option` (+ `.disabled`) |
| Status option title / note | `.admin-status-option-title` / `.admin-status-option-note` |
| Embedded picker grid | `.admin-assignment-grid-shell` |
| Review table card | `.admin-table-shell` + `.admin-table-head` + `.admin-table-title` |
| Scrollable review table body | `.admin-table-wrap.admin-review-table-wrap` |
| Summary card grid | `.admin-summary-grid` (cols via `.admin-summary-grid--cols-2/3/4/5` modifier) |
| Form card (single-column form sizing) | `.admin-form-card` (size via `.admin-form-card--sm/md/lg` modifier) |

### Wizard Authoring Rules
1. **No per-page `<style>` block for filter, status, review-table, or option styling.** Use the shared classes above. Page-only styles should be limited to genuinely unique concerns and namespaced.
2. **Do not use Bootstrap utility classes for filter chrome** (`small fw-bold text-muted`, `btn btn-sm btn-outline-secondary`, `<h6 class="fw-bold">`). Use the `.admin-filter-*` classes.
3. **Buttons in the wizard header always use `.action-link`** (with `.primary` or `.danger`), never raw Bootstrap `.btn` variants.
4. **Selection step always exposes a persistent `.admin-selection-tray`** so users never lose track of selections across paged grids (per UX priority).
5. **Step cards, panels, and `data-step-layout` values must match the skeleton above** so `toggleAdminViewportLayout()` and `is-selection-step` / `is-form-step` rules apply correctly.
6. **Final-step action button class** mirrors the action's destructiveness: `primary` for create/assign/save, `danger` for remove/delete.
7. **Section titles inside panels** use `.admin-section-header` + `.section-title.admin-section-title-fill` + `.admin-section-divider` — not ad-hoc headings.
8. **No inline `style="..."` attributes.** Use shared classes / modifiers (`.admin-summary-grid--cols-4`, `.admin-form-card--md`, etc.). Inline `style` is allowed only for runtime-computed values inside JS template strings (e.g. dynamic widths in progress bars).

---

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.

---

# iLearn.Admin.React — React Admin Portal

## 9. Project Overview

`iLearn.Admin.React` is the **next-generation Admin UI** built as a standalone React 19 SPA that replaces the legacy ASP.NET Core MVC + DevExtreme admin interface (`iLearn.Admin`). It consumes the existing `iLearn.API` backend via REST endpoints using Windows Authentication (Negotiate).

This is a **parallel deployment** — both old MVC admin and new React admin exist in the repository. The React portal is self-contained under `iLearn.Admin.React/` and does not share code with the MVC project.

## 10. Solution Map Update

| Project | Responsibility |
| --- | --- |
| `iLearn.Admin.React` | React 19 + TypeScript SPA admin portal. Vite bundler, Tailwind CSS 4, lucide-react icons. Consumes `iLearn.API` endpoints. Deployed as static files served by the API host. |

## 11. Tech Stack

| Concern | Technology |
| --- | --- |
| **Framework** | React 19 with TypeScript 6 |
| **Build** | Vite 8 (`npm run build` → `tsc -b && vite build`) |
| **Styling** | Tailwind CSS 4 (via `@tailwindcss/vite` plugin) + vanilla CSS design tokens in `src/index.css` |
| **Routing** | React Router DOM 7 (client-side, `<Routes>` in `App.tsx`) |
| **Icons** | lucide-react (tree-shakable SVG icons) |
| **Real-time** | @microsoft/signalr 10 (connection status beacon in header) |
| **Auth** | Windows Authentication / Negotiate (inherited from `iLearn.API` proxy) |
| **Fonts** | Google Fonts — **Inter** (body/tables/forms at 13px) + **Outfit** (display/headings) |

### Key Dependencies (from `package.json`)

```
react 19, react-dom 19, react-router-dom 7
tailwindcss 4, @tailwindcss/vite 4
lucide-react 1.16, @microsoft/signalr 10
typescript 6, vite 8, @vitejs/plugin-react 6
```

**Do not** add DevExtreme, Bootstrap, jQuery, or any legacy MVC dependencies to this project. This portal is intentionally DevExtreme-free.

## 12. File Structure

```
iLearn.Admin.React/
├── src/
│   ├── main.tsx                    # Entry point (BrowserRouter + App)
│   ├── App.tsx                     # Route definitions
│   ├── index.css                   # Global CSS: design tokens, layout, component styles
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AppLayout.tsx       # Shell: sidebar + header + <Outlet>
│   │   │   ├── Sidebar.tsx         # Dark slate navigation sidebar
│   │   │   ├── Header.tsx          # Top bar with breadcrumbs + auth status
│   │   │   └── Breadcrumbs.tsx     # Auto-generated breadcrumb trail
│   │   └── ui/
│   │       ├── AppButton.tsx       # Standard button (primary/secondary/danger/ghost)
│   │       ├── AppTable.tsx        # Bespoke data grid with paging, sorting, search, editing
│   │       ├── AppTreeView.tsx     # Recursive tree view for hierarchical data
│   │       ├── PageHeader.tsx      # Page-level header actions strip
│   │       ├── SelectionTray.tsx   # Chip-based multi-selection display
│   │       ├── StatusText.tsx      # Semantic status badge renderer
│   │       ├── DataGridSurface.tsx # Grid wrapper surface
│   │       ├── SidePanel.tsx       # Slide-out side panel
│   │       └── Toolbar.tsx         # Action toolbar strip
│   ├── lib/
│   │   ├── apiClient.ts            # fetchWithAccessControl() — API client with auth
│   │   ├── auth.ts                 # Windows Auth user context
│   │   ├── createDataSource.ts     # CRUD data source factory for AppTable
│   │   ├── format.ts               # Date/number formatting utilities
│   │   └── toast.ts                # Lightweight DOM-based toast notification system
│   ├── pages/
│   │   ├── DashboardPage.tsx       # Landing dashboard
│   │   ├── EntityListPage.tsx      # Generic list page driven by moduleConfigs
│   │   ├── moduleConfigs.ts        # Entity list configuration registry
│   │   ├── courses/
│   │   │   ├── CourseListPage.tsx   # Course catalog with tree view + grid
│   │   │   ├── CourseDetailPage.tsx # Course dashboard with Control Hub
│   │   │   ├── CourseEditorPage.tsx # Create/edit course form
│   │   │   └── VersionFormPage.tsx  # SCORM version upload form
│   │   ├── assignments/
│   │   │   ├── AssignmentDetailPage.tsx  # Assignment batch console
│   │   │   └── BulkAssignPage.tsx       # Multi-step bulk assignment wizard
│   │   ├── student-groups/
│   │   │   ├── StudentGroupDetailPage.tsx  # Student Group membership management
│   │   │   └── StudentGroupEditorPage.tsx  # Create/edit Student Group form
│   │   ├── learners/
│   │   │   └── LearnerProfilePage.tsx  # Learner profile view
│   │   └── system-config/
│   │       └── SystemConfigPage.tsx    # System configuration panel
│   └── config/                     # App configuration
```

## 13. Design System (React Admin)

### Visual Identity

The React admin uses a **card-free, flat, high-density** aesthetic. This is fundamentally different from the MVC admin's card-based Bootstrap approach.

| Aspect | Specification |
| --- | --- |
| **Font** | Inter 13px base, Outfit for display elements |
| **Brand color** | Indigo `#4f46e5` (CSS var `--admin-brand`) |
| **Surfaces** | Transparent/flat — no `bg-white border rounded shadow` cards on Detail pages |
| **KPI display** | Unified inline strip with vertical dividers (`border-r border-slate-200/60`) |
| **Metadata** | Flat `<dl>` grids with thin bottom dividers (`border-b border-slate-100/50`) |
| **Tables** | Rendered directly on page background, no card wrappers |
| **Sidebar panels** | Thin left-accent border (`border-l-2`) instead of boxed cards |
| **Status indicators** | Semantic pulsing beacons with glow effects |

### CSS Architecture

All design tokens and component styles live in `src/index.css`:

- **CSS Custom Properties** (`--admin-*`): Brand, surface, border, text, status colors
- **Tailwind CSS 4**: Utility-first classes for layout (imported via `@import "tailwindcss"`)
- **Component classes** (`.admin-button`, `.admin-app-shell`, `.admin-sidebar`, etc.): Vanilla CSS for reusable component patterns

### Typography Rules

- Global font: `Inter` at exactly `13px` (enforced via `* { font-size: 13px !important }`)
- **Do not** use `tracking-wider` or `tracking-widest` — these are banned from the codebase
- Labels use `text-xxs font-extrabold text-slate-400 uppercase`
- Section headers use `text-sm font-extrabold text-slate-700`
- Values use `text-slate-800 font-bold`

### Button Standards

All buttons follow a consistent dimensional system:

| Category | Spec |
| --- | --- |
| `AppButton` (`.admin-button`) | `min-height: 34px`, `padding: 0 12px`, `border-radius: 6px` |
| Control Hub — Core Actions | `p-3`, `rounded-lg`, full-width with icon + label + description |
| Control Hub — State Transitions | `p-2.5`, `rounded-md`, full-width with icon + label + badge |
| Inline Actions (Cancel, Commit) | `py-2`, `text-xs font-bold`, `rounded`, 50/50 split width |

### Icon Usage

- All icons from `lucide-react` — tree-shakable, no icon fonts
- Standard size: `h-4 w-4` for inline, `h-4.5 w-4.5` for section headers
- **Always** prune unused icon imports — TypeScript strict mode will error on them

## 14. Page Type Standards (React Admin)

### Detail Pages

Detail pages (`CourseDetailPage`, `StudentGroupDetailPage`, `AssignmentDetailPage`) follow the **Card-Free Details Standard**:

1. **No `bg-white` card wrappers** — content renders on the transparent page background
2. **KPI row** — Single horizontal strip with metrics separated by vertical dividers
3. **Metadata** — Flat `<dl>` descriptive lists with `border-b` between items
4. **Tables** — Sit directly on page canvas, no surrounding card container
5. **Sidebar panels** — Use thin `border-l-2` accent borders, not boxed cards
6. **Section headers** — `border-b border-slate-200/60 pb-3` underline, not card headers
7. **No PageHeader actions** — All actions consolidated into sidebar control panels (e.g. Course Control Hub)

### List Pages

List pages use `EntityListPage` driven by `moduleConfigs.ts` for generic CRUD grids, or custom list pages like `CourseListPage` with tree view + grid combination.

### Editor Pages

Editor pages (`CourseEditorPage`, `StudentGroupEditorPage`, `VersionFormPage`) use standard form layouts with:
- Labels: `text-xs font-bold text-slate-500 uppercase`
- Inputs: `border border-slate-200 rounded text-sm focus:outline-none focus:border-blue-600`

### Wizard Pages

`BulkAssignPage` implements multi-step wizard flows with step indicators, form panels, and review steps using the `SelectionTray` component for persistent selection visibility.

## 15. API Client Pattern

All API calls use `fetchWithAccessControl()` from `src/lib/apiClient.ts`:

```typescript
const resp = await fetchWithAccessControl<{ success: boolean; data: T }>(`Endpoint/${id}`)
if (resp.success) {
  // handle data
}
```

- Automatically includes Windows Auth credentials
- Base URL configured via `.env.local` (`VITE_API_BASE_URL`)
- All mutations use explicit `method`, `headers`, and `body` parameters
- Error handling via try/catch with `toast.error()` notifications

## 16. Implementation Rules For AI (React Admin)

1. **No DevExtreme.** Do not import or reference DevExtreme components. Use `AppTable`, `AppTreeView`, and native HTML elements.
2. **No Bootstrap.** Do not use Bootstrap utility classes. Use Tailwind CSS 4 utilities.
3. **No jQuery.** Use React state and hooks for all interactivity.
4. **No tracking utilities.** Do not use `tracking-wider` or `tracking-widest` in any Tailwind classes.
5. **No card wrappers on Detail pages.** Do not add `bg-white border rounded shadow` containers. Use flat, transparent layouts.
6. **Prune unused imports.** TypeScript strict mode errors on unused imports. Always clean up after refactoring.
7. **Use existing components.** Prefer `AppButton`, `AppTable`, `PageHeader`, `SelectionTray`, `StatusText` over custom implementations.
8. **Consistent font classes.** Labels are `text-xxs font-extrabold text-slate-400 uppercase`. Section headers are `text-sm font-extrabold text-slate-700`.
9. **API calls via `fetchWithAccessControl()`.** Do not use raw `fetch()` or axios.
10. **Toast notifications via `toast` from `src/lib/toast.ts`.** Do not use `alert()` or third-party toast libraries.
11. **Verify builds.** Run `npm run build` after changes to confirm zero TypeScript errors.

## 17. Route Registry

| Route | Page Component | Purpose |
| --- | --- | --- |
| `/` | `DashboardPage` | Landing dashboard |
| `/courses` | `CourseListPage` | Course catalog (tree + grid) |
| `/courses/new` | `CourseEditorPage` | Create new course |
| `/courses/:id` | `CourseDetailPage` | Course dashboard + Control Hub |
| `/courses/:id/edit` | `CourseEditorPage` | Edit course properties |
| `/courses/:courseId/version/new` | `VersionFormPage` | Upload SCORM version |
| `/courses/:courseId/version/:id/edit` | `VersionFormPage` | Edit SCORM version |
| `/content-library` | `EntityListPage` | Content items grid |
| `/assignments` | `EntityListPage` | Assignment batches grid |
| `/assignments/:id` | `AssignmentDetailPage` | Assignment batch console |
| `/assignments/bulk` | `BulkAssignPage` | Multi-step bulk assignment |
| `/student-groups` | `EntityListPage` | Student Groups grid |
| `/student-groups/:id` | `StudentGroupDetailPage` | Student Group membership management |
| `/student-groups/new` | `StudentGroupEditorPage` | Create new Student Group |
| `/student-groups/:id/edit` | `StudentGroupEditorPage` | Edit Student Group properties |
| `/learners` | `EntityListPage` | Learner directory grid |
| `/learners/:id/profile` | `LearnerProfilePage` | Learner profile view |
| `/master-data` | `EntityListPage` | Master data grid |
| `/system-config` | `SystemConfigPage` | System configuration |

