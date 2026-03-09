# Copilot Instructions — iLearn2

> GitHub Copilot ????????????????????????????????????????? repository ???
> ????????????????????????? — path ???????? `.github/copilot-instructions.md` ????????

---

## Project Overview

**iLearn2** ???? Internal e-Learning Management System (LMS) ????????????
???????????????????, ?????????? (Assignment), ???????????????? (Enrollment / LearningLog)
?????? SCORM 1.2/2004 Content ???????????????????????????????????? HTTP API

?????????????? 3 Web App:

| App | Type | Users | Dev Port |
|---|---|---|---|
| `iLearn.API` | ASP.NET Core Web API | Backend only | 7128 |
| `iLearn.Admin` | ASP.NET Core MVC (Razor Views) | Admin / SuperAdmin | 7270 |
| `iLearn.User` | ASP.NET Core MVC + Razor Pages | Learners (employees) | 7078 |

---

## Tech Stack

- **.NET 9.0** / **C# 13.0**
- **Clean Architecture** (Domain ? Application ? Infrastructure ? Presentation)
- **Entity Framework Core 9** with SQL Server (Code-First + Migrations)
- **Windows Authentication** (Negotiate / Kerberos / Active Directory SSO)
- **DevExtreme 25.2** (DataGrid, Chart, Gantt, Form, Popup, FileUploader)
- **DevExtreme.AspNet.Data 5.1** (server-side DataSourceLoader)
- **Bootstrap 5**, **jQuery**, **Font Awesome**, **SweetAlert2**
- **ExcelJS + FileSaver.js** (client-side Excel export)
- **Newtonsoft.Json** (GenericController) + **System.Text.Json** (API JSON options)
- **Swagger / Swashbuckle** for API documentation
- **xUnit + Coverlet** for testing
- **SCORM 1.2 / 2004** for e-Learning content

---

## Architecture & Project References

```
iLearn.Admin  ? iLearn.API ? iLearn.Infrastructure ? iLearn.Application ? iLearn.Domain
iLearn.User   ? iLearn.Application ? iLearn.Domain
iLearn.Tests  ? iLearn.Application + iLearn.Domain
```

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `iLearn.Domain` | Entities, Enums, BaseEntity — **zero dependencies** |
| Application | `iLearn.Application` | Interfaces, DTOs, Services, Mappings, Middleware, DI |
| Infrastructure | `iLearn.Infrastructure` | AppDbContext, Repositories, UnitOfWork, EF Migrations |
| Presentation | `iLearn.API` / `Admin` / `User` | Controllers, Views, Program.cs |

---

## Key Domain Entities

All entities inherit `BaseEntity` which provides:
`Id`, `IsActive`, `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt/By`

### Entity Relationships
```
Course (1) ? (N) CourseVersion (1) ? (N) CourseResource ? (N) Resource ? (1) FileStorage
Course (1) ? (N) Enrollment (N) ?? (N) Assignment  (via EnrollmentAssignment)
Course (1) ? (1) CourseType
Course (1) ? (1) Category ? (1) Division
Assignment (N) ? (1) StudentGroup (1) ? (N) StudentGroupMember
User (N) ?? (N) Role (via UserRole)
```

---

## DI Registration Pattern

```csharp
// In API Program.cs:
builder.Services.AddApplication();                         // from iLearn.Application.DependencyInjection
builder.Services.AddInfrastructure(builder.Configuration); // from iLearn.Infrastructure.DependencyInjection
```

### AddApplication() registers:
`ICourseService`, `ICourseVersionService`, `ICourseAssignmentService`,
`IAssignmentDashboardService`, `IStudentGroupService`

### AddInfrastructure() registers:
`AppDbContext`, `IUnitOfWork`, `IGenericRepository<>`, `ICourseRepository`,
`IDateTime`, `ICurrentUserService`, `IAssignmentNoGenerator`, `IScormService`,
`IStudentApiService` (HttpClient), `FileSettings` (Options)

### Admin / User apps additionally register (NOT in API):
`IApiUserService` ? `ApiUserService`,
`AddHttpClient("iLearnAPI")`, `AddMemoryCache()`,
`ApiUserSyncMiddleware`

> ?? **ApiUserService** must NEVER be registered in iLearn.API — it's only for Admin/User apps.

---

