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
*   แถบ Live Activity Feed ที่อัปเดตแบบเรียลไทม์จะมีไฟสถานะสีเขียวแบบเรืองแสงและกระพริบได้ (`.neon-glow-dot`) เพื่อสื่อสารถึงความเคลื่อนไหวล่าสุด

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
*   ฟอร์มการทำขั้นตอนต่าง ๆ (Wizards) จะควบคุมขนาดและโครงสร้างให้มีมาตรฐานสอดคล้องกันผ่านคลาส CSS:
    *   `.wiz-section`: ขีดเส้นใต้ระบุหัวข้อขั้นตอน
    *   `.wiz-section-title`: ขนาด `1rem` (16px) ตัวหนา Slate เข้ม
    *   `.wiz-label`: ตัวหนา (semibold 600) ขนาด `0.75rem` (12px) ตัวพิมพ์ใหญ่ สี Slate ปานกลาง ระบุชื่อฟิลด์
    *   `.wiz-input`: ปรับปรุง Padding เป็น `0.45rem 0.75rem` (ความสูงรวม 36px) ขนาดฟอนต์ช่องกรอก `0.875rem` (14px) มีกรอบอินพุตมนสีขาว เมื่อโฟกัสจะเปลี่ยนกรอบเป็นสี Indigo
    *   ระยะขอบหน้าจอคอนเทนต์หลัก: ปรับเป็น `p-4 px-5 pb-5`

### 2.4 หน้าต่างแจ้งเตือนและส่วนลอยตัวเลือก (Backdrop-Blurred Modals & Floating Badges)
*   ยกเลิกการทำ Side Panel (แผงข้างเลื่อนออก) มาเป็นกล่องแจ้งเตือนตรงกลาง (Centered Modals) พร้อมเอฟเฟกต์เบลอหลัง (`backdrop-blur-xs` และแอนิเมชัน `.modal-window` ขยายตัวแบบ `scale-in`)
*   ใช้ระบบปุ่มลอยระบุจำนวนข้อมูลที่เลือกชั่วคราว (`.selected-floating-badge`) ซึ่งมีวงแหวนกระพริบแบบชีพจร (`badge-pulse`) เพื่อชี้นำผู้ใช้ในการดำเนินการขั้นถัดไป (เช่น การดึงสมาชิกเข้ากลุ่มเรียน)

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
*   **แนวทางการเปลี่ยนผ่านสู่ React:** [REACT-ADMIN-REBUILD-PLAN.md](file:///c:/Users/n4734/source/repos/iLearn2/DOC/REACT-ADMIN-REBUILD-PLAN.md)
*   **ตารางความต่างและการพอร์ตโมดูล:** [REACT-ADMIN-GAP-ANALYSIS.md](file:///c:/Users/n4734/source/repos/iLearn2/DOC/REACT-ADMIN-GAP-ANALYSIS.md)
*   **โครงสร้างหน้าตารางหลัก:** [AppTable.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/AppTable.tsx)
