# PLAN-001: หน้า Upload SCORM (create) เลือก Content Type ไม่ได้

- **Status:** READY
- **Assigned:** Gemini
- **Priority:** High
- **Estimated scope:** 1 ไฟล์ (`ContentItemEditorPage.tsx`)

## Problem

หน้า `/content-library/new` (`iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`) ในโหมด create กำหนด wizard steps ไว้แค่ 2 step:

```ts
// บรรทัด ~251-262
if (isCreate) {
  return [
    { label: 'Package Upload', validate: validateUpload, render: renderUploadStep },
    { label: 'Review', render: renderReviewStep }
  ]
}
```

`renderMetadataStep` (ช่อง Display Name + Content Type dropdown Learn/Exam) ถูกใช้เฉพาะโหมด edit — ผลคือ**ตอนอัปโหลด SCORM ใหม่ ผู้ใช้เลือก Content Type ไม่ได้เลย** `form.typeId` ติดค่า default `1` (Learn) เสมอ และถูกส่งไปที่ `ContentItems/upload?typeId=${form.typeId}` — การอัปโหลดข้อสอบ (Exam, typeId=2) จึงทำไม่ได้จาก UI นี้ ผู้ใช้ต้องไปแก้ type ทีหลังในหน้า edit

## Scope (ทำแค่นี้)

1. เพิ่ม Metadata step เข้าไปในโหมด create ให้ wizard เป็น 3 steps: `Metadata → Package Upload → Review`
   - ใน Metadata step โหมด create: Display Name เป็น optional อยู่แล้ว (placeholder บอก "Leave blank to use ZIP filename") — คงพฤติกรรมนี้ไว้
   - `validateMetadata` ปัจจุบัน return true เสมอในโหมด create — ใช้ต่อได้เลย
2. ตรวจว่า Review step แสดง Content Type ที่เลือกถูกต้อง (โค้ด `selectedTypeName` มีอยู่แล้ว ควรทำงานได้ทันที)
3. ตรวจว่า `handleUpload` ยังส่ง `typeId` ที่ผู้ใช้เลือกไปกับ query string ถูกต้อง

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend (`ContentItemsController.upload`)
- ห้ามแก้โหมด edit (steps `Metadata → Review` เดิมถูกแล้ว)
- ห้ามแก้ `AppWizard`

## Acceptance criteria

- [ ] `/content-library/new` มี 3 steps และ step แรกเลือก Content Type ได้ (Learn/Exam)
- [ ] เลือก Exam → Review แสดง "Exam — assessment content" → upload แล้ว query string เป็น `typeId=2`
- [ ] ไม่กรอก Display Name ก็ยังผ่าน step ได้ (ใช้ชื่อไฟล์ ZIP เป็น fallback ตามเดิม)
- [ ] โหมด edit ไม่เปลี่ยนพฤติกรรม

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด `/content-library/new` ไล่ครบ 3 step

## Implementer Notes

(เติมหลังทำเสร็จ)
