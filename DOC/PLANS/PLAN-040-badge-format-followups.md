# PLAN-040: Follow-up จากรีวิว PLAN-037/039 — percent precision + pill ที่ตกหล่น

- **Status:** READY
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** Low
- **Estimated scope:** แก้ formatPercent 1 จุด + migrate hand-rolled pill 2 จุดมาใช้ `Badge`

## Problem

จากรีวิว implementation ของ PLAN-037 (Badge) และ PLAN-039 (format) โดย Claude Code (2026-06-16) พบ 2 ประเด็นค้าง — minor ทั้งคู่ ไม่บล็อก VERIFIED แต่ควรเก็บให้จบ:

### 1. `formatPercent` ทำให้ทศนิยมหายใน Dashboard
- ของเดิม (local `formatPercent` ใน `DashboardPage`) เป็น **adaptive**: จำนวนเต็มแสดง 0 ตำแหน่ง, ค่าที่มีเศษแสดง 1 ตำแหน่ง (`87.5%`)
- shared `formatPercent` ใหม่ใน `src/lib/format.ts` default = 0 ตำแหน่งเสมอ → KPI ที่เคยเป็น `87.5%` กลายเป็น `88%`
- PLAN-039 ระบุว่า "คง behaviour เดิม" แต่ไม่ครบ → ต้องคืนความละเอียดทศนิยมที่จุดที่ต้องการ

### 2. ยังมี hand-rolled pill ที่ไม่ได้ย้ายมา `Badge`
PLAN-037 ระบุไว้แล้วว่า call site list "ไม่ exhaustive" — เหลือ 2 จุดที่เป็น pill จริงแต่ยัง hardcode:
- [AdminUsersPage.tsx:67](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx#L67) — `inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold ...` (badge สถานะ admin/role)
- [UserEditorPage.tsx:311](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserEditorPage.tsx#L311) — `inline-flex items-center rounded-full bg-rose-50 border border-rose-100 px-2.5 py-0.5 text-xs font-bold text-rose-700` (chip role ที่จะถูกถอด)

---

## Scope (ทำแค่นี้)

### 1. คืนความละเอียดทศนิยมของ percent ใน Dashboard
- ที่ [DashboardPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/DashboardPage.tsx) จุดที่แสดง KPI percent (เดิมใช้ adaptive) ให้เรียก `formatPercent(value, Number.isInteger(value) ? 0 : 1)` เพื่อคืนพฤติกรรมเดิม
- **อย่าแก้ default ของ `formatPercent` ใน format.ts** (0 ตำแหน่งเป็น default ที่ถูกแล้วสำหรับที่อื่น เช่น `CompletionBar`) — แก้เฉพาะ call site ที่ต้องการเศษ
- ตรวจ `CompletionBar` (เดิม `toFixed(0)`) ให้คงเป็น 0 ตำแหน่งเหมือนเดิม — ไม่ต้องแตะ

### 2. Migrate pill 2 จุดมาใช้ `Badge`
- `AdminUsersPage:67` — แทนด้วย `<Badge variant="outline" .../>` หรือ `soft` ตามที่ใกล้ของเดิมที่สุด เลือก tone ให้ตรงความหมายเดิม (ดูเงื่อนไข class เดิมว่าสีไหน = อะไร) — ยกเลิก `text-[10px]` → ใช้ `size="xxs"`
- `UserEditorPage:311` — chip สีแดง (จะถอด role) → `<Badge variant="outline" tone="danger" size="xxs">` (เลิก `rose-*` ตามมาตรฐานใหม่ที่ map เป็น `red`/`danger`)
- ถ้าจุดไหน semantics ไม่เข้ากับ tone มาตรฐานเป๊ะ ให้เลือกตัวที่ใกล้สุด แล้วจดใน Implementer Notes

### ขอบเขตที่ห้ามทำ
- ห้ามแตะ inline text ที่ไม่ใช่ pill (เช่น `text-rose-700` ที่เป็นสีตัวเลข overdue ใน Dashboard, mono code captions, sidebar labels) — สแกนเจอแต่ไม่ใช่ badge
- ห้ามเปลี่ยน default `formatPercent`
- ห้ามแตะ backend / `iLearn.Admin` (MVC)

---

## Verification
```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
# ไม่ควรเหลือ rounded-full pill ที่ hardcode ใน 2 หน้านี้
rg "rounded-full .*(text-\[10px\]|rose-)" src/pages/users
```
- เปิดด้วยตา: Dashboard (KPI percent ที่มีเศษต้องโชว์ 1 ตำแหน่งเหมือนเดิม), Admin users list, User editor (chip ถอด role)

## Implementer Notes
(เติมหลังทำ: tone ที่เลือกให้แต่ละ pill + จุดที่ตัดสินใจไม่แตะ)
