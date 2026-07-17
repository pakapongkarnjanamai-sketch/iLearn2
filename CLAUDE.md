# iLearn2 — Agent Instructions

คำแนะนำสำหรับ AI agent ทุกตัวที่ทำงานใน repo นี้ (Claude Code, Antigravity/Gemini, อื่น ๆ)

## Multi-Agent Coordination (สำคัญที่สุด — อ่านก่อนแก้ไฟล์ใด ๆ)

repo นี้มี AI agent มากกว่าหนึ่งตัวทำงานคู่ขนานกัน **แบ่งบทบาทดังนี้:**

| Agent | บทบาท |
|---|---|
| **Claude Code** | Planner/Reviewer — วิเคราะห์ เขียนแผนใน `DOC/PLANS/`, รีวิวงานหลัง implement |
| **Antigravity (Gemini)** | Implementer — รับงานจาก `DOC/PLANS/` ที่ Assigned ให้ Gemini |
| **GitHub Copilot (GPT)** | Implementer — รับงานจาก `DOC/PLANS/` ที่ Assigned ให้ GPT |

กติกากลาง:

1. **ก่อนเริ่มงาน:** อ่าน `DOC/AGENT_LOG.md` (10 entry ล่าสุดพอ) เพื่อดูว่า agent อื่นเพิ่งแตะไฟล์ไหน/เปลี่ยน contract อะไร — ห้าม revert งานที่ agent อื่นเพิ่งทำโดยไม่มีเหตุผล ให้ reconcile แทน
2. **หลังจบงานที่มีการแก้โค้ด:** ต่อท้าย entry ใหม่ใน `DOC/AGENT_LOG.md` ตาม format ในไฟล์นั้น (ใหม่สุดอยู่บนสุด)
3. ถ้าพบว่าไฟล์ที่กำลังจะแก้เพิ่งถูกเปลี่ยนโดย agent อื่น (ดูจาก log หรือ git) ให้ตรวจ contract สองฝั่ง (API ↔ React types) ก่อนแก้เสมอ
4. **การแก้ไขแผนงาน:** เมื่อมีการสั่งแก้ไขหรือปรับปรุงคุณสมบัติเพิ่มเติมในแต่ละรอบการทำงาน ให้สร้างไฟล์แผนงานใหม่ (`PLAN-NNN-...`) ทุกครั้ง แทนการแก้ไขรายละเอียดในไฟล์แผนงานเดิม เพื่อเก็บบันทึกประวัติและป้องกันข้อมูลสับสน


กติกาเฉพาะ implementer (Gemini/GPT):

4. รับงานจากไฟล์แผนใน `DOC/PLANS/` ที่สถานะ `READY` และ Assigned ตรงกับตัวเอง — ทำตาม Scope ในแผน **ห้ามขยายขอบเขตเอง** ถ้าเจอปัญหานอกแผนให้จดลงท้ายไฟล์แผน (หัวข้อ Implementer Notes) แล้วทำงานเดิมต่อ
5. ทำเสร็จ: เปลี่ยนสถานะในไฟล์แผนเป็น `DONE` + เติม Implementer Notes (ทำอะไรต่างจากแผนบ้าง/เจออะไร) + รัน verification ตามที่แผนระบุ + ลง AGENT_LOG ตามปกติ

## โครงสร้างโปรเจค

- `iLearn.API` / `iLearn.Application` / `iLearn.Domain` / `iLearn.Infrastructure` — .NET 9 backend (Clean Architecture), Windows auth, API รันที่ `https://localhost:7128`
- `iLearn.Admin.React` — React 19 + Vite 8 + Tailwind 4 admin shell (dev ที่ `localhost:5173`) — **อ่าน `iLearn.Admin.React/README.md` ก่อนแก้ UI** มีกติกา UI Conventions + API Contract Sync ครบ
- `iLearn.Admin` — MVC admin เดิม (อย่าแก้เว้นแต่ถูกสั่ง), `iLearn.Tests` — xUnit

## กติกาสำคัญฝั่ง React (สรุปจาก README)

