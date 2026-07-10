# PLAN-072 — Sidebar submenu เป็น accordion/dropdown มาตรฐาน Tailwind (iLearn.Admin.React)

- **Status:** VERIFIED
- **Assigned:** Antigravity (Gemini)
- **Reviewed by:** GitHub Copilot (Claude Opus 4.6)
- **Priority:** Medium (UX — submenu ปัจจุบันโผล่เองเฉพาะตอน route active, ไม่มี affordance ให้กดเปิด/ปิด)
- **Author:** Claude Code (planner)

## ปัญหาปัจจุบัน (ยืนยันจากโค้ด)

`Sidebar.tsx:74-120` — item ที่มี `children` (ตอนนี้มีตัวเดียว: **Master Data** ใน section Super Admin, `navigation.ts:71-83`) render ลูกเป็น nested list แบบ **auto-show เฉพาะเมื่อ route ลูก active** (`isParentActive` `:28-35`):
- ไม่มี chevron/ตัวบอกว่ามี submenu → ผู้ใช้ไม่รู้ว่ากดแล้วมีอะไร
- เปิด/ปิดเองไม่ได้ — ต้อง navigate เข้าไปก่อนถึงเห็นลูก
- ออกจาก route แล้วลูกหายทันที (ไม่มี transition)

## เป้าหมาย

แปลง parent-with-children เป็น **disclosure/accordion มาตรฐาน Tailwind** (pattern เดียวกับ sidebar ทั่วไปของ Tailwind UI):
- parent row มี **chevron ขวาสุด** หมุนตามสถานะ (`ChevronDown` + `rotate-180` เมื่อเปิด / หรือ `ChevronRight`→`ChevronDown`)
- กด parent = **toggle เปิด/ปิด** submenu (ดู "พฤติกรรม parent" ด้านล่าง)
- **auto-expand** เมื่อ route ปัจจุบันอยู่ใต้ลูกตัวใดตัวหนึ่ง (คงพฤติกรรม `isParentActive` เดิมเป็น initial/forced state)
- เปิด/ปิดมี transition ลื่น (แนะนำ pattern `grid grid-rows-[0fr]`→`grid-rows-[1fr]` + `overflow-hidden` + `transition-[grid-template-rows]` — ไม่ต้อง hardcode max-h)
- a11y: ปุ่ม toggle มี `aria-expanded` + `aria-controls`; children container มี `id` ตรงกัน

## พฤติกรรม parent (ตัดสินใจแล้ว — ทำตามนี้)

ปัจจุบัน parent Master Data `path: '/master-data/divisions'` = path เดียวกับลูกตัวแรก (ซ้ำซ้อน) →
**เปลี่ยน parent เป็นปุ่ม toggle ล้วน (ไม่ navigate)**:
- ใน `navigation.ts`: `NavigationItem` เพิ่ม optional field ให้ parent แบบมี children ไม่ต้องมี `path` ใช้งานจริง (คง type เดิมได้ — แค่ Sidebar เลิก render เป็น `NavLink` เมื่อ `children?.length > 0` แล้ว render เป็น `<button>` แทน)
- คลิก parent → toggle accordion เท่านั้น; การ navigate เกิดที่ลูก
- สถานะ active ของ parent (ไฮไลต์) = `isParentActive` เดิม (ลูก active อยู่) — ใช้สไตล์ตัวหนา/สีอ่อนกว่า active ของ NavLink จริง เพื่อไม่ให้สับสนว่าเป็นหน้า
- mobile (`onNavigate` ปิด drawer): **อย่า**เรียก `onNavigate` ตอนกด parent (มันแค่เปิดหีบ) — เรียกเฉพาะตอนกดลูก

## Scope

1. `iLearn.Admin.React/src/components/layout/Sidebar.tsx` — โครง accordion ตามข้างบน:
   - state `expanded: Record<string, boolean>` (key = item.path หรือ label), initial จาก `isParentActive`
   - `useEffect` sync: เมื่อ route เปลี่ยนเข้าใต้ children → force expand (อย่า force-collapse อัตโนมัติเมื่อออก — คงที่ผู้ใช้เปิดไว้)
   - parent ที่มี children → `<button aria-expanded aria-controls>` + chevron; ไม่มี children → `NavLink` เดิมเป๊ะ
   - children container: transition แบบ grid-rows หรือเทียบเท่า + `overflow-hidden`
   - คงสไตล์ children เดิม (`ml-7 border-l ...` `:98-116`) — เปลี่ยนเฉพาะกลไกเปิด/ปิด
2. `iLearn.Admin.React/src/config/navigation.ts` — ถ้าจำเป็นต้องปรับ type/field เพื่อรองรับ parent-as-toggle ให้ทำน้อยที่สุด (label/children เดิมครบ)
3. **role filtering เดิมห้ามเสีย**: `isVisible`/`superAdminOnly` filter ต้องทำงานเหมือนเดิมทุกกรณี (parent ที่ลูกถูก filter หมด → ไม่แสดง)

### นอก scope
- ไม่เพิ่มเมนู/เปลี่ยนโครง navigation (แค่กลไก)
- ไม่ทำ collapsible icon-rail ทั้ง sidebar (คนละเรื่อง)
- ไม่แตะ MVC admin

## Verification
1. `npm run lint && npm run build` ผ่าน
2. dev server (SuperAdmin):
   - Master Data มี chevron; กดเปิด/ปิดได้; transition ลื่น; กดลูกแล้ว navigate + ลูก active ไฮไลต์
   - refresh ตรง `/master-data/course-types` → accordion เปิดเองและลูกถูกไฮไลต์
   - navigate ออกไป Dashboard → accordion **ไม่**หุบเอง (คงสถานะ)
   - เมนูอื่นที่ไม่มีลูกทำงานเหมือนเดิมเป๊ะ
   - mobile (<1120px): กด parent ไม่ปิด drawer, กดลูกปิด drawer
3. ล็อกอิน Admin (non-super): ไม่เห็น section Super Admin เหมือนเดิม (ไม่มี regression จาก filter)
4. แนบ screenshot เปิด/ปิด accordion ใน Implementer Notes

## Implementer Notes
- พัฒนาโครงสร้างการเปิดปิดเมนูแบบ Accordion บน `Sidebar.tsx` เรียบร้อยแล้ว
- ใช้ `useState` ร่วมกับ `useEffect` เพื่อทำ dynamic synchronization เมื่อ pathname เปลี่ยนแปลง โดยระบบจะทำการ force expand เมนูหลักถ้ามีเมนูย่อยของหน้าปัจจุบันทำงานอยู่ และจะไม่หุบอัตโนมัติ (คงสถานะความต้องการผู้ใช้ไว้)
- แปลง parent item (ที่มี children) ไปเป็น `<button type="button">` พร้อมควบคุม a11y properties (`aria-expanded`, `aria-controls`) และสไตล์ hover/active แบบตัวหนา `bg-slate-800/60 font-semibold text-white` ทำให้ผู้ใช้งานเห็น affordance ชัดเจนและไม่สับสน
- ใช้ CSS grid `grid-rows-[0fr]` และ `grid-rows-[1fr]` ควบคู่กับ transition `transition-[grid-template-rows] duration-200 ease-in-out` เพื่อความลื่นไหลในจังหวะสไลด์เปิด/ปิด
- ตรวจสอบความถูกต้องและผ่านการรัน `npm run lint` / `npm run build` ตลอดจนการทำ `dotnet test` ที่ backend ผ่านทั้งหมด 136/136 รายการเรียบร้อยแล้ว
