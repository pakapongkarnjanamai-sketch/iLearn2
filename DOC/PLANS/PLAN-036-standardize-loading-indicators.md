# PLAN-036: ปรับปรุง Loading Indicator ให้มีมาตรฐานเดียวกันทั่วทั้งระบบ

- **Status:** VERIFIED ✅ (Gemini review 2026-06-16)
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** Medium
- **Estimated scope:** 2 shared components + refactor component/page views ที่เขียนสปินเนอร์เอง

## Problem

ปัจจุบันหน้าจอต่าง ๆ ใน `iLearn.Admin.React` มีการแสดงผล Loading Indicator ที่ไม่เป็นมาตรฐานเดียวกัน:
1. **Button Saving/Submitting:** ปุ่ม submit ของฟอร์ม/Modal ต่าง ๆ (เช่น ปุ่ม Save, Create, Relocate, Clear Cache) ใช้วิธีเรียกปุ่ม native `<button>` และจัดการเงื่อนไขแสดงรูปสปินเนอร์ `{saving && <Loader2 ... />}` แยกกันไปเองในแต่ละหน้า ทำให้ความสูงของปุ่ม, ขนาดไอคอน, สีของไอคอน, และระยะ gap แตกต่างกันไปตามที่พัฒนา
2. **Explorer Tables & Dashboard:** มีการเขียนสปินเนอร์ระบุสถานะโหลดแบบ Custom เอง (เช่น ใน `ExplorerTable.tsx` และ `DashboardPage.tsx`) แทนการใช้งาน shared component `<LoadingState>`
3. **LoadingState (size="section"):** สังเกตว่า `LoadingState` เมื่อมีขนาดเป็น `section` จะไม่รองรับการแสดง `label` (ขณะที่ `page` รองรับ) ทำให้บางหน้าที่ต้องการข้อความประกอบตอนโหลดในส่วนย่อยจำต้องเขียน HTML ขึ้นมาเอง

**เป้าหมาย:** ทำการปรับปรุงส่วนประกอบแชร์ (Shared Components) ให้รองรับการโหลดที่ยืดหยุ่น และจัดการ Refactor หน้าต่าง ๆ เพื่อรวมการแสดงสถานะโหลดเข้าสู่มาตรฐานเดียวกันทั้งหมด

---

## Scope (ทำแค่นี้)

### 1. ขยายความสามารถของ Shared UI Components

#### 1.1 [AppButton.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/AppButton.tsx)
- เพิ่ม prop `loading?: boolean` ใน `AppButtonProps`
- ปรับแต่งให้ปุ่มถูก Disable อัตโนมัติเมื่อ `loading` มีค่าเป็น `true` (รวมเข้ากับ prop `disabled`)
- นำเข้า `Loader2` จาก `'lucide-react'`
- แสดงผล `<Loader2 className="animate-spin" aria-hidden="true" />` (ขนาดจะถูกควบคุมด้วย CSS `[&_svg]:h-4 [&_svg]:w-4` ของตัวปุ่มอยู่แล้ว) เมื่อ `loading === true`:
  - หากมี `icon` ให้แสดงไอคอนสปินเนอร์แทนที่ตำแหน่ง `icon` เดิม
  - หากไม่มี `icon` ให้เพิ่มรูปสปินเนอร์ไว้หน้าข้อความของปุ่ม

#### 1.2 [LoadingState.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/LoadingState.tsx)
- ปรับปรุงให้ `size="section"` รองรับการแสดงผล `label` เมื่อมีการส่งค่าเข้ามา โดยจัดวางไว้ใต้รูปสปินเนอร์ (เช่น ใช้คลาส `text-xs font-semibold text-slate-400 mt-2` และจัดสปินเนอร์ให้อยู่ในกรอบ flex-col gap-2)
- เพิ่ม prop `className?: string` (ค่าเริ่มต้นคือ `""`) เข้าไปใน wrapper หลักของ `LoadingState` (เช่น คลาสของ `size="section"` จากเดิม `flex h-32 items-center justify-center` ให้ปรับเป็น `flex h-32 items-center justify-center ${className}`) เพื่อเปิดให้ component อื่นกำหนดความสูงหรือขอบเขตเพิ่มได้ (เช่น ส่งค่า `className="h-full"`)

