# PLAN-131: Dashboard Redesign — Action-first + Thai Localization + UI Conventions

- **Status**: COMPLETED
- **Assigned**: Antigravity (Gemini)
- **Created**: 2026-07-23
- **Completed**: 2026-07-23

## Overview

Refactor หน้า Dashboard (`iLearn.Admin.React/src/pages/DashboardPage.tsx`) ตามการตัดสินใจของผู้ใช้:

1. **บุคลิกใหม่ = Action-first** — เน้น "วันนี้ต้องทำอะไร": งานเกินกำหนด/ใกล้ครบกำหนด และตารางรายการที่ตัดสินใจได้จริงขึ้นบนสุด ลดจำนวน chart ลง
2. **ภาษาไทยทั้งหน้า** — ตามมาตรฐานเดียวกับหน้า Reports ที่เพิ่ง localize ไปแล้ว
3. **ตัดทิ้ง 3 ส่วน** (ผู้ใช้ยืนยันแล้ว): Report Hub quick links (ซ้ำ sidebar), Category Mix chart, Task Status Pie + Legend
4. **แก้ให้ตรง UI Conventions** — หน้านี้เป็นหน้าเดียวที่ยัง hand-roll `<section className="rounded-lg border...">`, มี `SectionHeader`/`CompletionBar`/`relativeTime`/`formatDateShort` เวอร์ชันโลคัลซ้ำกับ shared components/`format.ts`

**Frontend-only** — ห้ามแตะ backend/DTO ใด ๆ (ดู Out of Scope)

## Scope of Changes

- `iLearn.Admin.React/src/pages/DashboardPage.tsx` — โครงหน้าใหม่ + localize + ใช้ shared components
- `iLearn.Admin.React/src/pages/dashboard/DashboardCharts.tsx` — ลบ `TaskStatusPie`, `TaskStatusLegend`, `CategoryMixChart` + localize empty label
- `iLearn.Admin.React/src/pages/dashboard/dashboardApi.ts` — **ห้ามลบ type ใด ๆ** (ดู §4)

## §1 โครงหน้าใหม่ (เรียงบน → ล่าง)

### 1.1 Header (คงโครงเดิม แปลไทย)

- Eyebrow `iLearn Admin` คงอังกฤษได้
- `Operational summary` → **"ภาพรวมการดำเนินงาน"**
- Scope: `All divisions` → **"ทุกสังกัด"**; `Generated {relativeTime}` → **"อัปเดต {formatRelativeTime(...)}"**
- ปุ่ม: `New Course` → **"สร้างคอร์สใหม่"** (secondary), `New Assignment` → **"มอบหมายงานใหม่"** (primary) — ปลายทาง navigate เดิม

### 1.2 Maintenance banner (คง logic เดิม แปลไทย)

- `Maintenance in progress` → **"กำลังดำเนินการปรับปรุงระบบ"** — polling 15 วิ เดิม ห้ามแก้ logic

### 1.3 KPI strip — 4 ช่อง เรียงตามความเร่งด่วน (action-first)

คง pattern แถบต่อกัน (grid-flow-col + border คั่น) แต่เปลี่ยน container เป็น `<Card bodyClassName="grid auto-cols-fr grid-flow-col">` (ไม่มี title) แทน section hand-roll เดิม:

| ลำดับ | ป้าย | ค่า | meta | ลิงก์ |
|---|---|---|---|---|
| 1 | **งานเกินกำหนด** (ตัวเลขโทน rose) | `kpi.overdueTasks` | จาก `kpi.totalLearningTasks` งานทั้งหมด | `/assignments` |
| 2 | **ใกล้ถึงกำหนด** (โทน amber) | `kpi.dueSoonTasks` | ภายใน 7 วัน | `/assignments` |
| 3 | **อัตราเรียนสำเร็จ** | `formatPercent(kpi.completionRate, ...)` | `kpi.assignedLearners` ผู้เรียน · `kpi.completedLearningTasks`/`kpi.totalLearningTasks` งาน | `/reports/courses` |
| 4 | **กิจกรรมการเรียน 30 วัน** | `kpi.learningSessionsLast30` | `DeltaTag` เทียบ 30 วันก่อนหน้า ("เพิ่มขึ้น/ลดลง/คงที่") | — |

