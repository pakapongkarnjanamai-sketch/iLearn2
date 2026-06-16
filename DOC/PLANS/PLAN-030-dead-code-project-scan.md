# PLAN-030: สแกน dead CSS/utility/component ทั้งโปรเจกต์ (นอก index.css) + ลบที่ปลอดภัย

- **Status:** VERIFIED ✅ (Claude ทำเอง + verify 2026-06-15)
- **Assigned:** Claude Code (ผู้ใช้สั่งให้ทำเอง)
- **Priority:** Low
- **Estimated scope:** ลบ 1 ไฟล์ (`PageHeader.tsx`)

## Problem / สแกน

ต่อจาก PLAN-029 (dead CSS ใน index.css) — สแกน dead code ที่เหลือทั้งโปรเจกต์ React (component, CSS variables/tokens, lib utility)

### ผลสแกน

**1. Dead component (ลบได้ปลอดภัย):**
- `src/components/ui/PageHeader.tsx` (13 บรรทัด) — export `PageHeader` แต่ **ไม่ถูก import ที่ไหนเลย** (grep ทั้ง src = 0, ไม่มี barrel ใน components/ui) → dead

**2. CSS design tokens (`--admin-*` ใน `@theme`) — ไม่ลบ (design decision):**
- token สี `--admin-brand`, `--admin-danger`, `--admin-sidebar`, `--admin-success`, `--admin-warning` ฯลฯ มี utility usage = 0 ใน tsx **แต่อยู่ใน `@theme {}`** ของ Tailwind v4 = เป็น **design palette tokens** ที่ generate utility (`bg-admin-brand` ฯลฯ)
- การลบ = ลด palette/utility ที่ใช้ได้ → เป็น **design decision ไม่ใช่ dead code ชัดเจน** จึง**ไม่รวมในงานนี้**
- token ที่ live แน่นอน: `--text-xxs` (utility `text-xxs` ใช้ 21 ไฟล์), `--animate-fade-in`/`scale-in` (ใช้ 7/5), และ `--admin-border/surface/text` (อ้างใน index.css เอง)

**3. lib utility — ไม่ลบ:**
- `useSyncExternalStoreWithSelector` (`useSyncExternalStoreWithSelectorShim.ts`) grep = 0 refs **แต่ CLAUDE.md ระบุห้ามลบ** (ใช้ผ่าน vite alias สำหรับ recharts — grep มองไม่เห็น) → คงไว้

## Scope (ทำแค่นี้)

ลบไฟล์ `iLearn.Admin.React/src/components/ui/PageHeader.tsx` (dead component) — เป็นรายการเดียวที่ลบได้ปลอดภัยจริง

## Out of scope (ห้ามแตะ)

- ห้ามลบ CSS `@theme` tokens (`--admin-*`) — design palette, เป็น decision แยก
- ห้ามแตะ `useSyncExternalStoreWithSelectorShim.ts` / `es-toolkit-compat/*` (CLAUDE.md ห้าม)
- ห้ามแตะ component อื่น (สแกนแล้วใช้ครบ มีแค่ PageHeader ที่ dead)

## Acceptance criteria

- [x] `PageHeader.tsx` ถูกลบ
- [x] grep `PageHeader` ทั้ง src = 0 (ไม่มี reference ค้าง)
- [x] `npm run build` ผ่าน, `npm run lint` 0/0

## Implementer Notes (Claude)

- ลบ `src/components/ui/PageHeader.tsx` — ยืนยัน 0 references, ไม่มี barrel re-export
- CSS `@theme` tokens ที่ utility usage = 0 (`--admin-brand` ฯลฯ) **ไม่ลบ** — เป็น design palette, ถ้าจะตัดต้องตัดสินใจระดับ design + audit ทีละ token (เสนอเป็น backlog ถ้าต้องการ)
- Verified: grep PageHeader = 0, `npm run build` ผ่าน, `npm run lint` 0/0
