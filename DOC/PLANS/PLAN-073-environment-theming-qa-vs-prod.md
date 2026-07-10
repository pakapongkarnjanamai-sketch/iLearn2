# PLAN-073 — แยกโทนสี + ไอคอน QA vs PROD (iLearn.Admin.React + iLearn.Admin MVC)

- **Status:** READY
- **Assigned:** Antigravity (Gemini)
- **Priority:** Medium-High (กันปฏิบัติงานผิดเครื่อง — ผู้ใช้เพิ่งสลับ QA/PROD บ่อยช่วง cutover)
- **Author:** Claude Code (planner)
- **Context:** ผู้ใช้ต้องการให้ QA "สีตรงข้าม" กับ PROD + ไอคอน (favicon) ต่างกัน ทั้งสองแอป admin

## ข้อเท็จจริงสำคัญ (ยืนยันจากโค้ด/สคริปต์แล้ว)

- **QA และ PROD ใช้ build artifact เดียวกัน**: `tools/deploy-admin-react.ps1` (QA → `\\AP-NTC2138-QAWB\...`) และ `deploy-admin-react-prod.ps1` (PROD → `\\ap-ntc2137-prwb\...`) เรียก `build-admin-react-prod.ps1` ตัวเดียวกัน → `.env.production` ชุดเดียว
- ⇒ **ห้ามใช้ build-time env แยกสี** — ต้องเป็น **runtime detection จาก `window.location.hostname`** (ฝั่ง React) / `Request.Host` (ฝั่ง MVC) — ข้อดี: ไม่ต้องแตะ deploy pipeline เลย, artifact เดียว rollback ง่ายเหมือนเดิม
- โทนปัจจุบัน (PROD look): brand indigo `#4f46e5` (favicon.svg + brand tile ใน Sidebar `bg-indigo-600`), sidebar `bg-slate-900`, MVC ใช้ `.navbar-custom` (navbar-dark fixed-top) + `favicon.svg`

## หลักการออกแบบ

- **PROD = หน้าตาปัจจุบันเป๊ะ (indigo) — ไม่เปลี่ยนอะไร**
- **QA = โทนตรงข้าม: amber/orange** (คู่ตรงข้ามของ indigo บนวงล้อสี และเป็น convention สากลของ staging/QA)
- localhost/dev = ถือเป็น non-PROD → ใช้โทน QA แต่ป้ายเขียน `DEV` (กันสับสนเหมือนกัน)
- เปลี่ยนเฉพาะ **จุด brand/signal** ไม่ invert ทั้งแอป (invert ทั้งหน้าจะพังคอนทราสต์และรีวิวไม่ไหว): brand tile, ป้าย environment, แถบ accent, favicon, title

## Scope A — iLearn.Admin.React

1. **Environment detection ที่ `src/config/appConfig.ts`** (จุดเดียว ห้าม detect ซ้ำตามหน้า):
   ```ts
   // hostname: *qawb* → 'QA' | *prwb* → 'PROD' | localhost/127.* → 'DEV' | อื่น ๆ → 'QA' (fail-safe: ไม่ใช่ PROD ให้ถือว่าไม่ใช่ prod)
   environmentName: 'QA' | 'PROD' | 'DEV'
   isProd: boolean
   ```
   - เพิ่ม optional env override `VITE_ILEARN_ADMIN_ENVIRONMENT` (ถ้าตั้งมา ให้ชนะ hostname) — เผื่ออนาคต host เปลี่ยนชื่อ; อัปเดต `.env.example` ด้วย (ค่าว่าง = auto)
2. **Sidebar (`components/layout/Sidebar.tsx`)**:
   - brand tile `iL` (`:52` `bg-indigo-600`) → QA/DEV: `bg-amber-500 text-slate-900`
   - ใต้ชื่อแอป (แถว "Enterprise LMS") เพิ่ม `Badge` environment: QA = amber ("QA"), DEV = amber ("DEV"), PROD = ไม่แสดง
