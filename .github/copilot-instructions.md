# Copilot Instructions — iLearn2

## Project Overview
**iLearn2**: Internal e-Learning Management System (LMS) with SCORM 1.2/2004 support.
- `iLearn.API` (Port 7128): ASP.NET Core Web API (Backend)
- `iLearn.Admin` (Port 7270): ASP.NET Core MVC (Admin)
- `iLearn.User` (Port 7078): ASP.NET Core MVC + Razor Pages (Learners)

## Tech Stack & Architecture
- **.NET 9.0 / C# 13.0**, **Clean Architecture**, **EF Core 9** (SQL Server, Code-First)
- **DevExtreme 25.2**, **Bootstrap 5**, **jQuery**
- **Layers**: `Domain` (zero dependencies) <- `Application` <- `Infrastructure` <- `Presentation` (API/Admin/User)

## Coding Rules & Patterns
1. **Soft Delete**: Use `DeleteAsync()` (`IsDeleted = true`). `HardDeleteAsync()` is strictly for `FileStorage`.
2. **Audit Fields**: Auto-set by `AppDbContext` (`CreatedAt/By`, `UpdatedAt/By`, `DeletedBy`). Do NOT set manually.
3. **Mapping**: Use manual extension methods in `MappingExtensions.cs` (e.g., `dto.ToEntity()`). No AutoMapper.
4. **Controllers**:
   - `GenericController<T>`: Uses `DataSourceLoader.Load()` & `JsonConvert.PopulateObject`.
   - Business Controllers: Standard REST with injected services.
5. **DateTime**: Always use injected `IDateTime` (`DateTimeService.Now` returns UTC+7). Never use `DateTime.Now`.
6. **JSON**: API uses `System.Text.Json` (camelCase). DevExtreme forms use `Newtonsoft.Json`.
7. **Assignment No**: Use `IAssignmentNoGenerator.NextAsync()` (Format: `AS-yyyyMMdd-NNN`).
8. **DI Registration**: `AddApplication()` and `AddInfrastructure()` in API. `ApiUserService` is ONLY for Admin/User apps.

## Key Relationships
- `Course` 1:N `CourseVersion` 1:N `CourseResource` N:1 `Resource` 1:1 `FileStorage`
- `Course` 1:N `Enrollment` N:M `Assignment` (via `EnrollmentAssignment`)
- `Assignment` N:1 `StudentGroup` 1:N `StudentGroupMember`

## Auth & External APIs
- **Auth**: Windows Auth. `ApiUserSyncMiddleware` syncs identity to API user & injects roles.
- **External API**: `EmployeeServiceV2` for student/employee lookups and dropdown data.