- ใช้ shared components ใน `src/components/ui` เสมอ: `Card`, `LoadingState`, `NotFoundState`, `Badge` (+wrapper `StatusBadge`/`StatusText`/`ReadinessBadge`), `SectionHeader`, `ListToolbar`, `ProgressBar`, `ControlsSidebar`/`ControlAction`, `useConfirm` (ห้าม `window.confirm`), `AppButton`/`IconButton`/`SegmentedToggle` (3 button primitives — ห้าม hand-roll `<button>` เอง; `AppButton` ปุ่มข้อความ variant/size + prop `loading`, `IconButton` ปุ่มไอคอนล้วน tone + `title` บังคับ, `SegmentedToggle` toggle/filter chips), `AppTable`
- เนื้อหาหลักทุกหน้าต้องอยู่ในการ์ด — ใช้ `<Card>` (อย่าเขียน `<section className="rounded-lg border border-slate-200 bg-white ...">` เอง)
- ป้ายสถานะ/ชนิด/ตัวเลขทั้งหมดใช้ `Badge` (tone × variant soft/outline/tag) — ห้าม hardcode `<span>` pill เอง
- วันที่/ตัวเลข format ผ่าน `src/lib/format.ts` เท่านั้น: `formatDate`/`formatDateTime`, `formatNumber`, `formatPercent`, `formatBytes` (ห้าม `toLocaleString`/`toFixed`/`Math.round(.../1024)` inline; ห้ามใส่ comma กับ ID/version/index)
- ตารางรายละเอียดยาว ๆ แบ่งหน้าด้วย `DETAIL_TABLE_CHUNK_SIZE` จาก `src/lib/tableStandards.ts` (Showing X of Y + Load more)
- **z-index ladder ของแอป** (ห้ามตั้งเลขมั่ว): content/sticky thead/sticky column = `z-10` · Header = `z-15` · sidebar overlay = `z-20` · Sidebar = `z-30` · modal = `z-50`/`z-60` · upload overlay = `z-9999` — **dropdown/popover ที่อยู่ใน container `sticky`/`fixed` ถูกจำกัดด้วย stacking context ของ container นั้น** (z ของลูกไม่มีผลข้าม context) ⇒ ต้องเทียบ z ของ **container** กับ content เสมอ ไม่ใช่แค่ของตัว dropdown (PLAN-089: bell dropdown ถูก grid ทับ เพราะ Header เป็น z-10 เท่ากับ card)
- **API Contract Sync:** ทุก response type ต้องลอกจาก C# DTO/controller จริง พร้อมคอมเมนต์ `// Mirrors <DtoName> (<path>)` — แก้ DTO ฝั่ง backend ต้อง grep endpoint ใน `src/` แล้วแก้ type ฝั่ง React ในงานเดียวกัน
- **Route remount:** route detail/editor ทุกตัวใน `App.tsx` ต้องครอบ `<Remount>` (กัน state ค้างข้าม route เพราะ React reuse component instance) — list page ใช้ `key={config.controller}` บน `AppTable`
- Learners rows เป็น **camelCase** (`nid`, `eId`) — backend deserialize เป็น typed DTO แล้ว

## กติกาสำคัญฝั่ง Backend (.NET) — ทุกข้อมาจาก bug จริงที่เคยหลุดรีวิว อย่าพลาดซ้ำ

