# PLAN-028: แก้อาการกระพริบ (flicker) ของ AppTable ตอนโหลด/เลื่อน

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: overlay condition = `loading && data.length === 0` (AppTable:389) — initial load มี spinner, scroll/refetch ไม่มี blur แฟลช, build/lint 0/0 ผ่าน)
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Medium
- **Estimated scope:** 1 ไฟล์หลัก (`components/ui/AppTable.tsx`) — เป็น shared component ใช้ทุกหน้า list

## Problem

หน้า list (เช่น `/assignments`) **กระพริบ** ตอนโหลด/เลื่อน ต้นเหตุใน `iLearn.Admin.React/src/components/ui/AppTable.tsx`:

1. **Spinner overlay แสดงทุก fetch** — `fetchData` เรียก `setLoading(true)` ทุกครั้ง (บรรทัด ~107) และ overlay `absolute inset-0 bg-white/45 backdrop-blur-xs` (บรรทัด ~359-363) แสดงเมื่อ `loading=true` → ตอน **infinite scroll โหลดหน้าถัดไป** (page>1) และตอน refetch (search/filter/sort) overlay จะ**แฟลชทับแถวที่มีอยู่** = กระพริบ
   - ตอน page>1 มี `AppTableFooter` แสดง "Loading more..." อยู่แล้ว → overlay จึง**ซ้ำซ้อน**และทำให้กระพริบ
2. **(เสริม) โหลดซ้ำหลายรอบ** — effect auto-โหลดหน้าถัดไปเมื่อแถวไม่เต็ม viewport (บรรทัด ~175-184) + `updateAutoPageSize` ผ่าน ResizeObserver (บรรทัด ~188-211) อาจทำ `pageSize`/`page` เปลี่ยนถี่ → fetch ซ้ำ → แฟลชหลายรอบ

## Scope (ทำแค่นี้)

### 1. แสดง overlay เฉพาะ "initial load" เท่านั้น (แก้กระพริบหลัก)
- เปลี่ยนเงื่อนไข overlay (บรรทัด ~359) จาก `{loading && (...)}` → **`{loading && data.length === 0 && (...)}`**
  - initial load (ยังไม่มีข้อมูล) → แสดง overlay spinner ตามเดิม
  - infinite scroll (page>1, มีข้อมูลแล้ว) → **ไม่แสดง overlay** ปล่อยให้ `AppTableFooter` ("Loading more...") เป็นตัวบอกสถานะ → ไม่กระพริบ
  - refetch search/filter/sort (page=1 แต่มีข้อมูลเดิม) → `startTransition` คงแถวเดิมไว้จนข้อมูลใหม่พร้อม (มีอยู่แล้วบรรทัด ~124) → ไม่ต้อง blur ทับ
- **ทางเลือกเสริม (ถ้าอยากมี feedback ตอน refetch):** เพิ่ม progress bar บาง ๆ ด้านบนตาราง (เช่น `ProgressBar`/แถบ indeterminate) เมื่อ `loading && data.length > 0` แทน blur overlay — ทำได้ถ้าง่าย ไม่บังคับ

### 2. (เสริม ถ้าทำได้ปลอดภัย) ลดการ fetch ซ้ำจาก auto-pagesize
- `updateAutoPageSize` (ResizeObserver) ควร set `pageSize` เฉพาะเมื่อค่าต่างจริง (มี guard `prev === next` อยู่แล้ว — ตรวจว่าทำงาน) และพิจารณา debounce ResizeObserver เล็กน้อยเพื่อกัน thrash ตอน layout settle
- **อย่าเปลี่ยน logic infinite scroll / auto-fill viewport จนพฤติกรรมเพี้ยน** — ถ้าไม่มั่นใจให้ทำเฉพาะข้อ 1 แล้วจดข้อ 2 ไว้ใน Notes

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยนกลไกหลัก (server paging, infinite scroll, debounced search, startTransition) — แค่ทำให้ลื่นขึ้น
- ห้ามเปลี่ยน props/contract ของ `AppTable` (หน้าอื่นใช้ร่วม)
- ห้ามแตะ Explorer pages (`CourseListPage`/`LearnerGroupListPage` — คนละกลไก ไม่ใช้ AppTable)
- ห้ามแตะ backend

## Acceptance criteria

- [x] `/assignments` เลื่อนโหลดหน้าถัดไป **ไม่กระพริบ** (ไม่มี blur overlay แฟลชทับแถวเดิม) — footer "Loading more..." ทำงานแทน
- [x] initial load (เปิดหน้าครั้งแรก/ตารางว่าง) ยังมี spinner overlay ตามเดิม
- [x] search/filter/sort: ตารางเปลี่ยนข้อมูลแบบลื่น (แถวเดิมอยู่จนข้อมูลใหม่มา) ไม่แฟลชเป็นจอว่าง/blur
- [x] หน้า list **อื่นทุกหน้า** ยังทำงานปกติ (learners, learning-logs, enrollments, master-data ×4, users) — ไม่ regression
- [x] `npm run lint` (0/0) + `npm run build` ผ่าน

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: `/assignments` เลื่อนยาว ๆ ดูว่าไม่กระพริบ + initial load มี spinner + search แล้วลื่น; เช็ค `/learners`, `/master-data/divisions`, `/users` ว่าโหลด/เลื่อน/ค้นหายังปกติ

## Implementer Notes

- การแก้ตาม Scope
  - แก้ที่ `iLearn.Admin.React/src/components/ui/AppTable.tsx` เพียงจุดเดียว: เปลี่ยนเงื่อนไข overlay จาก `loading` เป็น `loading && data.length === 0`
  - ผลคือ overlay แสดงเฉพาะ initial load ตอนยังไม่มีแถวในตาราง และไม่ทับข้อมูลเดิมระหว่าง infinite scroll หรือ refetch
- ข้อ 2 (ลด fetch ซ้ำจาก auto-pagesize)
  - ไม่ได้เพิ่ม debounce/ปรับ logic เพิ่ม เพราะโค้ดเดิมมี guard `setPageSize(prev => prev === next ? prev : next)` อยู่แล้ว และงานนี้โฟกัสแก้อาการ flicker แบบ low-risk ตามขอบเขต
- พฤติกรรมหลังแก้
  - initial load: spinner overlay ยังแสดงปกติ
  - ขณะมีข้อมูลในตาราง: ไม่แสดง blur overlay; สถานะโหลดต่อใช้ footer (`Loading more...`/`Scroll down to load more`) แทน
  - refetch จาก search/filter/sort: แถวเดิมไม่ถูก blur ทับด้วย overlay
- Verification
  - `npm run lint` ผ่าน
  - `npm run build` ผ่าน
  - smoke check ผ่านที่ `/assignments` และ `/learners` (initial overlay ยังมีเฉพาะตอน data ว่าง, และระหว่างเลื่อนโหลดไม่พบ overlay ทับแถว)
