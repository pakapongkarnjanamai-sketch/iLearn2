# Copilot Instructions — iLearn2

## Project Overview
**iLearn2**: ระบบ Internal e-Learning (LMS) รองรับมาตรฐาน SCORM 1.2/2004
- `iLearn.API`: ASP.NET Core Web API (Backend)
- `iLearn.Admin`: ASP.NET Core MVC (Admin UI - Brand Blue #0050b3)
- `iLearn.User`: ASP.NET Core MVC + Razor Pages (Learner UI - Brand Teal #027d83)

## Architecture & Tech Stack
- **Clean Architecture**: Domain -> Application -> Infrastructure -> Presentation
- **Stack**: .NET 9, C# 13, EF Core 9 (SQL Server), Windows Auth
- **Frontend**: DevExtreme 25.2, Bootstrap 5, jQuery, SweetAlert2

## Coding Rules
1. **Soft Delete**: ใช้ `DeleteAsync()` (`IsDeleted = true`) เป็นค่าเริ่มต้น (Hard delete เฉพาะ FileStorage)
2. **Audit Fields**: ปล่อยให้ `AppDbContext` จัดการ `CreatedAt/By`, `UpdatedAt/By` อัตโนมัติ ห้าม set เอง
3. **Manual Mapping**: ใช้ Extension methods ใน `MappingExtensions.cs` (เช่น `dto.ToEntity()`) ห้ามใช้ AutoMapper
4. **DateTime**: ใช้ `IDateTime` (DI) เท่านั้น เพื่อให้เป็นเวลาไทย (UTC+7) ห้ามใช้ `DateTime.Now`
5. **Assignment No**: ใช้ `IAssignmentNoGenerator.NextAsync()` (Format: `AS-yyyyMMdd-NNN`)
6. **Dependency Injection**: `AddApplication()` และ `AddInfrastructure()` ลงทะเบียน Services หลักทั้งหมด

## Front-end Design Patterns

โปรเจ็กต์นี้มีการออกแบบ UI 2 สไตล์ที่แตกต่างกันอย่างชัดเจน:

### 1. Admin UI (`iLearn.Admin`) - "Minimal Clean Japanese / Ant Design"
เน้นความเรียบง่าย แบนราบ (Flat) ข้อมูลหนาแน่น (Data-heavy)
- **Colors**: Primary `#0050b3`, Background `#fafafa`, Border `#f0f0f0`
- **Typography**: Base `13px` (System font), Section Headers `11px` (Uppercase + Letter spacing)
- **Elements**: ขอบเหลี่ยมเล็กน้อย (`border-radius: 4px`), เน้นใช้เส้นขอบบางๆ (`1px solid`) แทนการใช้เงา (No Box-shadow)
- **Components**: ใช้ `.page-header`, `.panel`, `.status-pill` (โค้งมนสีอ่อน), `.action-link` (ปุ่มแบบ flat)

### 2. Learner UI (`iLearn.User`) - "Soft UI / User-Friendly"
เน้นความนุ่มนวล เป็นมิตร ใช้งานบนมือถือได้ดี
- **Colors**: Primary `#027d83`, Background `#f4f6f8`, Light/Tint `#e0f2f1`
- **Typography**: ใช้ฟอนต์ **'Sarabun'** เป็นหลักเพื่อความสบายตาในการอ่านภาษาไทย
- **Elements**: ขอบมน (`border-radius: 8px`), ใช้เงาฟุ้งแบบนุ่มนวล (`box-shadow: 0 4px 20px rgba(0,0,0,0.08)`)
- **Interactions**: ปุ่มและการ์ดควรมี Hover effect (เช่น `transform: translateY(-2px)` และเพิ่มเงา)

### 3. Notifications & Feedback
- **Quick Toast (Stack)**: ให้ใช้ฟังก์ชัน Global `showToast(message, type)` หรือถ้าเรียก `DevExpress.ui.notify` โดยตรง **ต้องส่งพารามิเตอร์ที่ 2 เสมอ** เช่น `{ position: 'bottom center', direction: 'up-push' }` เพื่อให้ Toast แสดงผลแบบเรียงซ้อนกันได้ (Stack) และตกแต่งสไตล์ข้อความให้รองรับ Soft UI (มีเงา, ขอบมน, มีไอคอน FontAwesome)
- **Dialog & Confirm**: ใช้ **SweetAlert2** (`Swal.fire`) โดยต้องปรับ Custom Class ให้ปุ่ม Confirm เป็นปุ่มสีธีม (`.btn-primary`) และขอบกล่องโค้งมน
- **Empty States**: หากไม่มีข้อมูลให้แสดงไอคอนขนาดใหญ่สีเทาจาง พร้อมข้อความที่เป็นมิตรตรงกลางพื้นที่ว่างเสมอ

### 4. DevExtreme Implementation Rules
- โค้ด JavaScript เฉพาะหน้าต้องอยู่ใน `@section Scripts { }` เสมอ
- **DataStore**: สร้างผ่านฟังก์ชัน `createDataStore(baseUrl, controllerName, options)`
- **DataGrid (Admin)**: เริ่มต้นด้วย `initDxGrid(selector, options)` ซึ่งตั้งค่า Default ไว้แล้ว (แสดงกรอบและสลับสีแถว)
- **Exporting**: ใช้ `handleExporting(e, fileName)` สำหรับ Export Excel (ทำงานผ่าน ExcelJS)