---

### 2. Refactor ตารางและหน้าจอข้อมูลให้ใช้ LoadingState

#### 2.1 [ExplorerTable.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/explorer/ExplorerTable.tsx)
- นำโค้ดโหลดสปินเนอร์แบบ custom (บรรทัด ~35-41) ออก
- เปลี่ยนไปใช้ `<LoadingState size="section" label={loadingLabel} className="h-full" />` แทน

#### 2.2 [DashboardPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/DashboardPage.tsx)
- นำสปินเนอร์แบบ custom (บรรทัด ~216-223) ออก
- เปลี่ยนมาใช้ `<LoadingState label="Loading dashboard…" />`

---

### 3. Refactor ปุ่ม Submit/Save ที่เขียน Loader เองให้ใช้ AppButton + loading

เปลี่ยนจาก native `<button>` ที่ระบุคลาสปุ่มและเขียน `{saving && <Loader2 ... />}` แยกเอง มาใช้ `<AppButton>` ร่วมกับ prop `loading`:

#### 3.1 [CourseDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx)
- ปุ่ม Submit ของ modal บันทึกคุณสมบัติ (Properties) (บรรทัด ~851-858) ให้เปลี่ยนมาใช้:
  ```tsx
  <AppButton
    type="submit"
    variant="primary"
    loading={savingProperties}
  >
    Save Changes
  </AppButton>
  ```

#### 3.2 [CourseEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx)
- ปุ่ม Submit เพื่อบันทึกการแก้ไขหลัก (บรรทัด ~864-875) ให้เปลี่ยนมาใช้:
  ```tsx
  <AppButton
    type="submit"
    variant="primary"
    icon={Save}
    loading={saving}
  >
    Save Changes
  </AppButton>
  ```

#### 3.3 [CourseListPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseListPage.tsx)
- ปุ่ม Submit ใน Modal สำหรับการสร้างหมวดหมู่ใหม่ (Create Category) (บรรทัด ~805-812) ให้เปลี่ยนเป็น `AppButton` ร่วมกับ `loading={submittingCreate}` และ `variant="primary"`
- ปุ่ม Submit ใน Modal สำหรับเปลี่ยนชื่อหมวดหมู่ (Rename Category) (บรรทัด ~860-867) ให้เปลี่ยนเป็น `AppButton` ร่วมกับ `loading={submittingRename}` และ `variant="primary"`

#### 3.4 [VersionDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx)
- ปุ่ม Submit สำหรับเซฟ General Info (บรรทัด ~620-627) ให้เปลี่ยนเป็น `AppButton` ร่วมกับ `loading={savingGeneral}` และ `variant="primary"`
- ปุ่ม Submit สำหรับเซฟ Content Info (บรรทัด ~785-792) ให้เปลี่ยนเป็น `AppButton` ร่วมกับ `loading={savingContent}` และ `variant="primary"`

#### 3.5 [LearnerGroupDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx)
- ปุ่ม Submit ของ Properties Modal (บรรทัด ~1058-1074) ให้ปรับมาใช้:
  ```tsx
  <AppButton
    type="submit"
    variant="primary"
    icon={Check}
    loading={savingProperties}
  >
    {savingProperties ? 'Saving...' : 'Save Changes'}
  </AppButton>
  ```

#### 3.6 [LearnerGroupListPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx)
- ปุ่ม Submit ใน Modal สำหรับสร้างโฟลเดอร์ใหม่ (Create Folder) (บรรทัด ~780-787) ให้เปลี่ยนเป็น `AppButton` ร่วมกับ `loading={creatingFolder}` และ `variant="primary"`
- ปุ่ม Submit ใน Modal ย้ายกลุ่ม (Relocate Group) (บรรทัด ~836-844) ให้เปลี่ยนเป็น `AppButton` ร่วมกับ `loading={movingInProgress}` และ `variant="primary"`

#### 3.7 [SystemConfigPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/system-config/SystemConfigPage.tsx)
- ปุ่มล้างแคชระบบ (Clear Cache) (บรรทัด ~220-237) ให้ปรับมาใช้ `AppButton` ร่วมกับ `loading={clearingCache}`, `variant="danger"`, `icon={Trash2}`, และ `className="w-full"`
  - *หมายเหตุ:* สปินเนอร์จะเปลี่ยนจากไอคอน `RefreshCw` ไปใช้ `Loader2` ตามมาตรฐานสปินเนอร์ของ `AppButton`

