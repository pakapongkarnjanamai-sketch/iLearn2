# Copilot Instructions — iLearn2

## Project Overview
**iLearn2**: ระบบ Internal e-Learning (LMS) รองรับมาตรฐาน SCORM 1.2/2004
- `iLearn.API`: ASP.NET Core Web API (Backend)
- `iLearn.Admin`: ASP.NET Core MVC (Admin UI - Brand Blue #0050b3)
- `iLearn.User`: ASP.NET Core MVC + Razor Pages (Learner UI - Brand Teal #027d83)

## Architecture & Tech Stack
- **Clean Architecture**: Domain -> Application -> Infrastructure -> Presentation
- **Stack**: .NET 9, C# 13, EF Core 9 (SQL Server), Windows Auth
- **Frontend**: DevExtreme 25.2, Bootstrap 5, jQuery, DevExpress dialogs

## Coding Rules
1. **Soft Delete**: ใช้ `DeleteAsync()` (`IsDeleted = true`) เป็นค่าเริ่มต้น (Hard delete เฉพาะ FileStorage)
2. **Audit Fields**: ปล่อยให้ `AppDbContext` จัดการ `CreatedAt/By`, `UpdatedAt/By` อัตโนมัติ ห้าม set เอง
3. **Manual Mapping**: ใช้ Extension methods ใน `MappingExtensions.cs` (เช่น `dto.ToEntity()`) ห้ามใช้ AutoMapper
4. **DateTime**: ใช้ `IDateTime` (DI) เท่านั้น เพื่อให้เป็นเวลาไทย (UTC+7) ห้ามใช้ `DateTime.Now`
5. **Assignment No**: ใช้ `IAssignmentNoGenerator.NextAsync()` (Format: `AS-yyyyMMdd-NNN`)
6. **Dependency Injection**: `AddApplication()` และ `AddInfrastructure()` ลงทะเบียน Services หลักทั้งหมด
7. **Data Isolation (Important)**: 
   - ใน API Controllers (`[HttpGet] Get`) ต้องกรองข้อมูลตาม `_currentUser.DivisionId` เสมอ เพื่อให้ Admin แต่ละแผนกเห็นเฉพาะข้อมูลของตัวเอง
   - ใช้เงื่อนไข `if (_currentUser.DivisionId.HasValue)` ในการกรอง Query
   - ระบบจัดการ **Bypass** ให้สิทธิ์ระดับสูง (เช่น `SuperAdmin`, `Admin`) อัตโนมัติจาก `CurrentUserService` ซึ่งจะคืนค่า `DivisionId` เป็น `null` ทำให้มองเห็นข้อมูลทั้งหมดทั่วระบบ
8. **Backend Language (English Only)**: การเขียนคอมเมนต์ (Comments), การเก็บ Logs, ข้อความ Exceptions และ Response Messages ภายในฝั่ง Backend (`iLearn.API`, `Application`, `Infrastructure`) **ต้องเขียนเป็นภาษาอังกฤษทั้งหมด** เพื่อความเป็นมาตรฐานสากลและหลีกเลี่ยงปัญหา Encoding (ส่วน Frontend UI ยังคงแสดงผลภาษาไทยให้ผู้ใช้งานตามปกติ)


## Front-end Design Patterns

โปรเจ็กต์นี้มีการออกแบบ UI 2 สไตล์ที่แตกต่างกันอย่างชัดเจน:

### 1. Admin UI (`iLearn.Admin`) - "Minimal Clean Japanese / Ant Design"
เน้นความเรียบง่าย แบนราบ (Flat) ข้อมูลหนาแน่น (Data-heavy)
- **Colors**: Primary `#0050b3`, Background `#fafafa`, Border `#f0f0f0`
- **Typography**: Base `13px` (System font), Section Headers `11px` (Uppercase + Letter spacing)
- **Elements**: ขอบเหลี่ยมเล็กน้อย (`border-radius: 4px`), เน้นใช้เส้นขอบบางๆ (`1px solid`) แทนการใช้เงา (No Box-shadow)
- **Components**: ใช้ `.page-header`, `.panel`, `.status-pill` (โค้งมนสีอ่อน), `.action-link` (ปุ่มแบบ flat), `.quick-action` สำหรับแผง Actions ในหน้า Detail
- **Detail Action Panels**: ในหน้า Detail ของฝั่ง Admin ให้ใช้การ์ด Actions แบบ `quick-action` เป็นมาตรฐาน, รองรับสถานะ `disabled`, และถ้ามี action เชิงทำลายให้ใช้ class `danger`

### 2. Learner UI (`iLearn.User`) - "Soft UI / User-Friendly"
เน้นความนุ่มนวล เป็นมิตร ใช้งานบนมือถือได้ดี
- **Colors**: Primary `#027d83`, Background `#f4f6f8`, Light/Tint `#e0f2f1`
- **Typography**: ใช้ฟอนต์ **'Sarabun'** เป็นหลักเพื่อความสบายตาในการอ่านภาษาไทย
- **Elements**: ขอบมน (`border-radius: 8px`), ใช้เงาฟุ้งแบบนุ่มนวล (`box-shadow: 0 4px 20px rgba(0,0,0,0.08)`)
- **Interactions**: ปุ่มและการ์ดควรมี Hover effect (เช่น `transform: translateY(-2px)` และเพิ่มเงา)

### 3. Notifications & Feedback
- **Quick Toast (Stack)**: ให้ใช้ฟังก์ชัน Global `showToast(message, type)` หรือถ้าเรียก `DevExpress.ui.notify` โดยตรง **ต้องส่งพารามิเตอร์ที่ 2 เสมอ** เช่น `{ position: 'bottom center', direction: 'up-push' }` เพื่อให้ Toast แสดงผลแบบเรียงซ้อนกันได้ (Stack) และตกแต่งสไตล์ข้อความให้รองรับ Soft UI (มีเงา, ขอบมน, มีไอคอน FontAwesome)
- **Dialog & Confirm**: ใช้ `DevExpress.ui.dialog.custom` หรือ helper กลางของโปรเจ็กต์ เช่น `showAdminConfirmDialog(...)` สำหรับกล่องยืนยันและข้อความสำคัญ โดยคงสไตล์ปุ่มให้เป็นสีธีมของหน้า และหลีกเลี่ยง `DevExpress.ui.dialog.confirm(...)` ตรงๆ ในหน้าใหม่หรือโค้ดที่ปรับปรุง
- **Edit Dialogs**: ถ้าเป็นฟอร์มแก้ไขขนาดเล็กถึงกลางในหน้า Admin Detail ให้ prefer `DevExpress.ui.dialog.custom` + `dxForm` + `dxButton` มากกว่าแยกไปหน้าใหม่หรือใช้ popup แบบเก่าที่ไม่จำเป็น
- **Empty States**: หากไม่มีข้อมูลให้แสดงไอคอนขนาดใหญ่สีเทาจาง พร้อมข้อความที่เป็นมิตรตรงกลางพื้นที่ว่างเสมอ

### 3.1 Section Identity Icons
- ให้กำหนด **ไอคอนประจำตัว** ของแต่ละส่วนงานให้คงที่ทั้งในเมนู, page header, quick actions, empty state, summary card, dialog title และปุ่มหลัก เพื่อให้ผู้ใช้จดจำส่วนงานได้ทันที
- ใช้ Font Awesome แบบ `fas` เป็นค่าเริ่มต้น และหลีกเลี่ยงการเปลี่ยนไอคอนของโมดูลเดียวกันไปมาระหว่างหน้าต่างๆ ถ้าไม่จำเป็น
- Mapping มาตรฐานของ Admin UI:
   - `Dashboard` -> `fas fa-chart-pie`
   - `Courses` / `Course Management` -> `fas fa-book`
   - `Assignments` -> `fas fa-tasks`
   - `Students` -> `fas fa-user-graduate`
   - `Student Groups` -> `fas fa-users`
   - `Resources` -> `fas fa-folder-open`
   - `Categories` -> `fas fa-layer-group`
   - `Course Types` -> `fas fa-tag`
   - `Divisions` -> `fas fa-building`
   - `Users` -> `fas fa-user`
   - `Roles` -> `fas fa-shield-halved`
   - `System Config` -> `fas fa-cog`
   - `Reports` / printable summary -> `fas fa-file-alt`
   - `Learning Logs` / activity history -> `fas fa-clock-rotate-left`
- Action icon mapping มาตรฐาน:
   - `Add/Create` -> `fas fa-plus` หรือ `fas fa-plus-circle`
   - `Add User/Member/Student` -> `fas fa-user-plus`
   - `Edit` -> `fas fa-edit`
   - `Delete` -> `fas fa-trash-alt`
   - `View Detail / Open` -> `fas fa-eye` หรือ `fas fa-external-link-alt`
   - `Assign` -> `fas fa-book-open`
   - `Activate` -> `fas fa-check-circle`
   - `Deactivate` / `Pause` -> `fas fa-pause-circle`
   - `Refresh / Retry / Reassign / Reset` -> `fas fa-rotate-right` หรือ `fas fa-redo-alt`
   - `Filter` -> `fas fa-filter`
   - `Save` -> `fas fa-save`
   - `Export Excel` -> `fas fa-file-excel`
   - `Print` -> `fas fa-print`
- Loading icon มาตรฐานทุกส่วนงาน: `fas fa-spinner fa-spin`
- ถ้าจำเป็นต้องใช้ไอคอนอื่น ให้เลือกตัวที่มีความหมายใกล้เคียงที่สุดกับ mapping นี้ และพยายามคงให้สม่ำเสมอในทุก view ของโมดูลเดียวกัน

### 4. Performance & Loading States (UX)
- **Skeleton Loaders (iLearn.User)**: ในระหว่างรอข้อมูลจาก API (เช่น โหลดรายการคอร์สเรียน, แดชบอร์ด) ให้แสดงโครงร่างกล่องเทาๆ (Skeleton) ที่มีเอฟเฟกต์ Shimmer กระพริบนุ่มๆ แทนการใช้ Spinner หมุนๆ กลางจอ
- **Button Loading State**: เมื่อคลิกปุ่มบันทึก/Submit ต้อง Disable ปุ่มเสมอ พร้อมกับเปลี่ยนข้อความและแสดง Spinner (เช่น `<i class="fas fa-spinner fa-spin"></i> กำลังประมวลผล...`) เพื่อป้องกันผู้ใช้กดเบิ้ล
- **Action Loading State**: ในหน้า Admin Detail ถ้า action เป็น `quick-action`, เมื่อเริ่ม request ให้ Disable action นั้นทันที, เปลี่ยน icon เป็น spinner, และเปลี่ยนข้อความเป็นสถานะปัจจุบัน เช่น `Saving...`, `Deleting...`, `Activating...`, `Adding...`
- **Popup/Dialog Submit State**: ปุ่มยืนยันใน `dxPopup` หรือ `DevExpress.ui.dialog.custom` ต้องมี loading state เช่นเดียวกัน และถ้ากำลัง submit ห้ามปิด dialog/popup จาก outside click จนกว่า request จะสำเร็จหรือเกิด error
- **Lazy Loading**: รูปภาพประกอบคอร์สเรียน (Thumbnails) และรูปภาพขนาดใหญ่ ต้องใส่ Attribute `loading="lazy"` ไว้เสมอ
- **DataGrid Loading**: ใน DevExtreme DataGrid ให้ใช้ฟีเจอร์ Loading Panel มาตรฐาน ไม่ต้องทำ Skeleton

### 5. DevExtreme Implementation Rules
- โค้ด JavaScript เฉพาะหน้าต้องอยู่ใน `@section Scripts { }` เสมอ
- **DataStore**: สร้างผ่านฟังก์ชัน `createDataStore(baseUrl, controllerName, options)`
- **DataGrid Defaults**: เริ่มต้น DataGrid ด้วย `initDxGrid(selector, options)` ซึ่งตั้งค่า Default ไว้ให้รองรับกรอบ, สลับสีแถว, และ **เปิดฟีเจอร์ Export อัตโนมัติ**
- **Grid Height Rule**: ปัจจุบัน `initDxGrid` **ไม่คำนวณ auto-height จาก viewport ให้อีกแล้ว**; ถ้าหน้าใดต้องการความสูงเฉพาะ ต้องกำหนด `height` เองใน `options` หรือควบคุมผ่าน container CSS ของหน้านั้นอย่างชัดเจน
- **DataGrid Exporting**: ใช้ `handleExporting(e, fileName)` ร่วมกับ `ExcelJS` (ถูกฝังอยู่ใน Default แล้ว หากต้องการแก้ชื่อไฟล์ให้ Override `onExporting` ในหน้านั้นๆ)
- **PivotGrid Exporting**: ใช้ `handlePivotExporting(e, fileName)` สำหรับหน้า Report หรือตารางสรุปผลแบบ Pivot
- **Shared Formatting Helpers**: ในฝั่ง Admin ให้ใช้ helper กลางจาก `iLearn.Admin/wwwroot/js/admin-layout.js` เช่น `formatAdminDate`, `formatAdminDateTime`, `formatAdminPercentage`, `formatAdminCountLabel`, `formatAdminFileSize`, `formatAdminDuration` แทนการประกอบ string หรือ format วันที่/ตัวเลขแยกในแต่ละหน้า
- **Popup vs Dialog**: งานเลือกข้อมูลหรือจัดการรายการจำนวนมากยังใช้ `dxPopup` ได้ตามปกติ แต่ dialog สำหรับยืนยัน/แก้ไขข้อมูลสั้นๆ ควรใช้ `DevExpress.ui.dialog.custom` เพื่อให้ UX และสไตล์สม่ำเสมอ
- **DataGrid Performance Defaults** (ตั้งค่าใน `admin-layout.js` แล้ว ห้ามเปลี่ยน):
   - `remoteOperations: true` — ใช้ Server-side paging/sorting/filtering เสมอ
   - `scrolling.mode: 'virtual'` + `rowRenderingMode: 'virtual'` — ใช้ Virtual Scrolling แทน Pager
   - `paging.pageSize: 30` — โหลดครั้งละ 30 rows
   - `selectAllMode: 'page'` — บน `selectionGrid` preset ให้ Select All เฉพาะหน้าปัจจุบัน (ป้องกัน full-data fetch)
- **Selection Grids in Wizards**:
   - ใช้ `preset: "selectionGrid"` และปิด `headerFilter: { visible: false }` เพื่อป้องกัน DevExtreme ดึงข้อมูลทั้งหมดสำหรับ dropdown filter
   - **ห้ามใช้ virtual scrolling** ใน wizard — ให้ใช้ standard paging แทน เพื่อป้องกันปัญหา Select All ดึงข้อมูลทั้งหมดและ viewport miscalculation:
     ```js
     scrolling: { mode: 'standard' },
     paging: { enabled: true, pageSize: 15 },
     pager: { visible: true, showPageSizeSelector: false, showInfo: true, showNavigationButtons: true }
     ```
   - ต้อง override `selection: { selectAllMode: 'allPages' }` เพื่อให้ Select All เลือกข้อมูลทุกหน้าที่ filter ไว้ (ไม่ใช่แค่หน้าปัจจุบัน)
   - **ต้องกำหนดความสูงเองอย่างชัดเจน** สำหรับ grid ใน wizard เช่น `height: STUDENT_GRID_HEIGHT` หรือกำหนดผ่าน container CSS ที่หน้าเป็นคนควบคุม; หลีกเลี่ยง `height: '100%'` หาก parent ไม่มี explicit height จริง
   - ถ้า grid อยู่ใน wizard step ที่ยัง `display: none` ขณะหน้าโหลด ต้อง **lazy-init** grid เมื่อ step นั้นแสดงครั้งแรก เพื่อไม่ให้โหลดข้อมูลก่อนที่ผู้ใช้จะเปิด step
   - หลัง init grid ใน step ที่เพิ่งแสดง ต้องเรียก `refreshGridInstance()` (`repaint` + `updateDimensions`) เสมอ

### 6. Wizard Page Pattern (Admin)
- สำหรับหน้าที่เป็น flow หลายขั้นตอน เช่น `Add Members`, `Bulk Course Assignments`, หรือ flow ที่ต้องเลือกข้อมูลจำนวนมาก ให้ใช้ **wizard layout แบบเต็มหน้า** เป็นมาตรฐาน แทนการยัดทุกอย่างไว้ใน popup
- โครงสร้างมาตรฐาน:
   - ด้านบนใช้ `.page-header` + subtitle
   - ใต้ header ใช้ step cards เรียงแนวนอน (`grid`) พร้อมสถานะ `active` และ `complete`
   - เนื้อหาหลักใช้ layout `row` แบ่งเป็น **snapshot sidebar ซ้าย** และ **main card ขวา** เมื่อข้อมูลมีหลายมิติหรือผู้ใช้ต้องติดตาม context ระหว่างทำรายการ
   - ปุ่ม action หลักให้อยู่ใน `bottom-toolbar` เสมอ เช่น `Previous`, `Continue`, `Review`, `Confirm`
- **Snapshot Sidebar**:
   - ใช้สำหรับสรุป context ปัจจุบัน เช่น ชื่อรายการ, ช่วงเวลา, จำนวนที่เลือก, target mode, target name
   - ใช้รูปแบบ meta item แบบ label/value ที่อ่านเร็ว และอัปเดตแบบ real-time เมื่อผู้ใช้เปลี่ยนค่าใน form หรือ grid
- **Wizard Steps**:
   - Step title ใช้ตัวพิมพ์ใหญ่สั้นๆ และมีคำอธิบาย 1 บรรทัดใต้หัวข้อ
   - จำนวน step ควรชัดเจนตั้งแต่ต้น และไม่เปลี่ยนลำดับไปมาระหว่าง iteration ถ้าไม่จำเป็น
   - ถ้า step ใดมีข้อมูลน้อย เช่น criteria หรือ review ให้ card สูงเท่าที่จำเป็น (`fit-content`) ไม่ต้องฝืนยืดเต็มความสูง
- **Selection Step Layout**:
   - ถ้าเป็น step เลือกข้อมูล ให้ใช้ซ้ายเป็น filter panel ขวาเป็น grid เป็นค่าเริ่มต้น
   - Grid height ต้องถูกควบคุมแบบ explicit โดยหน้าปัจจุบัน เช่นใช้ค่าคงที่ `height` ใน `initDxGrid(...)` หรือ host container ที่มีความสูงชัดเจน; ถ้าต้อง responsive สามารถใช้ container ที่มี `clamp(...)` หรือค่าคงที่แยกตาม use case ได้
   - ถ้า grid ถูก initialize ขณะ panel ซ่อนอยู่ ต้อง refresh `updateDimensions()` เมื่อ step ถูกแสดง เพื่อป้องกันความสูงเพี้ยน
   - ใช้ inline selection เป็นค่าเริ่มต้นก่อน popup; popup ใช้เฉพาะกรณีที่ task นั้นต้องการ isolated workflow จริงๆ
- **Review Step**:
   - แสดง summary cards ด้านบนก่อน แล้วค่อยตามด้วย detail blocks / conflict lists / impact tables
   - ระยะห่างก่อน review content ให้คุมจาก shared utility class กลางบน header เช่น `.admin-review-header` แทนการใส่ `mt-*` ซ้ำที่ content block แรกของแต่ละหน้า
   - ถ้ามีหลาย block ใน review ให้ห่อด้วย shared stack utility เช่น `.admin-review-stack` แทนการใส่ `mb-*` ไล่ทีละ block
   - ถ้าไม่มี conflict หรือข้อมูลว่าง ให้มี empty state หรือ success state ที่ชัดเจน ไม่ปล่อยพื้นที่ว่างโล่ง
- **Shared Review Layout Rule**:
   - ใน wizard ของฝั่ง Admin เมื่อเข้า step review ให้ใช้ shared layout classes เช่น `.admin-review-flow`, `.admin-review-layout-row`, `.admin-review-sidebar-col`, `.admin-review-main-col` เพื่อซ่อน snapshot sidebar และจัด main content ให้อยู่กึ่งกลางในความกว้างแบบ container
   - ใช้ shared utility class สำหรับ spacing ของ review header และ review content blocks เพื่อให้ทุกหน้าใช้ pattern เดียวกัน
- **Responsiveness**:
   - บน mobile/tablet step cards และ summary grids ควร collapse เป็น 1 คอลัมน์
   - ต้องเผื่อ `padding-bottom` ให้มากพอสำหรับ `bottom-toolbar` ทุกครั้ง เพื่อไม่ให้ content ถูกบัง
- **Consistency Rule**:
   - ถ้ามี wizard ใหม่ในฝั่ง Admin ให้ยึด `iLearn.Admin/Views/StudentGroups/AddMembers.cshtml` เป็น visual baseline ก่อน แล้วค่อยปรับเฉพาะส่วนที่จำเป็นตาม use case



---
description: 'Answer questions about DevExpress UI Components and their API using the dxdocs server'
---

You are a .NET/JavaScript programmer and DevExpress product expert.

Your task is to answer questions about DevExpress components and their APIs using dxdocs MCP server tools.

When replying to **ANY** question about DevExpress components, use the dxdocs server to construct your answer.

## Workflow:

1. **Call devexpress_docs_search** to obtain help topics related to the user's question
2. **Call devexpress_docs_get_content** to fetch and read the most relevant help topics
3. **Reflect on the obtained content** and how it relates to the question
4. **Provide a comprehensive answer** based solely on retrieved information

## Constraints:

- **Use devexpress_docs_search only once** per question to avoid redundant queries
- **Answer questions based solely** on information obtained from MCP server tools
- If relevant code examples are available in documentation, **include those code examples**
- **Reference specific DevExpress controls and properties** mentioned in the docs
- If a user specifies a version (such as v24.2 or 24.2), invoke MCP server tools corresponding to that version (for example, "dxdocs24_2")