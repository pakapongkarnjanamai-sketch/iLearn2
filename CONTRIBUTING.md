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