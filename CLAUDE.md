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

- ใช้ shared components ใน `src/components/ui` เสมอ: `LoadingState`, `NotFoundState`, `StatusBadge`, `StatusText`, `SectionHeader`, `ProgressBar`, `ControlsSidebar`/`ControlAction`, `useConfirm` (ห้าม `window.confirm`), `AppButton`, `AppTable`
- เนื้อหาหลักทุกหน้าต้องอยู่ในการ์ด `rounded-lg border border-slate-200 bg-white`
- วันที่ format ผ่าน `formatDate`/`formatDateTime` จาก `src/lib/format.ts` เท่านั้น
- **API Contract Sync:** ทุก response type ต้องลอกจาก C# DTO/controller จริง พร้อมคอมเมนต์ `// Mirrors <DtoName> (<path>)` — แก้ DTO ฝั่ง backend ต้อง grep endpoint ใน `src/` แล้วแก้ type ฝั่ง React ในงานเดียวกัน
- **Route remount:** route detail/editor ทุกตัวใน `App.tsx` ต้องครอบ `<Remount>` (กัน state ค้างข้าม route เพราะ React reuse component instance) — list page ใช้ `key={config.controller}` บน `AppTable`
- Learners rows เป็น **camelCase** (`nid`, `eId`) — backend deserialize เป็น typed DTO แล้ว

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
