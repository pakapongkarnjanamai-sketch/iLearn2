# UX/UI & Architecture Analysis: iLearn.Admin.React

เอกสารนี้รวบรวมผลการวิเคราะห์โครงสร้าง เทคโนโลยี และแนวทางการออกแบบ UX/UI ของระบบ **iLearn.Admin.React** รวมถึงการเชื่อมโยงกับ API หลังบ้านของระบบ **iLearn** เพื่อเป็นแนวทางและเกณฑ์อ้างอิงสำหรับการพัฒนาและปรับปรุงระบบในอนาคต

---

## 1. ข้อมูลสถาปัตยกรรมทางเทคนิค (Technical Architecture Stack)

ระบบส่วนหน้า (Frontend) ถูกพัฒนาใหม่แยกส่วนออกมาจากระบบเดิมที่เป็น ASP.NET Core MVC/Razor เพื่อเปลี่ยนผ่านเข้าสู่ Single Page Application (SPA) เต็มรูปแบบ:

*   **แกนหลัก (Core):** Vite 8, React 19, TypeScript (Strict Mode)
*   **การจัดการสไตล์ (Styling):** Tailwind CSS v4 ผ่าน `@tailwindcss/vite` ร่วมกับ Custom CSS Variables
*   **สไตล์แบบอักษร (Typography):**
    *   **Inter (ภาษาอังกฤษ):** ฟอนต์สำหรับหน้าจอผู้ใช้งานโดยเฉพาะ ระยะช่องไฟลงตัว แยกตัวอักษร I-l ชัดเจน
    *   **Noto Sans Thai (ภาษาไทย):** ฟอนต์ไทยแนวทันสมัยไม่มีหัว (Loopless) ที่มีอัตราส่วนสเกลความสูงของตัวอักษรใหญ่สมดุลกับภาษาอังกฤษ (Inter) ป้องกันปัญหาภาษาไทยตัวเล็กเกินไป
    *   **Monospace:** กำหนดข้อยกเว้นบังคับสไตล์ Monospace (เช่น `Consolas`, `Menlo`, `monospace`) ให้กับคลาส `.font-mono` แท็ก `code` และ `pre` เพื่อใช้ในจุดที่เป็นรหัสพนักงาน รหัสหลักสูตร ค่าคอนฟิก และ URL ปลายทาง ป้องกัน bug จากการจัดสไตล์ทับเส้นทางแบบอักษรหลัก
*   **การนำทาง (Routing):** `react-router-dom` v7 โดยกำหนดสิทธิ์เข้าถึงของโมดูลในระดับ Route ผ่านตัวแผงควบคุมสิทธิ์ (RequireRole)
*   **กราฟและสถิติ (Analytics Charts):** Recharts แทนการใช้ DevExtreme Charts แบบเดิม พร้อมติดตั้ง `ChartErrorBoundary` ป้องกันชาร์ตล่มทั้งหน้าจอ
*   **ระบบ Real-time:** SignalR Client เชื่อมต่อเข้ากับ `AdminActivityHub` ของ API เพื่ออัปเดต Live Activity Feed ในหน้า Dashboard (พร้อมปิดกั้นการ Polling ซ้ำซ้อนเมื่อการเชื่อมต่อ Active)
*   **การเชื่อมต่อ API:** ใช้ระบบดึงข้อมูลแบบดั้งเดิมของเบราว์เซอร์ (`fetch`) ที่ตั้งค่า `credentials: 'include'` ร่วมกับ Windows Authentication

---

## 2. รูปแบบโครงสร้าง UX/UI (UX/UI Design Layout Patterns)

การออกแบบถูกกำหนดชื่อแนวทางไว้ในเชิง **Premium Enterprise Console (หรือ Premium Slate & Indigo Minimalist)** โดยยึดหลัก **Card-Free Control Hub** เพื่อลดความซับซ้อนของส่วนติดต่อผู้ใช้ และมีรูปแบบการตอบสนองหลัก 4 ประเภทหลัก:

### 2.1 หน้า Dashboard & Live Analytics
*   มุ่งเน้นการแสดงผล Dashboard ที่ดูดีเป็นพิเศษด้วย Recharts ที่ปรับขนาดตามหน้าจอได้ (Responsive)
*   แถบ Live Activity Feed ที่อัปเดตแบบเรียลไทม์จะมีไฟสถานะสีเขียวสื่อสารถึงความเคลื่อนไหวล่าสุด (custom class `.neon-glow-dot` ถูกลบใน PLAN-029 เพราะไม่ถูกใช้แล้ว)

