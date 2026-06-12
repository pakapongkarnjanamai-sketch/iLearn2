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

**Route state leakage:** React Router reuses a component instance when two routes render the same component type (`courses/new` ↔ `courses/:id/edit`, or `/courses/1` → `/courses/2`), so form values, tabs, and selections leak across pages. Every detail/editor route in `App.tsx` must be wrapped in `<Remount>` (keys by pathname, forces a clean mount). List routes through `EntityListPage` are covered by `key={config.controller}` on `AppTable`.

## API Contract Sync

UI ที่ว่างเปล่าโดยไม่มี error เกือบทุกครั้งเกิดจาก type ฝั่ง React ไม่ตรงกับ response จริง — ป้องกันด้วยกติกานี้:

1. **ทุก response type ต้องคัดลอกจาก DTO ฝั่ง C# จริงเท่านั้น** — ห้ามเดา field จากชื่อที่ "น่าจะใช่" ให้เปิดไฟล์ DTO หรือ controller แล้วลอก property มาทีละตัว (anonymous object ใน `Ok(new {...})` ให้ลอกจาก controller โดยตรง)
2. **ใส่คอมเมนต์ `// Mirrors <DtoName> (<path>)` เหนือ type ทุกตัว** เช่น `// Mirrors AssignmentDashboardDto returned by GET Assignments/dashboard/{id}` — เพื่อให้ตามรอยกลับไปเช็คได้เมื่อสงสัย
3. **เมื่อแก้ DTO หรือ response ฝั่ง backend** ให้ grep หา endpoint path นั้นใน `src/` (เช่น `grep "Assignments/dashboard"`) แล้วอัปเดต type ฝั่ง React ในคอมมิตเดียวกัน
4. **field ที่ backend ประกาศเป็น nullable (`string?`, `DateTime?`) ต้องเป็น optional/nullable ฝั่ง TS ด้วย** และตอน render ต้องมี fallback (`?? '-'`)
5. **อย่าใช้ field ที่ไม่มีใน DTO เป็น React key** — ถ้า DTO ไม่มี `id` ให้ประกอบ key จาก field ที่ unique จริง (เช่น `${learnerCode}-${assignmentRuleId}`)
6. ทดสอบหน้าใหม่กับ API จริงเสมอ — ถ้าหน้า render แต่ค่าเป็น `undefined`/ว่าง ให้เปิด Network tab เทียบ payload กับ type ก่อนทำอย่างอื่น

## Validation

```powershell
npm run lint
npm run build
```

The current shell reads from existing Admin API endpoints but does not modify MVC views, .NET projects, or the solution file.