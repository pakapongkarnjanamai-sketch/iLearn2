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