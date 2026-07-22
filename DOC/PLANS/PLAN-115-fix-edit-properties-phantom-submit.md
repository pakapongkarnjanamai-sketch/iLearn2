# PLAN-115: แก้ปุ่ม Edit Properties หน้า Master Data detail — phantom form submit ตอนสลับปุ่มเป็น Save

- **Status:** REVIEWED
- **Assigned:** Antigravity Gemini (React ล้วน — shared component + หน้าเดียว)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** Copilot รายงานระหว่าง PLAN-113 ว่าปุ่ม Edit Properties หน้า Category detail "กดแล้วฟอร์มไม่เปิด" → ผู้ใช้กดเองด้วยมือยืนยันว่า**เสียจริง** (2026-07-22)

---

## Root cause (วินิจฉัยจากโค้ด — อธิบายทุกอาการที่เคยเห็น)

`MasterDataDetailPage.tsx` ครอบทั้งหน้าใน `<form onSubmit={handleSave}>` และ sidebar สลับปุ่มตามโหมด:

- view mode: ปุ่มแรก = **Edit Properties** (`type="button"`, `onClick={() => setIsEditing(true)}`)
- edit mode: ปุ่มแรก = **Save Changes** (`type="submit"`)

**ลำดับเหตุการณ์ตอนคลิก Edit:**
1. click dispatch → `onClick` ยิง `setIsEditing(true)`
2. React (18/19) flush re-render **แบบ sync ภายใน dispatch เดียวกัน** — ปุ่มทั้งสอง render อยู่ตำแหน่งเดียวกัน element type เดียวกัน (`<button>`) ⇒ React **reuse DOM node เดิม** แค่เปลี่ยน attribute → node ที่เพิ่งถูกคลิกกลายเป็น `type="submit"`
3. เบราว์เซอร์ประเมิน **default action** ของ click *หลัง* handler จบ โดยอ่าน attribute **ปัจจุบัน** ของ node = submit ⇒ **form submit ทันที**
4. `handleSave` ยิง PUT (ค่าที่ยังไม่ได้แก้) → toast "Changes saved successfully" → `setIsEditing(false)` → กลับ view mode ในพริบตา

⇒ ผู้ใช้เห็นเป็น "ปุ่มไม่ทำงาน" และ**ไขปริศนาเก่า**: toast "Changes saved successfully" ระหว่าง QA smoke ของ PLAN-111 ที่เคยถูกสรุปว่าเป็น SignalR broadcast ของ admin คนอื่น — จริง ๆ คือหน้านี้ save ตัวเอง (ค่าเดิมทับค่าเดิม DB จึงไม่เปลี่ยน สอดคล้องกับที่ตรวจ DB แล้วไม่พบการแก้)

**ยืนยัน scope:** grep แล้ว `ControlAction type="submit"` ที่สลับตำแหน่งกับปุ่ม type=button ใน form เดียวกัน มีที่ `MasterDataDetailPage.tsx:192` **ที่เดียว** (จุดอื่นเป็น submit ใน modal form ที่ไม่สลับปุ่มกลางคลิก)

## Scope

### §1 แก้ที่ราก — `ControlsSidebar.tsx` (`ControlAction`)

เปลี่ยน branch ปุ่ม (บรรทัด ~121-125) ให้ **preventDefault เมื่อไม่ใช่ submit**:

```tsx
return (
  <button
    type={type}
    onClick={(event) => {
      // ปุ่ม type="button" ไม่มี default action ของตัวเอง — preventDefault ที่นี่กัน
      // เคส DOM node ถูก reuse แล้วกลายเป็น type="submit" ระหว่าง re-render ใน click เดียวกัน
      // (phantom submit — PLAN-115)
      if (type !== 'submit') event.preventDefault()
      onClick?.()
    }}
    className={rowStyles[variant]}
    title={title}
  >
```

- ปุ่ม `type="submit"` (Save Changes) ไม่โดน preventDefault — submit ปกติ
- ปุ่ม Link (`to`) ไม่แตะ
- กระทบทุกหน้าที่ใช้ `ControlAction` แบบ type=button — ปลอดภัยเพราะปุ่มพวกนี้ไม่มี default action ที่ต้องรักษาไว้

### §2 กันอีกชั้น — `MasterDataDetailPage.tsx`

ใส่ `key` แยก identity ปุ่มสองโหมดใน sidebar เพื่อบังคับ React สร้าง DOM node ใหม่ตอนสลับโหมด (node เก่าที่รับ click ถูกถอดทิ้ง — default action ไม่มีเป้า):