- ตัด tile "Course Portfolio" ออก (ข้อมูลคอร์สมีในตาราง "คอร์สที่ต้องติดตาม" และหน้า `/courses` อยู่แล้ว) — ถ้าอยากคงจำนวนคอร์ส ให้ใส่เป็น meta บรรทัดเดียว ห้ามเพิ่ม tile ที่ 5
- `KpiTile`/`DeltaTag` คงเป็น component โลคัลได้ (เฉพาะหน้านี้) แต่ตัวเลข format ผ่าน `formatNumber`/`formatPercent` เท่านั้น

### 1.4 "งานมอบหมายที่ต้องจัดการ" — ตารางเต็มความกว้าง (ยกขึ้นเป็นส่วนหลัก)

- `<Card title="งานมอบหมายที่ต้องจัดการ" icon={ClipboardList} actions={<ลิงก์ "ดูทั้งหมด →" ไป /assignments>}>`
- ข้อมูล: `overview.priorityAssignments` (เดิม)
- คอลัมน์: **งานมอบหมาย / สถานะ / ผู้เรียน / กำหนดส่ง / ความคืบหน้า / (ลิงก์ "รายละเอียด →")**
- สถานะใช้ `StatusBadge` เดิม; ความคืบหน้าใช้ **shared `ProgressBar`** (ลบ `CompletionBar` โลคัล); วันที่ใช้ **`formatDate`** จาก `format.ts` (ลบ `formatDateShort` ที่ hand-roll `Intl.DateTimeFormat`)
- Empty state: **"ไม่มีงานมอบหมายที่ต้องจัดการในขณะนี้"**

### 1.5 "คอร์สที่ต้องติดตาม" — ตารางเต็มความกว้าง

- `<Card title="คอร์สที่ต้องติดตาม" icon={BookOpen}>` — ข้อมูล `overview.courseAttention` (เดิม)
- คอลัมน์: **คอร์ส / งานเรียน / เกินกำหนด / ความคืบหน้า** — ชื่อคอร์สลิงก์ไป `/courses/{id}` เดิม
- Empty state: **"ทุกคอร์สเป็นไปตามแผน"**

### 1.6 แถวล่าง (lg:grid-cols-3)

- ซ้าย (col-span-2): `<Card title="แนวโน้มกิจกรรมการเรียน" ...>` + subtitle/หมายเหตุ "6 เดือนล่าสุด" — `LearningActivityChart` เดิม ครอบ `ChartErrorBoundary` เดิม
- ขวา (1 คอลัมน์): `<Card title="กิจกรรมล่าสุดของผู้ดูแล">` — feed เดิม (SignalR + fallback polling 60 วิ — **ห้ามแก้ logic subscribe/interval**)
  - Badge มุมขวา: `Live` → **"เรียลไทม์"**, `Polling` → **"รีเฟรชอัตโนมัติ"**
  - เวลาใช้ `formatRelativeTime` จาก `format.ts` (ลบ `relativeTime` โลคัล — ฟังก์ชันกลางเป็นภาษาไทยอยู่แล้ว)
  - Empty state: **"ยังไม่มีกิจกรรมล่าสุด"**

### 1.7 Loading / Error states

- `LoadingState label="กำลังโหลดแดชบอร์ด..."`
- Error: "ไม่สามารถโหลดแดชบอร์ดได้" + ปุ่ม **"ลองใหม่"** (`AppButton` เดิม)

## §2 ส่วนที่ลบทิ้ง