### 2.2 หน้าตารางแสดงรายการข้อมูล (Compact Infinite-Scroll Directories)
*   ใช้คอมโพเนนต์ร่วม [AppTable.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/AppTable.tsx)
*   ใช้สไตล์การดึงข้อมูลแบบ **Infinite Scroll** (เลื่อนลงสุดตารางเพื่อโหลดหน้าใหม่ถัดไปทันที) แทน Pagination แบบปุ่มกดหน้าแบบเดิม
*   **เกณฑ์ความหนาแน่นตัวอักษรและขนาดตาราง (Compact Density):**
    *   ขนาดตัวหนังสือตาราง: `text-xxs / sm:text-[12px]`
    *   Padding เซลล์ตาราง: `py-2` (8px บน/ล่าง)
    *   ความสูงคำนวณแบ่งหน้า: `rowHeight = 38` (38px)
    *   การตัดพับข้อความ: กำหนด `whitespace-nowrap` บังคับคอลัมน์สำคัญ (เช่น รหัส, Assignment No. และวันเวลา) ให้อยู่ในบรรทัดเดียวเสมอ เพื่อป้องกันการตัดคำในจุดวิกฤต (ยกเว้น `description`, `name`, `title` ที่ตัดพับบรรทัดได้)
*   ตารางในหน้าโมดูลหลักจะเชื่อมโยงกับ Data Store ที่สร้างขึ้นมาโดยเฉพาะ:
    *   [createAdminDataSource.ts](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/lib/createDataSource.ts): สำหรับ Endpoint ที่รองรับ DevExtreme LoadOptions
    *   [createRestDataSource.ts](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/lib/createRestDataSource.ts): สำหรับ REST API ปลายทางปกติที่มี `/paged` และฟิลเตอร์เฉพาะ
*   ยกเลิกการแก้ไขข้อมูลแบบ Inline (ในช่องตาราง) หรือตารางแบบแก้ไขในตัว เพื่อความเป็นระเบียบและลดความผิดพลาดในการกรอกข้อมูล

### 2.3 หน้าต่างจัดการข้อมูลและฟอร์มแก้ไข (Dedicated Editor Sub-Pages & Compact Wizards)
*   เมื่อผู้ใช้ทำการดับเบิ้ลคลิกบนตาราง หรือกดปุ่มดำเนินการใด ๆ ระบบจะเปลี่ยนเส้นทางไปยังหน้าจัดการเฉพาะ (เช่น `/courses/:id` หรือ `/master-data/:type/:id`)
*   **โครงสร้างของเมนูด้านซ้าย (Sidebar) รูปแบบใหม่:**
    *   ความกว้าง Sidebar: **`210px`** เพื่อเพิ่มพื้นที่หน้าจอฝั่งตารางข้อมูลด้านขวา
    *   ขนาดฟอนต์เมนูด้านซ้าย: `text-[13.5px]`
    *   ความสูงปุ่มหัวข้อในเมนู: `min-h-[34px]` และไอคอนขนาด `16px` เพื่อความกะทัดรัด
*   ฟอร์มการทำขั้นตอนต่าง ๆ (Wizards) จะควบคุมขนาดช่องกรอกและป้ายกำกับให้มีมาตรฐานสอดคล้องกันผ่านคลาส CSS:
    *   `.wiz-label`: ตัวหนา (semibold 600) ขนาด `0.75rem` (12px) ตัวพิมพ์ใหญ่ สี Slate ปานกลาง ระบุชื่อฟิลด์
    *   `.wiz-input`: ปรับปรุง Padding เป็น `0.45rem 0.75rem` (ความสูงรวม 36px) ขนาดฟอนต์ช่องกรอก `0.875rem` (14px) มีกรอบอินพุตมนสีขาว เมื่อโฟกัสจะเปลี่ยนกรอบเป็นสี Indigo
    *   ระยะขอบหน้าจอคอนเทนต์หลัก: ปรับเป็น `p-4 px-5 pb-5`

### 2.4 หน้าแสดงรายละเอียดข้อมูล (Standardized Detail Pages)

หน้าประเภท Detail (`/courses/:id`, `/content-library/:id`, `/learner-groups/:id`, `/users/:id`, `/assignments/:id`, `/master-data/:type/:id`, `/learners/:id/profile`) ใช้โครงสร้างมาตรฐานเดียวกันทั้งหมด ผ่านชุดคอมโพเนนต์ร่วมใน `src/components/ui/detail/` (ดู PLAN-007/008):