- edit branch: `<ControlAction key="save" type="submit" ...>` + `<ControlAction key="cancel" ...>`
- view branch: `<ControlAction key="edit" ...>` + `<ControlAction key="delete" ...>`

### §3 ปิด loop ที่ค้างจาก PLAN-111

หลัง fix ขึ้น QA: ทดสอบ **แก้ sortOrder ผ่าน UI จริง** (สิ่งที่ไม่เคยพิสูจน์ได้เพราะบั๊กนี้) — เปิด Category detail → Edit Properties → ฟอร์มเปิด → แก้ sortOrder → Save → refresh เห็นค่าใหม่ → แก้กลับ

### นอก Scope (ห้ามทำ)

- ห้ามแตะ `handleSave`/logic save ของหน้า (ถูกอยู่แล้ว — ปัญหาคือ submit ที่ไม่ตั้งใจ)
- ห้ามเปลี่ยน signature `onClick` ของ `ControlAction` ที่ callers เห็น (ยังเป็น `() => void` — event ถูกจัดการภายใน)
- ห้ามแตะปุ่ม/ฟอร์มหน้าอื่น (CourseDetail/CourseEditor/ฯลฯ ใช้ submit ใน modal — คนละ pattern ไม่โดนบั๊กนี้)

## Contract ที่เปลี่ยน

ไม่มี — behavior fix ฝั่ง React เท่านั้น

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Manual (QA):
1. Category detail → กด **Edit Properties** → ฟอร์มแก้ไขเปิดค้าง (ช่อง Name/Sort Order/Active/Description) **ไม่มี** toast "Changes saved successfully" โผล่เอง
2. แก้ sortOrder → **Save Changes** → toast สำเร็จ → ค่าใหม่แสดง + ตาราง Categories เรียงตาม (ปิด loop PLAN-111) → แก้กลับคืน
3. **Cancel** ใน edit mode → กลับ view mode ค่าไม่เปลี่ยน
4. Divisions / Course Types / Roles detail → Edit Properties เปิดฟอร์มปกติ (ใช้หน้าเดียวกัน)
5. Regression ปุ่ม sidebar หน้าอื่นที่ใช้ `ControlAction` (เช่น Assignment detail: Open Report/Add Courses/Delete Batch) → ยังทำงานปกติ
6. console 0 error

## Deploy note

- **Admin React เท่านั้น** (ไม่มี API/migration) — QA → verify → PROD (รอผู้ใช้ยืนยัน)

## Implementer Notes

- **§1**: แก้ไข `ControlsSidebar.tsx` โดยเพิ่ม `event.preventDefault()` สำหรับปุ่มทุกตัวที่ `type !== 'submit'` เพื่อป้องกัน phantom submit เมื่อ DOM node ถูก reuse ระหว่าง click event dispatch
- **§2**: เพิ่ม `key` ("save", "cancel", "edit", "delete") ใน `MasterDataDetailPage.tsx` สำหรับ `ControlAction` ทั้งใน Edit Mode และ View Mode เพื่อบังคับ React Re-create DOM node แยกออกจากกัน
- **Verification**: รัน `npm run lint` ผ่าน 0 errors และ `npm run build` ผ่าน 0 errors (built in 1.68s)

## Reviewer Sign-off (Claude Code, 2026-07-22)

**ผลรีวิว: ✅ ผ่าน — REVIEWED**

1. **§1** `ControlAction`: `preventDefault` เฉพาะ `type !== 'submit'` ก่อนเรียก `onClick?.()` — ตรงสเปคเป๊ะ, signature ภายนอกไม่เปลี่ยน (`onClick?: () => void` เดิม), ปุ่ม submit/Link ไม่โดนกระทบ, มีคอมเมนต์อธิบาย phantom submit ติดไว้ในโค้ด
2. **§2** `key` ครบทั้ง 4 ปุ่ม (save/cancel/edit/delete) — สองชั้นตามแผน
3. **Reviewer รัน verify เอง:** lint + build 0 errors
4. หมายเหตุ: การ verify จริงว่าบั๊กหาย (กด Edit แล้วฟอร์มเปิดค้าง) ต้องทำบน QA หลัง deploy — อยู่ใน Manual ข้อ 1-6 ของแผน รวม §3 ทดสอบแก้ sortOrder ผ่าน UI ครั้งแรก

**คงค้าง: deploy Admin React ขึ้น QA → manual 1-6 (โดยเฉพาะข้อ 1-2) → PROD รอผู้ใช้ยืนยัน** — deploy รวมกับ PLAN-114 ได้ (อยู่ working tree เดียวกัน)
