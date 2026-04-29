# Contributing

## Project Standards

### Authorization and Access Control
- `SuperAdmin` must be able to access every admin page and see all data across all divisions.
- Access rules must be enforced server-side with authorization attributes or policies. Do not rely on hiding menus or buttons in the UI.
- Pages or endpoints intended only for `SuperAdmin` must use the `SuperAdminOnly` policy.
- Division-based data isolation must bypass filtering for `SuperAdmin` users.
- When role or division claims are refreshed, both UI-side and API-side authorization behavior must reflect the updated claims consistently.

### Data Isolation
- For normal division-scoped administrators, filter data by the current user's `DivisionId`.
- For `SuperAdmin`, data queries must not apply division filtering.

### Review Checklist
- Verify the page is reachable by `SuperAdmin`.
- Verify `SuperAdmin` can load unfiltered data.
- Verify non-`SuperAdmin` users cannot access `SuperAdmin`-only pages.
- Verify authorization is enforced in controllers or endpoints, not only in navigation.

## API Layering Rules (`iLearn.API`)

`iLearn.API` is the presentation layer. It should contain HTTP plumbing
only — controllers, middleware, hubs, DI composition, and serialization
configuration. The following layering rules apply:

- **Controllers may depend only on `iLearn.Application` interfaces and DTOs.**
  Controllers must not reference `iLearn.Infrastructure` types directly
  (no `using iLearn.Infrastructure.Persistence;`, no `AppDbContext` injection,
  no `DbSet<T>` access).
- **Transactions** belong in services or controllers via `IUnitOfWork`
  (`BeginTransactionAsync`, `SaveChangesAsync`, `AddRangeAsync<T>`), not via
  `_dbContext.Database.BeginTransactionAsync()`.
- **Cross-cutting concerns** (caching, maintenance status, realtime
  notifiers) live behind interfaces in `iLearn.Application.Interfaces.*`,
  with implementations in `iLearn.Infrastructure.Services` (or in `iLearn.API`
  only when they are intrinsically presentation, e.g., the SignalR notifier).
- **Composition root**: `iLearn.API/Program.cs` is intentionally thin and
  delegates to extension methods in `iLearn.API/Extensions/` (auth, authz,
  CORS, Swagger, presentation). Configuration values such as the Windows
  domain prefix or CORS origins must be read from `appsettings*.json`,
  never hard-coded in `Program.cs`.
- **Errors**: throw the most specific exception type (or
  `KeyNotFoundException` / `InvalidOperationException` / `ArgumentException`).
  The global `GlobalExceptionMiddleware` converts these into standard
  `ProblemDetails` responses; per-action `try/catch + StatusCode(500, ex.Message)`
  blocks are not allowed because they leak exception details.

## Local Secret Bootstrap

- Live secrets must not be committed to `appsettings.json` or `appsettings.Development.json`. Repository config now contains placeholders only.
- `iLearn.API` requires `ConnectionStrings:DefaultConnection` and `LearnerProxyAuth:SharedSecret` from `dotnet user-secrets` or environment variables.
- `iLearn.User` requires the same `LearnerProxyAuth:SharedSecret` value as `iLearn.API`.
- Both apps fail fast during startup in `Development` when a required secret is missing or still uses a placeholder value.

Use `dotnet user-secrets` for local development:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<sql-connection-string>" --project .\iLearn.API\iLearn.API.csproj
dotnet user-secrets set "LearnerProxyAuth:SharedSecret" "<shared-secret>" --project .\iLearn.API\iLearn.API.csproj
dotnet user-secrets set "LearnerProxyAuth:SharedSecret" "<same-shared-secret>" --project .\iLearn.User\iLearn.User.csproj
```

Equivalent environment variable names:

- `ConnectionStrings_DefaultConnection`
- `LearnerProxyAuth_SharedSecret`

Typical local run commands after provisioning secrets:

```powershell
dotnet run --project .\iLearn.API\iLearn.API.csproj --launch-profile https
dotnet run --project .\iLearn.User\iLearn.User.csproj --launch-profile https
```

## Production Deploy Scripts

Production side-by-side deploys should run with PowerShell 7 (`pwsh`), not Windows PowerShell 5.1.

Install PowerShell 7 once:

```powershell
winget install --id Microsoft.PowerShell --source winget
```

User app deploy:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1
```

API deploy:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1
```

Admin deploy:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-admin.ps1
```

Dry-run before switching production:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-admin.ps1 -SkipPublish -WhatIf
```

Both scripts publish to `artifacts/publish/*`, copy to the production UNC share, then update the target app's `web.config` `aspNetCore.arguments` to the new side-by-side folder.

Use the step-by-step runbook in `DOC/DEPLOY-CHECKLIST.md` for every deploy and rollback.