#### 3.8 [AppWizard.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/AppWizard.tsx)
- ปรับเปลี่ยนปุ่มที่ตรึงไว้ส่วนท้าย (Pinned Footer) ของวิซาร์ด (Cancel/Back, Next, Submit) ให้ใช้งานผ่าน `<AppButton>` (บรรทัด ~180-214) โดยระบุไอคอนและคลาสที่เหมาะสม พร้อมส่งผ่าน `loading={isSubmitting}` บนปุ่ม Submit

---

## Out of scope (ห้ามแตะ)

- ห้ามแก้ไข API หรือ Contract ในส่วน backend
- ห้ามเปลี่ยนกลไกการดึงข้อมูล/โหลดของเดิม (คงการใช้ useState/loading logic หรือ react-query เดิมไว้ทั้งหมด)
- ห้ามเปลี่ยน overlay blocking ขนาดใหญ่ที่ออกแบบไว้เฉพาะจุด (เช่น full blocking validation overlay ใน `BulkAssignPage.tsx` บรรทัด ~590-601) ให้คงเดิมไว้

---

## Acceptance criteria

- [x] `AppButton` มี prop `loading?: boolean` ที่ทำงานอย่างถูกต้อง (disables button + แสดงไอคอนหมุน `Loader2` แทนที่ icon/นำหน้าข้อความ)
- [x] `LoadingState` ทำงานแบบถอยหลังเข้ากันได้ (backward compatible) และรองรับการแสดง label เมื่อระบุขนาดเป็น `size="section"` ร่วมกับเปิดให้ส่ง `className` ปรับแต่ง wrapper ได้
- [x] `ExplorerTable` และ `DashboardPage` แสดง Loading State ผ่าน shared `LoadingState`
- [x] ปุ่มบันทึก/ส่งข้อมูลทั้งหมดในฟอร์มย่อย/Modal ตามที่ระบุ ได้รับการ Refactor ให้ใช้ `AppButton` ร่วมกับ prop `loading` อย่างสมบูรณ์
- [x] สปินเนอร์ในปุ่มทั้งหมดใช้ไอคอนมาตรฐานตัวเดียวกัน มีความสูงปุ่ม ระยะห่าง และความสมมาตรตามมาตรฐานของ `AppButton`
- [x] รันคำสั่งตรวจสอบ `npm run lint` และ `npm run build` ผ่านทั้งหมดโดยไม่มีข้อผิดพลาด

---

## Verification

```powershell
# รันตรวจสอบจากโฟลเดอร์ iLearn.Admin.React
npm run lint
npm run build
```

**การทดสอบ Manual:**
- ทดสอบเข้าหน้า Dashboard ดูว่าสปินเนอร์เริ่มต้นแสดงถูกต้อง
- เข้าหน้า Course Explorer และ Learner Group Explorer สังเกตสปินเนอร์ตอนโหลดตารางโฟลเดอร์
- ลองเปิด Modal แก้ไข/สร้าง/ย้าย/ลบข้อมูล แล้วกด Submit ดูว่าปุ่มแสดงไอคอนโหลดพร้อมสปินเนอร์ และ Disabled ตัวเองอย่างถูกต้อง

---

## Implementer Notes
- เพิ่ม `loading?: boolean` ให้ `AppButton` พร้อม disable อัตโนมัติและ spinner มาตรฐาน `Loader2`; ขยาย `icon` ให้รองรับทั้ง `LucideIcon` และ `ReactNode` เพื่อให้ `AppWizard submitIcon` ใช้ต่อได้โดยไม่เปลี่ยน contract
- ปรับ `LoadingState` ให้รองรับ `className` และรองรับ `label` ใน `size="section"`
- Refactor ตาม Scope ครบ: `ExplorerTable`, `DashboardPage`, `AppWizard` footer buttons, และปุ่ม submit/save ในหน้า Course/LearnerGroup/SystemConfig ที่ระบุไว้ทั้งหมด
- Verification ผ่าน:
  - `npm run lint`
  - `npm run build`