*   **โครงหน้า (DetailLayout):** กริด 2 คอลัมน์ `lg:grid-cols-[minmax(0,1fr)_280px]` — เนื้อหาหลักซ้าย (`min-w-0`) + `ControlsSidebar` ขวากว้างคงที่ **280px** (ประกอบด้วย `ControlAction` รายการคำสั่งที่เป็นปุ่มดำเนินการเท่านั้น ห้ามนำข้อมูลรายละเอียด คุณสมบัติ หรือ Metadata อื่น ๆ มาใส่ในส่วนนี้ โดยให้ย้ายข้อมูลที่ไม่ใช่ปุ่มไปไว้ใน section เนื้อหาหลักฝั่งซ้าย)
*   **การ์ดเนื้อหา (DetailCard):** เนื้อหาแบ่งเป็น section การ์ด `rounded-lg border border-slate-200 bg-white p-5 space-y-5` เปิดหัวด้วย `SectionHeader`
*   **ตารางข้อเท็จจริง (FactGrid / Fact):** ข้อมูลแบบ label–value แสดงเป็น `dl` กริด 2–3 คอลัมน์ (`gap-x-6 gap-y-5 text-xs`) แต่ละช่องคือ `Fact`: `dt` label ตัวพิมพ์ใหญ่ (`text-slate-400 font-bold uppercase tracking-wider`) + `dd` ค่า (`mt-1`) — ค่าที่เป็นรหัส/path ใช้ `mono` (font-mono + wrap-break-word)
*   **หัวข้อย่อยในกลุ่มการ์ด (DetailSubSection):** คั่นกลุ่มข้อมูลด้วยเส้น `border-slate-100` + ป้ายหัวข้อจิ๋วตัวพิมพ์ใหญ่ ก่อนเข้าเนื้อหากลุ่มถัดไป
*   **ลำดับ section แบบเรียงหน้าเดียว (Stacked Sections):** หน้า Detail ที่มีหลายส่วนข้อมูล (เช่น Courses, Assignments, Learner Groups) ให้แสดงทุกส่วนเรียงลงมาในหน้าเดียวตามลำดับเดิม โดยใช้ `SectionHeader` คั่นแต่ละการ์ด และไม่ใช้แท็บสลับข้อมูลแล้ว ส่วน KPI/Metric ให้อยู่ใน section **Overview** แทน
*   **สถานะหน้า:** โหลด = `LoadingState`, ไม่พบ = `NotFoundState` (พร้อมลิงก์ย้อนกลับ), breadcrumb ตั้งชื่อรายการจริงผ่าน `useBreadcrumbs().setLabel(id, name)`
*   **ข้อห้าม:** ห้ามเขียนมาร์กอัปกริดสองคอลัมน์ / dt-dd fact เองในหน้าเพจอีก — ต้อง import จาก `src/components/ui/detail/` เท่านั้น (ยกเว้นเนื้อหาเฉพาะทางภายในการ์ด เช่น ตาราง members)

### 2.5 หน้าต่างแจ้งเตือน (Backdrop-Blurred Modals)
*   ยกเลิกการทำ Side Panel (แผงข้างเลื่อนออก) มาเป็นกล่องแจ้งเตือนตรงกลาง (Centered Modals) พร้อมเอฟเฟกต์เบลอหลัง (`backdrop-blur-xs` และแอนิเมชัน `.modal-window` ขยายตัวแบบ `scale-in`)
*   (หมายเหตุ: custom class `.selected-floating-badge` + keyframes `badge-pulse`/`badge-fade-slide-in` ถูกลบใน PLAN-029 เพราะไม่ถูกใช้งานแล้ว)

---

## 3. หลักการออกแบบที่ตกลงร่วมกัน (UI/UX Design Conventions)

ในเอกสารอ้างอิงของโครงการมีการระบุเกณฑ์การเขียน Code เพื่อควบคุมหน้าตาและการทำงานฝั่ง Frontend ไว้ดังนี้:

1.  **ห้ามคัดลอกมาร์กอัปซ้ำ (Shared Primitives):** เมื่อมีองค์ประกอบทางสายตาที่ปรากฏซ้ำเป็นครั้งที่สองในหน้าอื่น ให้แยกออกมาเป็นคอมโพเนนต์ย่อยใน `src/components/ui` เสมอ
2.  **การแสดงสถานะและการจัดรูปแบบข้อมูล:**
    *   **Loading:** ทุก ๆ Loading Spinner บนหน้าจอต้องถูกแสดงผลผ่าน `<LoadingState />` (เต็มหน้า) หรือ `<LoadingState size="section" />` (ภายในส่วนย่อย)
    *   **Badges:** ใช้ `StatusBadge` หรือ `StatusText` เพื่อจัดการสีป้ายตามข้อความสถานะโดยอัตโนมัติ (เช่น สีเขียวสำหรับความสำเร็จ, สีน้ำเงินสำหรับกำลังดำเนินงาน, สีแดงสำหรับงานค้าง/หมดอายุ)
    *   **Dates:** ห้ามเรียกใช้งานฟังก์ชันการจัดรูปแบบวันที่ของ JavaScript ตรง ๆ ในหน้าเพจ แต่ต้องเรียกผ่าน `formatDate` / `formatDateTime` จาก [format.ts](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/lib/format.ts) เท่านั้น
3.  **การยืนยันขั้นตอนสำคัญ (Destructive Actions):** การดำเนินการที่มีผลกระทบสูง (เช่น การลบข้อมูล การยกเลิกสิทธิ์) จะต้องยืนยันตัวตนผ่าน Dialog ยืนยันที่พัฒนาขึ้นมาเฉพาะ โดยเขียนเรียกผ่าน `await confirm({ title, message, danger })` ห้ามใช้ `window.confirm` แบบเดิม

---

## 4. แฟ้มข้อมูลอ้างอิงสำคัญในการพัฒนา (Key Code References)

*   **การตั้งค่าสไตล์หลัก:** [index.css](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/index.css)
*   **แนวทางการพัฒนา React Admin (ฉบับใช้งานปัจจุบัน):** [iLearn.Admin.React/README.md](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/README.md)
*   **โครงสร้างหน้าตารางหลัก:** [AppTable.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/AppTable.tsx)
