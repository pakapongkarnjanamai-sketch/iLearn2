# PLAN-005: Learner Group Categories — เปลี่ยน new/edit จาก modal เป็น Wizard ตามมาตรฐานระบบ

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: ไฟล์ editor+page committed, build/test ผ่าน)
- **Assigned:** GPT
- **Priority:** Medium
- **Estimated scope:** 1 ไฟล์ใหม่ (`LearnerGroupCategoryEditorPage.tsx`) + แก้ 2 ไฟล์ (`LearnerGroupCategoriesPage.tsx`, `App.tsx`)

## Problem

หน้า `/master-data/learner-group-categories` (`iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`) ทำ create/edit category ผ่าน **modal กลางจอในหน้า list** (บรรทัด ~224-297: `form.id ? 'Edit Category' : 'New Category'`) ซึ่งไม่ตรงมาตรฐานระบบ — editor มาตรฐานต้องเป็น**หน้าแยกแบบ Wizard** (`AppWizard` จาก `src/components/ui/AppWizard.tsx`) ที่ route `/new` และ `/:id/edit` ครอบ `<Remount>` (ดูตัวอย่าง `ContentItemEditorPage.tsx`, `LearnerGroupEditorPage.tsx` และ PLAN-004 ที่ทำหน้า Users แบบเดียวกัน)

## Backend contract (มีอยู่แล้ว ห้ามแก้ — หน้าเดิมเรียกใช้อยู่)

REST endpoints (ดู call site เดิมใน `LearnerGroupCategoriesPage.tsx` บรรทัด ~44-135):

- `GET LearnerGroupCategories` — คืน array (หรือ `{ success, data }`) ของ `{ id, name, description, parentId, parentName, depth, hasChildren, childCount, learnerGroupCount }` (camelCase — type `LearnerGroupCategory` มีอยู่แล้วในไฟล์เดิม ย้าย/export ไปใช้ร่วม)
- `POST learnerGroupCategories` — JSON body `{ name, description, parentId }`
- `PUT learnerGroupCategories/{id}` — JSON body เดียวกัน
- (DELETE คงอยู่ที่หน้า list ตามเดิม — ไม่เกี่ยวกับแผนนี้)

ไม่มี endpoint GetById — หน้า edit ให้โหลด `GET LearnerGroupCategories` ทั้งก้อน (ข้อมูล category มีไม่มาก) แล้ว `find(c => c.id === Number(id))`; ไม่เจอ → `NotFoundState`. โหลดทั้งก้อนนี้จำเป็นอยู่แล้วเพื่อทำ dropdown Parent

## Scope (ทำแค่นี้)

### 1. ไฟล์ใหม่ `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`

ใช้ `AppWizard` ตาม pattern ของ editor มาตรฐาน (state `currentStep`, `steps: WizardStep[]`, submit → toast → `navigate('/master-data/learner-group-categories')`) รองรับทั้ง create (`/new`) และ edit (`/:id/edit`) ในไฟล์เดียว (ดู param `id` แบบ `ContentItemEditorPage`)

**2 steps ทั้งสองโหมด:**
1. **Details** — ฟิลด์เดิมจาก modal ทั้ง 3: Name (required), Description (optional), Parent Category (select มี option "— Root (no parent) —" + indent ตาม depth ตามเดิม บรรทัด ~268-284) — `validate`: name ไม่ว่าง (toast แจ้งเหมือนเดิม)
2. **Review** — สรุป Name / Description / Parent (แสดงชื่อ parent ไม่ใช่ id)

- โหมด edit: ตัด option ตัวเองออกจาก Parent dropdown (พฤติกรรมเดิม `parentOptions` บรรทัด ~137 — คงไว้เท่าเดิม อย่าเพิ่ม logic กรอง descendant)
- submit: POST หรือ PUT ตาม contract ข้างบน → toast `Category created`/`Category updated` → navigate กลับหน้า list
- ใช้ `LoadingState` ระหว่างโหลด, `NotFoundState` เมื่อ edit id ที่ไม่มีจริง

### 2. `LearnerGroupCategoriesPage.tsx` — ตัด modal ออก เปลี่ยนเป็น navigate

- ปุ่ม "New Category" → navigate `/master-data/learner-group-categories/new`
- ปุ่ม Edit (icon Edit3) ในแถว → navigate `/master-data/learner-group-categories/${id}/edit`
- ลบ `form` state, `openCreate`/`openEdit`/`closeForm`/`handleSubmit`, `parentOptions`, และ JSX modal (บรรทัด ~224-297) — **คง `handleDelete` + `useConfirm` ไว้ตามเดิม**
- export type `LearnerGroupCategory` (และ `ApiListResponse` ถ้าหน้าใหม่ใช้) ให้ editor import — อย่าประกาศซ้ำ