- **เวลา:** ห้ามใช้ `DateTime.Now`/`DateTime.UtcNow` ดิบกับค่าที่เขียนลง DB — ใช้ `IDateTime.Now` เสมอ (`DateTimeService.Now = UtcNow.AddHours(7)` = เวลาไทย และ `SaveChanges` ก็เซ็ต `CreatedAt`/`CreatedBy` จากตัวนี้) — ผสมกันแล้วคอลัมน์ในแถวเดียวกันเพี้ยน 7 ชม. (PLAN-088: `ReadAt` น้อยกว่า `CreatedAt` = เหมือนอ่านก่อนถูกสร้าง)
- **วันครบกำหนดของ learner:** `ExtendDueDateAsync` อัปเดตแค่ `Assignment.DueDate` + `EnrollmentAssignment.DueDate` — **ไม่เคยแตะ `Enrollment.DueDate`** ⇒ รายงาน/สถานะทุกที่ต้องใช้ **effective dates** (มี active link → `Min(link.StartDate)`/`Max(link.DueDate)`, ไม่มี link → fallback คอลัมน์ enrollment) ตาม `GetEffectiveSchedule` (EnrollmentsController) หรือ `BuildVisibleEnrollmentRowsQuery` (ReportService) — อ่านคอลัมน์ดิบ = ตัวเลขขัดกับหน้า assignment (PLAN-086)
- **งานที่เขียนไฟล์ถาวรก่อน validate** (เช่น SCORM archive) ต้องลบไฟล์ + row ที่เพิ่งสร้างใน `catch` — ไม่งั้นไฟล์ค้างบน disk ทุกครั้งที่ upload พัง (PLAN-084: leak ได้ถึง 1GB/ครั้ง)
- **Side-effect เสริม** (notification / activity log) ต้องห่อ try/catch + log เอง **ห้ามให้มันพังแล้วทำ request หลักล้ม** และห้ามเปลี่ยน HTTP status/body เดิมของ endpoint ที่ไป hook
- **Migration:** ต้องอยู่ `iLearn.Infrastructure/Migrations/` + namespace `iLearn.Infrastructure.Migrations` เท่านั้น (ที่เดียวกับ `AppDbContextModelSnapshot`) — วางที่อื่น EF ยัง scan เจอ (ไม่พัง runtime) แต่ไฟล์จะกระจายคนละที่ (PLAN-088)
- **Deploy build ที่มี migration ใหม่ = ต้องรัน `dotnet ef database update --connection <env>` คู่กันเสมอ** (ไม่มี auto-migrate) — deploy PLAN-088 โดยไม่รัน migration ทำ endpoint ใหม่ 500 ทั้ง QA (PLAN-092)
- **Unique index บนตารางที่ soft-delete:** ต้องมี `.HasFilter("[IsDeleted] = 0")` เสมอ — ไม่งั้น row ที่ลบแล้ว (ซึ่ง query filter ฝั่งแอปมองไม่เห็น) จะบล็อกการสร้าง row ใหม่ค่าซ้ำ = duplicate key 500 (PLAN-092: add คอร์สที่เคยลบกลับเข้า batch; precedent เดิม: ScormRuntimeState)
- **SignalR per-user push:** `Clients.User(id)` ใช้ค่าจาก `NidUserIdProvider` ซึ่งต้อง normalize **ให้ตรงกับ `ICurrentUserService.UserId` เป๊ะ** (Nid ล้วน ไม่มี domain prefix) — ถ้าไม่ตรง push จะ**เงียบหายโดยไม่มี error** หาบั๊กยากมาก (PLAN-088)

## คำสั่งตรวจสอบ (รันก่อนปิดงานเสมอ)

```powershell
# Frontend (จากโฟลเดอร์ iLearn.Admin.React)
npm run lint
npm run build          # = tsc -b && vite build

# Backend — ถ้า API กำลังรันใน VS อยู่ bin จะถูกล็อก ให้ build ออก artifacts แทน
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

## หมายเหตุเฉพาะทาง

- `FileStorage.Data` เก็บ SCORM ZIP ทั้งก้อนเป็น `byte[]` ใน DB — **ห้าม Include/โหลด entity นี้ใน query รายการเด็ดขาด** ใช้ `ContentItem.CachedFileLength` แทน
- Controller ส่วนใหญ่คืน anonymous object (`Ok(new {...})`) — OpenAPI ใช้ generate type ไม่ได้ ต้องลอก shape ด้วยมือ
- ห้ามลบ `src/lib/es-toolkit-compat/*` และ `useSyncExternalStoreWithSelectorShim.ts` — ถูกใช้ผ่าน vite alias สำหรับ recharts
- Antigravity เก็บบันทึก session ไว้ที่ `C:\Users\n4734\.gemini\antigravity\brain\<guid>\` (เรียงตาม LastWriteTime) — Claude Code อ่านได้ถ้าต้องการ context เพิ่ม
