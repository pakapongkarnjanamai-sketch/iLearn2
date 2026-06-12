# iLearn.Admin.React

Standalone React Admin shell for the iLearn LMS. This project is intentionally side by side with the existing `iLearn.Admin` MVC application and is not added to `iLearn.sln`.

## Stack

- Vite 8, React 19, TypeScript
- Tailwind CSS 4 through `@tailwindcss/vite`
- React Router 7 with `BrowserRouter` basename support
- Recharts for dashboard charts, lucide-react for icons
- Windows-auth API calls with `credentials: 'include'`

## Local Setup

```powershell
npm install
npm run dev
```

Create a local `.env.local` from `.env.example` when the API base path or deploy base path differs from QA.

## Environment

```text
VITE_ILEARN_ADMIN_APP_BASE_PATH=/
VITE_ILEARN_ADMIN_API_BASE_URL=/api
VITE_ILEARN_ADMIN_SIGNALR_BASE_URL=
VITE_ILEARN_ADMIN_ENABLE_SIGNALR=false
VITE_ILEARN_ADMIN_ENABLE_SESSION_BOOTSTRAP=true
```

For localhost development, point `VITE_ILEARN_ADMIN_API_BASE_URL` at the absolute API origin because the Vite proxy does not relay NTLM/Negotiate correctly. In deployed IIS environments, relative values such as `/api` are preferred. Leave `VITE_ILEARN_ADMIN_SIGNALR_BASE_URL` empty to auto-derive from the API base URL.

For IIS deployment under `/iLearnNew/admin-react`, set `VITE_ILEARN_ADMIN_APP_BASE_PATH=/iLearnNew/admin-react/` before building. The static `public/web.config` contains the matching SPA fallback path.

## UI Conventions

Pages must compose the shared building blocks in `src/components/ui` instead of re-implementing them locally:

- `LoadingState` — every loading spinner. `<LoadingState />` for full-page loads (optional `label`), `<LoadingState size="section" />` inside panels/tabs.
- `NotFoundState` — every "record not found / invalid route" screen, with `backTo`/`backLabel` and an optional `tone="danger"`.
- `ControlsSidebar` + `ControlAction` + `ControlsDivider` — the sticky right-hand controls panel on detail pages. `ControlAction` renders a `Link` when given `to`, a button when given `onClick`, and supports `variant="danger" | "primary"`, `disabled`, `loading`, and `type="submit"`.
- `StatusBadge` — solid soft-background status pills in tables/KPIs. With no `tone`, the tone is derived from the status text via `statusTone()` (Completed → green, In Progress/Active/Enrolling → blue, Overdue/Expired → red).
- `StatusText` — outlined rounded-full status pill for overview/metadata sections (`tone="neutral" | "success" | "warning" | "danger"`).
- `useConfirm` / `ConfirmDialog` — every destructive or irreversible action must confirm through `await confirm({ title, message, danger })` and render `{confirmDialog}` in the page — never `window.confirm`.
- `SectionHeader` — icon + heading above tables/panels. `variant="plain"` (uppercase, used above open content) or `variant="card"` (inside bordered panels).
- `ProgressBar` — slim progress bar with percentage label in learner/enrollment tables.
- `AppButton`, `AppTable`, `AppWizard`, `PageHeader` — existing shared widgets; check here before writing new markup.
- Dates: always format through `formatDate` / `formatDateTime` from `src/lib/format.ts` — never call `toLocaleDateString()` inline.
- Wizard/editor forms: use the `.wiz-section` / `.wiz-label` / `.wiz-input` classes from `index.css` so all forms share one scale.

When a visual pattern appears on a second page, extract it into `src/components/ui` rather than copying the markup.

## Validation

```powershell
npm run lint
npm run build
```

The current shell reads from existing Admin API endpoints but does not modify MVC views, .NET projects, or the solution file.