### 3. `App.tsx` — เพิ่ม 2 routes ใต้ route `master-data/learner-group-categories` เดิม

```tsx
<Route path="master-data/learner-group-categories/new"
  element={<RequireRole superAdminOnly><Remount><LearnerGroupCategoryEditorPage /></Remount></RequireRole>} />
<Route path="master-data/learner-group-categories/:id/edit"
  element={<RequireRole superAdminOnly><Remount><LearnerGroupCategoryEditorPage /></Remount></RequireRole>} />
```

**ระวังลำดับ route:** ต้องวางก่อน route generic `master-data/:type/new` และ `master-data/:type/:id` (บรรทัด ~155-170) เพื่อไม่ให้ถูก match ไปเข้า `MasterDataDetailPage` — จริง ๆ React Router 7 จัด specificity ให้เอง แต่ให้วางไว้ติดกับ route `learner-group-categories` เดิมเพื่ออ่านง่าย แล้ว**ทดสอบว่าเปิดแล้วเข้า editor ใหม่จริง ไม่หลุดไป MasterDataDetailPage**

(Breadcrumb: segment `learner-group-categories`, `new`, `edit` มีใน `SEGMENT_MAP` ครบแล้ว — ไม่ต้องแก้ `Breadcrumbs.tsx`)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend (`LearnerGroupCategoriesController`)
- ห้ามแก้ `AppWizard.tsx` / shared components
- ห้ามแตะ delete flow และตาราง list (คอลัมน์/depth indent เดิม)
- ห้ามแตะ `MasterDataDetailPage.tsx` และ route `master-data/:type/*` อื่น
- ห้ามเพิ่ม logic กันเลือก parent เป็น descendant ของตัวเอง (พฤติกรรมเดิมกรองแค่ตัวเอง — ถ้าเห็นว่าควรแก้ ให้จดใน Implementer Notes แล้วข้าม)

## Acceptance criteria

- [x] `/master-data/learner-group-categories/new` และ `/:id/edit` เป็นหน้า wizard 2 steps (Details → Review)
- [x] หน้า list ไม่เหลือ modal; ปุ่ม New Category / icon Edit นำทางไปหน้า wizard; Delete ยังทำงานเหมือนเดิม
- [x] สร้าง/แก้ไขแล้วกลับมาหน้า list เห็นข้อมูลใหม่ (list โหลดใหม่ตอน mount อยู่แล้ว)
- [x] เปิด edit ด้วย id ที่ไม่มีจริง → `NotFoundState`
- [x] Parent dropdown ใน edit ไม่มีตัวเอง + แสดง indent ตาม depth ตามเดิม
- [x] route ใหม่ไม่ถูก generic `master-data/:type/*` ดัก (ทดสอบเปิดทั้งสอง route แล้วเข้า editor ที่ถูกต้อง)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: New Category → ไล่ 2 steps → เห็นในตาราง; Edit เปลี่ยน parent → Review → Save; ลอง `/master-data/learner-group-categories/999999/edit` → NotFoundState; เช็คว่า `/master-data/divisions/new` (MasterDataDetailPage) ยังทำงานปกติ

## Implementer Notes

- เพิ่มไฟล์ใหม่ `LearnerGroupCategoryEditorPage.tsx` ใช้ `AppWizard` 2 steps (Details/Review) รองรับ create+edit ในหน้าเดียว
- หน้า edit โหลด `GET LearnerGroupCategories` ทั้งก้อนแล้ว `find` ตาม id; ไม่เจอแสดง `NotFoundState` ตามแผน
- โหมด edit ตัด option ตัวเองออกจาก Parent dropdown โดยไม่เพิ่ม descendant guard (ตาม out-of-scope)
- รีแฟกเตอร์ `LearnerGroupCategoriesPage.tsx` เอา modal ออกทั้งหมด เหลือ list + delete เดิม และเปลี่ยน New/Edit เป็น route navigate
- เพิ่ม route ใหม่ใน `App.tsx` สำหรับ `/master-data/learner-group-categories/new` และ `/:id/edit` พร้อม `RequireRole + Remount` วางก่อน generic master-data routes
- Verification: `npm run lint` ผ่าน (11 warnings baseline, 0 errors), `npm run build` ผ่าน
- Manual verify ผ่าน: เปิด `/master-data/learner-group-categories/new` เห็น wizard 2 steps, สร้าง category ชั่วคราว `PLAN005_TMP`, เปิด edit และบันทึกค่าใหม่ได้, เปิด `/master-data/learner-group-categories/999999/edit` เห็น `NotFoundState`, เปิด `/master-data/divisions/new` ยังเข้า `MasterDataDetailPage` ปกติ; ลบข้อมูลทดสอบ `PLAN005_TMP` ออกแล้ว
