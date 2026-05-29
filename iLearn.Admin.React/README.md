# iLearn.Admin.React

Standalone React Admin shell for the iLearn LMS. This project is intentionally side by side with the existing `iLearn.Admin` MVC application and is not added to `iLearn.sln`.

## Stack

- Vite 8, React 19, TypeScript
- Tailwind CSS 4 through `@tailwindcss/vite`
- DevExtreme React 25.2 with `devextreme-aspnet-data-nojquery`
- React Router 7 with `BrowserRouter` basename support
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
VITE_ILEARN_ADMIN_DEVEXTREME_LICENSE_KEY=ewogICJmb3JtYXQiOiAxLAogICJjdXN0b21lcklkIjogIjQzMzdjY2M1LTA4ZjYtNDE2NS05NmJiLWU3MmY1NmY2MjA4MCIsCiAgIm1heFZlcnNpb25BbGxvd2VkIjogMjUyCn0=.msUWqj0CLKKVTKUeCMJaSMQVVJywgLDSkWDBfPtwwreYLfwUyK/UvfODZGJNx7wAaZlPK4SIgVLQZGkGwaKEpGXSTkOp20qOjyy0xCUGBN73QilDt/zJHzjAFvDXkJcsEr6Pgg==
```

For localhost development, point `VITE_ILEARN_ADMIN_API_BASE_URL` at the absolute API origin because the Vite proxy does not relay NTLM/Negotiate correctly. In deployed IIS environments, relative values such as `/api` are preferred. Leave `VITE_ILEARN_ADMIN_SIGNALR_BASE_URL` empty to auto-derive from the API base URL.

The DevExtreme license key should use the full signed key value, not only the base64 payload prefix. The app applies `VITE_ILEARN_ADMIN_DEVEXTREME_LICENSE_KEY` from `src/devextreme-license.ts`, with a legacy fallback for older env files.

For IIS deployment under `/iLearnNew/admin-react`, set `VITE_ILEARN_ADMIN_APP_BASE_PATH=/iLearnNew/admin-react/` before building. The static `public/web.config` contains the matching SPA fallback path.

## Validation

```powershell
npm run lint
npm run build
```

The current shell reads from existing Admin API endpoints but does not modify MVC views, .NET projects, or the solution file.