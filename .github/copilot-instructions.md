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

## Front-end Design Patterns (Minimal Clean Japanese)
เน้นความเรียบง่าย สะอาดตา (Zen) อ้างอิงสไตล์ Ant Design โดยใช้เส้นขอบแทนการใช้เงา

1. **Design Tokens (Admin)**:
   - **Colors**: Primary `#0050b3`, Border `#f0f0f0`, Background `#fafafa`
   - **Typography**: Base `13px`, Section Headers `11px` (Uppercase + Letter spacing)
   - **Elements**: ใช้ `1px solid` border, `border-radius: 4px`, และห้ามใช้ Box-shadow

2. **UI Components**:
   - **Page Header**: ใช้คลาส `.page-header` เป็นแถบสีขาวเรียบๆ ติดขอบบน
   - **Panels**: ใช้คลาส `.panel` และ `.panel-body` สำหรับแบ่งส่วนเนื้อหา
   - **Status/Tags**: ใช้ `.status-pill` (โค้งมน) หรือ `.tag-pill` (เหลี่ยม) พร้อมสีแบบ Tint (พื้นหลังอ่อน ตัวอักษรเข้ม)
   - **Actions**: ใช้คลาส `.action-link` สำหรับปุ่มหรือลิงก์คำสั่ง

3. **DevExtreme Implementation**:
   - **DataGrid**: เริ่มต้นด้วย `initDxGrid(selector, options)` พร้อมเปิด `rowAlternationEnabled` และ `showBorders`
   - **DataStore**: สร้างผ่าน `createDataStore(baseUrl, controllerName, options)`
   - **Export**: ใช้ `handleExporting(e, fileName)` สำหรับ Excel export (ExcelJS)

4. **Scripts**:
   - โค้ด JavaScript เฉพาะหน้าต้องอยู่ใน `@section Scripts { }`
   - ใช้ `Swal.fire(...)` สำหรับการแจ้งเตือนหรือยืนยันคำสั่ง