## Coding Rules

### 1. Soft Delete by Default
- `DeleteAsync()` = Soft Delete (`IsDeleted = true`, record stays in DB)
- `HardDeleteAsync()` = Real delete (only for `FileStorage`)
- `AppDbContext` applies global `HasQueryFilter(e => !e.IsDeleted)` to all entities automatically

### 2. Audit Fields
`AppDbContext.SaveChangesAsync` automatically sets `CreatedAt/By` on Add, `UpdatedAt/By` on Modify, `DeletedBy` on Soft Delete. Never set these manually.

### 3. Manual Mapping (No AutoMapper)
Use extension methods in `Application/Mappings/MappingExtensions.cs`.
Example: `course.ToDto()`, `dto.ToEntity()`

### 4. API Controller Patterns
- **GenericController\<T\>**: Route `api/admin/[controller]`, uses `DataSourceLoadOptions` + `DataSourceLoader.Load()`, `JsonConvert.PopulateObject` for form values
- **Business Controllers**: Route `api/[controller]`, standard REST endpoints with service injection

### 5. Front-end Patterns
- Layout: `_DevExtremeLayout.cshtml` loads all scripts + defines helper functions
- Helper: `createDataStore(baseUrl, controllerName, options)` — creates DevExtreme AspNet DataStore
- Helper: `handleExporting(e, fileName)` — Excel export via ExcelJS
- Helper: `initDxGrid(selector, pageOptions)` — creates DataGrid with defaults
- Alerts: Use `Swal.fire(...)` (SweetAlert2)
- Page scripts go in `@section Scripts { }`

### 6. DateTime
`DateTimeService.Now` returns `DateTime.UtcNow.AddHours(7)` (Thai timezone).
Always use `IDateTime` via DI — never call `DateTime.Now` directly.

### 7. JSON Serialization
- API controllers use `System.Text.Json` with `camelCase` + `IgnoreCycles`
- `GenericController` uses `Newtonsoft.Json` (`JsonConvert.PopulateObject`) for DevExtreme form values

### 8. Assignment Number Generation
Uses DB Sequence `AssignmentNoSeq` via `IAssignmentNoGenerator.NextAsync()`.
Format: `AS-yyyyMMdd-NNN` (race-condition free).

---

## Namespace Quirk ??

Some service implementations live in `Application/Services/` but use namespace `iLearn.Infrastructure.Services`:
- `ScormService.cs`
- `StudentApiService.cs`
- `CurrentUserService.cs`
- `DateTimeService.cs`

This is a known tech debt from a refactor. Do NOT change these namespaces without also updating all `DependencyInjection.cs` files and `using` directives across the solution.

---

## Configuration

### iLearn.API — `appsettings.json`
```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "FileSettings": { "HostUrl": "...", "HostUnc": "...", "CourseFolder": "course" }
}
```

### iLearn.Admin & iLearn.User — `appsettings.json`
```json
{
  "ApiSettings": { "BaseUrl": "https://.../api" }
}
```

---

## External API Integration

| Endpoint | Purpose |
|---|---|
| `EmployeeServiceV2/api/StudentLookup/{code}` | Lookup single employee |
| `EmployeeServiceV2/api/Student/all` | Bulk lookup (server-cached 24h) |
| `EmployeeServiceV2/api/Student/divisions` | Employees by division |
| `EmployeeServiceV2/api/StudentLookup/GetDistinct*` | Dropdown data (Sections, Divisions, Departments, Positions) |

---

## Authentication & Authorization

- All apps use **Windows Authentication** (Negotiate/Kerberos)
- `ApiUserSyncMiddleware` (Admin/User only): syncs Windows identity ? API user ? injects Role claims
- Policies: `AdminOnly`, `SuperAdminOnly`, `ManagerOrAbove`, `UserOrAbove`, `DomainUser`

---

## Known Technical Debt

1. `IUnitOfWork` is registered but not yet consumed — services still call `Repository.SaveChangesAsync()` directly
2. `FileSettings.cs` uses file-scoped namespace (no explicit declaration visible)
3. `ScormController.cs` is entirely commented out
4. `iLearn.Admin` references `iLearn.API.csproj` directly (should ideally reference only Infrastructure/Application)
5. Some namespace/file-location mismatches (see Namespace Quirk section above)