3. **Header (`components/layout/Header.tsx`)**: แถบ accent บาง ๆ `h-0.5 bg-amber-500` ใต้ header (หรือ border-b amber) เฉพาะ QA/DEV — ให้เห็นตลอดแม้ sidebar หุบ
4. **Favicon + title (runtime)**:
   - เพิ่ม `public/favicon-qa.svg` — กราฟิกเดิมแต่พื้น `#f59e0b` (amber-500) + ตัวอักษรเข้ม + มุมป้าย "QA" เล็ก
   - ใน `main.tsx` (หรือ helper ที่ appConfig เรียก): ถ้า non-PROD → สลับ `<link rel="icon">` href เป็น favicon-qa.svg + `document.title = 'iLearn Admin (QA)'` (DEV = `(DEV)`)
   - PROD: ไม่แตะอะไร (favicon/title เดิม)

## Scope B — iLearn.Admin (MVC)

5. **`Views/Shared/_DevExtremeLayout.cshtml`**:
   - ประกาศตัวแปรบนหัวไฟล์: `var isProd = Context.Request.Host.Host.Contains("prwb", StringComparison.OrdinalIgnoreCase);` (+ localhost → DEV label เช่นกัน)
   - non-PROD: เพิ่ม class `navbar-qa` ให้ `<nav class="navbar ... navbar-custom">` (`:61`) + ป้าย `QA`/`DEV` (badge เล็ก) ติดกับ navbar-brand (`:63`)
   - favicon (`:47`): non-PROD ชี้ `~/favicon-qa.svg`
6. **CSS**: เพิ่ม `.navbar-qa` override ในไฟล์ site css ที่ `.navbar-custom` อยู่ — โทน amber เข้ม (พื้น `#92400e`→`#b45309` หรือ amber-800/700 ให้ navbar-dark text ยังอ่านออก) — **หา selector `.navbar-custom` เดิมก่อนแล้ว override เฉพาะสีพื้น/เส้นขอบ**
7. **`wwwroot/favicon-qa.svg`**: ชุดเดียวกับฝั่ง React (copy)

## กติกาสำคัญ
- **PROD pixel-perfect เดิม**: ทุกการเปลี่ยนต้องอยู่หลังเงื่อนไข non-PROD เท่านั้น
- ไม่แตะ deploy scripts / .env.production (runtime detection ทั้งหมด)
- MVC เดิมห้ามรื้อโครง — เพิ่มเงื่อนไข + class + badge เท่านั้น (repo rule: อย่าแก้ iLearn.Admin เว้นแต่ถูกสั่ง — งานนี้คือถูกสั่ง จำกัดเฉพาะ layout/css/favicon)

## Verification
1. React: `npm run lint && npm run build` ผ่าน; MVC: `dotnet build iLearn.Admin` ผ่าน (ถ้า bin ล็อก build ออก artifacts ตาม CLAUDE.md)
2. Local dev (`localhost:5173`): เห็นโทน amber + ป้าย DEV + favicon QA + title `(DEV)`
3. จำลอง hostname: ทดสอบ logic ด้วย unit ของ detection function (ถ้าแยก pure function ได้) หรือทดสอบผ่าน `VITE_ILEARN_ADMIN_ENVIRONMENT=PROD` แล้วเห็นหน้าตาเดิมเป๊ะ (ไม่มี badge/amber ใด ๆ)
4. Deploy QA แล้วเปิด `ap-ntc2138-qawb/iLearn/admin-react` + `/iLearn/admin`: brand tile amber + ป้าย QA + favicon QA + navbar MVC โทน amber
5. Deploy PROD (หรือรอรอบ deploy ปกติ): เปิด `ap-ntc2137-prwb/...` → **เหมือนเดิมทุกจุด** (เกณฑ์สำคัญสุด)
6. แนบ screenshot QA vs PROD (หรือ local จำลอง) ใน Implementer Notes

## Implementer Notes
(เติมหลังทำเสร็จ)
