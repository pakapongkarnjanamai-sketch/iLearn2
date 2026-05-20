# React Admin Rebuild Plan

Last updated: 2026-05-20

## Purpose

This document evaluates replacing the current `iLearn.Admin` MVC/Razor + jQuery frontend with a new React-based Admin SPA built with Vite, TypeScript, Tailwind CSS, and DevExtreme React.

The goal is to reduce UI maintenance cost, make complex workflows easier to evolve, and keep the existing API, LMS lifecycle rules, auditability, Windows Authentication, and division-based authorization intact.

## Inputs Reviewed

- Repository architecture and lifecycle documents, especially `DOC/LIFECYCLE-OVERVIEW.md` and `CONTRIBUTING.md`.
- Current Admin host: `iLearn.Admin` MVC, Razor views, jQuery, DevExtreme MVC/jQuery widgets.
- Current API host: `iLearn.API`, Windows Authentication, explicit authorization policies, DevExtreme `DataSourceLoadOptions` endpoints.
- Skills:
  - `frontend-react-admin-shell-architecture`
  - `frontend-devextreme-react-grids-charts`
  - `frontend-devextreme-react-form-patterns`
  - `frontend-react-windows-auth`
  - `backend-aspnet-clean-architecture-net9`
  - `backend-devextreme-crud-generic-controller`
  - `backend-aspnet-windows-auth-domain-nid`
- MCP configuration in `.vscode/mcp.json`:
  - `dxdocs25_2` for DevExtreme 25.2 documentation.
  - `playwright` for browser smoke tests.
  - `github` for GitHub workflows when needed.
- DevExpress MCP docs for DevExtreme React 25.2 DataGrid remote data, CustomStore, and large dataset performance.

## Current Frontend Assessment

### Strengths

- Business APIs already exist for most Admin workflows.
- Many endpoints already accept DevExtreme `DataSourceLoadOptions` and use `DataSourceLoader`.
- Windows Authentication and role/division claims are already implemented in API and Admin middleware.
- Existing Admin CSS has useful enterprise-console tokens and design conventions.
- Current pages already model important workflows: courses, content library, assignments, learner groups, reports, and master data.

### Pain Points

- Razor pages contain large inline scripts with page-level mutable globals, jQuery selectors, and imperative DevExtreme widget setup.
- Complex pages such as Courses Editor and Bulk Assignment are difficult to refactor safely because markup, state, API calls, and widget lifecycle are interleaved.
- Shared UI behavior exists, but many views still depend on page-local wiring and fragile DOM ids.
- Debugging production issues often requires inspecting rendered inline script instead of typed source modules.
- Testability is limited: most behavior is not unit-testable without browser-level tests.
- Deploying MVC views and static assets together can leave stale UI behavior if the side-by-side deploy does not fully switch or sync assets.

## Recommendation

Build a new side-by-side React Admin SPA first, then migrate workflows incrementally. Do not replace `iLearn.Admin` in one step.

Recommended project path:

```text
iLearn.Admin.React/
```

Recommended first production-like route:

```text
/iLearnNew/admin-react/
```

Keep the current MVC Admin at:

```text
/iLearnNew/admin/
```

After feature parity and UAT, the React app can either become `/iLearnNew/admin/` or stay as the new Admin route while legacy MVC remains available behind a legacy link during transition.

## Target Stack

- Vite 8
- React 19
- TypeScript strict mode
- Tailwind CSS 4 via `@tailwindcss/vite`
- DevExtreme 25.2 React components
- `devextreme-aspnet-data-nojquery` for API-backed stores
- `react-router-dom` 7 with `BrowserRouter basename`
- Playwright smoke tests
- Optional SignalR client for `AdminActivityHub`

## Target Frontend Structure

```text
iLearn.Admin.React/
  package.json
  vite.config.ts
  tsconfig.json
  eslint.config.js
  .env.example
  public/
    web.config
  src/
    main.tsx
    App.tsx
    index.css
    devextreme-license.ts
    config/
      appConfig.ts
      navigation.ts
    lib/
      apiClient.ts
      createDataSource.ts
      toast.ts
      auth.ts
      format.ts
    components/
      layout/
        AppLayout.tsx
        Sidebar.tsx
        Header.tsx
      ui/
        AppButton.tsx
        Toolbar.tsx
        PageHeader.tsx
        StatusText.tsx
        DataGridSurface.tsx
        SidePanel.tsx
    pages/
      dashboard/
      courses/
      content-library/
      assignments/
      learner-groups/
      learners/
      master-data/
      access-denied/
```