1. Section "Report Hub" quick links ทั้งการ์ด + component `ReportLink` + icon import ที่ไม่ใช้แล้ว (`FolderTree`, `Database`, `CalendarRange`, `Globe2`?, `Users`, `FileBarChart` — ตาม lint)
2. `TaskStatusPie`, `TaskStatusLegend`, `CategoryMixChart` ใน `DashboardCharts.tsx` + import recharts ที่เหลือใช้ (`Pie`, `PieChart`, `Cell`) + `STATUS_COLORS` ถ้าไม่เหลือผู้ใช้
3. Component/helper โลคัลที่ซ้ำ shared: `SectionHeader` (ใช้ผ่าน `Card` title/`SectionHeader` กลาง), `CompletionBar` (→ `ProgressBar`), `formatDateShort` (→ `formatDate`), `relativeTime` (→ `formatRelativeTime`)

## §3 สิ่งที่ต้องคงไว้ (ห้ามแตะ)

- `ChartErrorBoundary` — คงไว้ครอบ chart ที่เหลือ (แปล fallback text เป็น "ไม่สามารถแสดงกราฟได้")
- SignalR: `subscribeHubEvent('AdminActivityCreated', ...)` ผ่าน `useNotifications()` (tech debt fix PLAN-131 — ห้ามเปิด connection ใหม่)
- Maintenance polling 15 วิ / activities fallback polling 60 วิ + `isSignalRConnectedRef` guard
- โครง `loadAll` + `Promise.all` เดิม

## §4 Out of Scope (สำคัญ)

- **ห้ามแตะ backend ทุกไฟล์** — `admin/Dashboard/Overview` ยังคืน `taskStatus`/`categoryMix` เหมือนเดิม ฝั่ง React แค่ไม่ render
- **ห้ามลบ type ใน `dashboardApi.ts`** (`TaskStatusPoint`, `CategoryMixPoint`, ฯลฯ) — เป็น mirror ของ C# DTO ตามกติกา API Contract Sync (ถ้า lint ฟ้อง unused ให้คง export ไว้ — exported type ไม่โดนฟ้อง)
- ไม่แตะ `App.tsx` / routing / `NotificationProvider`
- ตารางในหน้านี้**ไม่ใช้ `AppTable`** — `AppTable` ผูกกับ `AppClientStore` สำหรับ list page; ใช้ table เบา ๆ ใน `<Card>` แบบเดียวกับหน้า reports (คง helper `Th`/`Td` โลคัลได้)
- หน้านี้เป็นหน้า scroll ปกติ — **ไม่ต้อง**ทำ full-height layout / infinite scroll แบบหน้า reports (ข้อมูลมีจำกัดไม่เกิน ~10 แถวต่อตาราง)

## Verification

```powershell
cd iLearn.Admin.React
npm run lint    # ต้อง 0 errors
npm run build   # ต้อง 0 errors
```

Smoke (dev `localhost:5173` + API `https://localhost:7128`):
- เปิด `/` — KPI 4 ช่อง, ตาราง 2 ตาราง, chart 1 ตัว, activity feed แสดงครบ, console 0 errors
- Badge "เรียลไทม์" ขึ้นเมื่อ SignalR ต่อสำเร็จ
- คลิก KPI tile / ลิงก์ตาราง navigate ถูกปลายทาง

## Implementer Notes

- ดำเนินการ refactor `DashboardPage.tsx` และ `DashboardCharts.tsx` ตามข้อกำหนด PLAN-131 ครบถ้วน
- เปลี่ยน UI ให้ใช้ shared components (`Card`, `ProgressBar`, `StatusBadge`, `AppButton`, `Badge`, `LoadingState`) และ `lib/format.ts` (`formatDate`, `formatRelativeTime`, `formatNumber`, `formatPercent`)
- แปลข้อความภาษาไทยทุกจุดตามข้อกำหนด
- ตรวจสอบ `npm run lint` ผ่าน 0 errors และ `npm run build` ผ่าน 0 errors (built in 1.48s)