## Environment Contract

Feature code must not read `import.meta.env` directly. Only `src/config/appConfig.ts` should read environment variables.

Suggested variables:

```dotenv
VITE_ILEARN_ADMIN_APP_BASE_PATH=/iLearnNew/admin-react
VITE_ILEARN_API_BASE_URL=/iLearnNew/Service/api
VITE_ILEARN_ADMIN_PORTAL_BASE_URL=/iLearnNew/admin
VITE_ILEARN_ADMIN_HUB_URL=/iLearnNew/Service/hubs/admin-activity
VITE_DEVEXTREME_LICENSE_KEY=
```

Local development note: Vite proxy should not be used for Windows Authentication. The React app should call the API through an absolute URL and every request must send credentials.

## DevExtreme Data Rules

Based on DevExtreme React 25.2 docs and local skills:

- Use `createStore` from `devextreme-aspnet-data-nojquery` for endpoints already compatible with `DataSourceLoadOptions`.
- Enable DataGrid remote operations for server-side filtering, sorting, and paging.
- Use virtual scrolling for medium and large Admin datasets.
- Memoize every data source. Do not create a new CustomStore on every render.
- Do not set `keyExpr` on DataGrid when the CustomStore already declares the key.
- Avoid client-side processing for large datasets.
- For lookups, prefer dedicated API lookup endpoints such as `Courses/lookup`, `Categories/lookup`, `ContentLibrary/lookup`, `Divisions/lookup`, and `Roles/lookup`.
- For large lookup displays, return human-readable display fields in the main row when possible and use `calculateDisplayValue` rather than eager-loading every lookup list.

Baseline grid shape:

```tsx
<DataGrid dataSource={dataSource} height="100%" showBorders={false} showRowLines={true}>
  <RemoteOperations filtering paging sorting grouping={false} summary={false} />
  <FilterRow visible={true} />
  <HeaderFilter visible={true} />
  <Scrolling mode="virtual" rowRenderingMode="virtual" />
</DataGrid>
```

## Authentication And Authorization

React must rely on the existing API security model. UI hiding is not authorization.

Rules:

- Every `fetch` call uses `credentials: 'include'`.
- Every DevExtreme store sets `xhrFields: { withCredentials: true }` through `onBeforeSend`.
- Every SignalR hub connection passes `{ withCredentials: true }`.
- API endpoints keep `[Authorize(Policy = ...)]` server-side.
- SuperAdmin and division-scoped behavior remains enforced by API claims and services.
- Add or standardize a small `GET /api/admin/session/me` endpoint for the React shell instead of using the MVC-only claim sync path.

The existing `Users/windows-auth` endpoint can be referenced during migration, but a dedicated session endpoint is cleaner for the SPA shell because it returns current identity, roles, division, and access flags without implying user creation.

## API Contract Work Needed

The new SPA can reuse many endpoints immediately, but the API should be audited before each page migration.

Checklist per page:

- Confirm the endpoint has explicit authorization policy.
- Confirm normal admins are division-scoped and SuperAdmin bypasses division filtering.
- Confirm list endpoints use `IQueryable` + `DataSourceLoader.LoadAsync` where data can grow.
- Avoid endpoints that call `GetAllAsync()` and then run `DataSourceLoader` in memory for large tables.
- Return stable DTOs with camelCase fields and domain capability flags, not raw entity graphs.
- Keep create/update workflows in Application services when business rules are involved.
- Use dedicated JSON or multipart endpoints for complex workflows instead of generic CRUD when lifecycle rules apply.

## Migration Order

### Phase 0 - Contract Inventory

- Build a route/page inventory for all Admin MVC views and API endpoints.
- Classify each page as: CRUD list, detail/read-only, wizard, report, or operational action.
- Mark endpoints as ready, needs DTO cleanup, needs authorization review, or needs performance review.
- Add `GET /api/admin/session/me` if missing.

### Phase 1 - React Shell Foundation

- Scaffold `iLearn.Admin.React` with Vite, React, TypeScript, Tailwind, DevExtreme.
- Implement app config, Router basename, DX license bootstrap, API client, DataSource factory, toast helper, and access-denied page.
- Build AppLayout with header, sidebar, route titles, user metadata, and legacy Admin links.
- Configure `.env.example` and IIS `web.config` SPA fallback.
- Add Playwright smoke checks for shell load, session load, and access denied behavior.

### Phase 2 - Read-Only Pilot

Recommended pilot:

1. Dashboard overview.
2. Courses list.
3. Course detail read-only sections.

Reasons:

- Validates routing, auth, API base URL, DataGrid remote operations, status display, and deployment without risking data mutations first.
- Courses are central to later content and assignment workflows.

### Phase 3 - First Controlled Mutation

Recommended first mutation workflow:

1. Content Library publish/unpublish preview and action flow, or
2. Course create/edit without automatic open/retire behavior changes.

Rules:

- Preserve lifecycle rules from `COURSE-LIFECYCLE-RULES.md` and `CONTENT-LIFECYCLE-RULES.md`.
- Keep SCORM upload validation on the server.
- Use typed form state and explicit request DTOs.
- Avoid inline grid editing for lifecycle-sensitive actions unless the API endpoint already owns validation.

### Phase 4 - High-Value Wizards

Migrate workflows where React will pay back the most maintenance cost:

1. Assignments BulkAssign.
2. LearnerGroups Editor and AddMembers.
3. Courses Editor and VersionForm.

Design focus:

- Persistent selected-item trays.
- Clear cascading filters.
- Typed reducer/state machine for wizard steps.
- Review-before-submit summaries.
- No hidden mutation on step navigation.

### Phase 5 - Master Data And Reports

- Migrate lower-risk CRUD pages: Divisions, Categories, Roles, Course Types, Users, System Config.
- Migrate report/profile pages after shared chart/table/report primitives are stable.
- Keep legacy MVC links available until each module passes smoke tests and UAT.

## Deployment Plan

Create a new deploy script, for example:

```text
tools/deploy-admin-react.ps1
```

Expected behavior:

- Run Vite build with `VITE_ILEARN_ADMIN_APP_BASE_PATH=/iLearnNew/admin-react`.
- Publish static output to a side-by-side folder under the IIS share.
- Stamp `public/web.config` SPA fallback to `/iLearnNew/admin-react/index.html`.
- Sync static assets atomically or through a new versioned folder.
- Smoke test:
  - `/iLearnNew/admin-react/`
  - `/iLearnNew/admin-react/courses`
  - `/iLearnNew/Service/api/admin/session/me`
  - A DataGrid endpoint with credentials.

## Testing Strategy

- Unit tests for utility functions: URL config, NID normalization, formatting, status mapping.
- Component tests for layout and workflow state where practical.
- Playwright smoke tests for each migrated page.
- API tests for new or changed contracts, especially lifecycle-sensitive workflows.
- Browser checks must include both local development and IIS subfolder deployment.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Windows Auth breaks in local Vite dev | Use direct absolute API URL, no Vite proxy, credentials included, configured CORS origins. |
| React duplicates old business rules | Keep business rules in API/Application services; React only orchestrates UI state and confirmations. |
| Generic CRUD endpoints bypass lifecycle rules | Use custom API endpoints for course/content/assignment workflows. |
| DataGrid loads too much data | Prefer `IQueryable` + `DataSourceLoader.LoadAsync`, remote operations, virtual scrolling. |
| Side-by-side migration confuses users | Keep navigation explicit: New Admin vs Legacy Admin links, page-by-page rollout. |
| Status names drift | Centralize frontend status maps from `STATUS-DEFINITIONS.md`. |
| SCORM upload behavior regresses | Keep server-side validation and run SCORM regression tests before replacing related pages. |

## Initial Definition Of Done

The React rebuild is ready to begin feature migration when:

- The shell loads from IIS under `/iLearnNew/admin-react/`.
- Current user, roles, and division display correctly from an authenticated API endpoint.
- A remote DevExtreme React DataGrid loads with filtering, sorting, and virtual scrolling.
- Access denied behavior works for missing role permissions.
- Playwright smoke tests run against the deployed QA URL.
- No MVC Admin route is removed or broken.

## Open Decisions

1. Final route name: `/admin-react`, `/admin-next`, or another temporary path.
2. Whether the React app eventually replaces `/admin` or stays as a separate Admin experience.
3. Which pilot page should be first: Dashboard/Courses read-only is safest; BulkAssign gives faster UX payoff.
4. Whether to create a Backend-for-Frontend facade for complex workflows, or call existing API endpoints directly from React.
5. How long legacy MVC Admin remains available after React parity.