# PLAN-123: ReadinessBadge variant="soft" unification + UI inline CSS cleanup

- **Status:** DONE — Implement สำเร็จแล้ว (2026-07-22)
- **Assigned:** Antigravity Gemini
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้สังเกตพบ inline Tailwind classes ของป้ายสถานะ `Published` (`<span class="inline-flex min-h-[24px] items-center rounded-full border px-2.5 py-0.5 font-bold text-xs border-emerald-300 bg-emerald-50 text-emerald-700">`) ใน DevTools บนหน้า `/courses/569/version/721` ซึ่งทำให้เกิด UI Inconsistency กับ `StatusBadge` (`Active Version` — `variant="soft"`) บนการ์ดด้านบนในหน้าเดียวกัน
- **อ่าน `iLearn.Admin.React/README.md` ก่อนเริ่ม**

---

## วินิจฉัย

1. **`ReadinessBadge.tsx`**: ปัจจุบัน hardcode `variant="outline"` ซึ่งสร้างรูปทรงแคปซูล/วงรี (`rounded-full`) มีเส้นขอบ (`border-emerald-300 bg-emerald-50 text-emerald-700`) ขณะที่กติกา UI (`README.md`) และ `StatusBadge` / `CourseStatusBadge` ทั้งระบบใช้ `variant="soft"` (สี่เหลี่ยมมุมมน `rounded` สีทึบนุ่ม `bg-emerald-100 text-emerald-800`) ทำให้เมื่อปรากฏในหน้าเดียวกัน สไตล์และรูปทรงขัดแย้งกันอย่างเห็นได้ชัด
2. **Raw `<button>` ใน `VersionDetailPage.tsx` (L562)**: ปุ่ม "Open SCORM Player" ในตารางใช้ `<button className="inline-flex items-center gap-1.5 rounded border border-indigo-100 px-2 py-1 text-xs font-bold text-indigo-600 hover:bg-indigo-50 ...">` ซึ่งเป็น ad-hoc button ขัดกับกติกา `AppButton`
3. **Hardcoded `<span>` category/count pills**:
   - `LearnerGroupEditorPage.tsx` (L563) & `LearnerGroupDetailPage.tsx` (L291): ใช้ `<span className="bg-indigo-50 text-blue-700 border border-blue-100 px-2 py-0.5 rounded font-extrabold truncate flex-1">`
   - `LearnerDirectorySelector.tsx` (L659): ใช้ `<span className="bg-indigo-50 text-indigo-700 border border-indigo-100 px-2.5 py-0.5 text-xxs font-extrabold rounded-full shadow-3xs shrink-0">`

---

## Scope

### §1. Unify `ReadinessBadge.tsx` ให้เป็น `variant="soft"` โดย default
- ไฟล์: `src/components/ui/ReadinessBadge.tsx`
- เพิ่ม prop `variant?: 'soft' | 'outline' | 'tag'` ให้ปรับเปลี่ยนได้ โดยให้ **default เป็น `'soft'`**
- อัปเดต `ReadinessBadge` ให้ส่ง `variant={variant}` ไปที่ `<Badge>`
- ผลลัพธ์: ป้ายสถานะ `Published` / `Not Ready` / `Queued Upload` / `Missing Launch` จะถูกเปลี่ยนเป็นสไตล์ `soft` (สี่เหลี่ยมมุมมน สีทึบนุ่ม สอดคล้องกับ `StatusBadge` ทั้งแอป)

### §2. เปลี่ยน Raw `<button>` เป็น `AppButton`
- ไฟล์: `src/pages/courses/VersionDetailPage.tsx`
- เปลี่ยนปุ่ม "Open SCORM Player" ในตาราง `Current Content` (L562) จาก raw `<button>` มาใช้ `<AppButton variant="secondary" size="sm" icon={ExternalLink}>`

### §3. Replace Hardcoded `<span>` Pills ด้วย `<Badge>`
- ไฟล์: `src/pages/learner-groups/LearnerGroupEditorPage.tsx` (L563) & `src/pages/learner-groups/LearnerGroupDetailPage.tsx` (L291)
  - เปลี่ยน `categoryName` span เป็น `<Badge tone="neutral" variant="soft">` (เหมือนที่ทำใน `BulkAssignPage.tsx` ตาม PLAN-122)
- ไฟล์: `src/components/shared/LearnerDirectorySelector.tsx` (L659)
  - เปลี่ยน count span เป็น `<Badge tone="neutral" size="xxs">{filteredCount}</Badge>`

---

## Verification Plan

### Automated Tests
1. `npm run lint` — ผ่าน 0 errors
2. `npm run build` — ผ่าน 0 errors

### Manual Verification
1. เปิดหน้า `/admin-react/courses/569/version/721`
   - สังเกตป้าย `Active Version` ใน Overview Card และป้าย `Published` ในตาราง Current Content ต้องเป็นสไตล์ `soft` (สี่เหลี่ยมมุมมน สีทึบนุ่ม) รูปทรงสอดคล้องกันทั้งหน้า
   - ตรวจสอบปุ่ม "Open SCORM Player" ต้องมีสไตล์มาตรฐาน `AppButton` และทำงานเปิด SCORM Player ได้ถูกต้อง
2. ตรวจสอบหน้า Learner Group Detail และ Editor ป้าย Category ต้องแสดงผลผ่าน `<Badge>` อย่างสวยงาม

---

## Implementer Notes

- **`Badge.tsx`**: ส่งออก `export type BadgeVariant` เพื่อให้ `ReadinessBadgeProps` สามารถ import ไปใช้งานได้อย่างถูกต้อง
- **`ReadinessBadge.tsx`**: ปรับให้ default เป็น `variant="soft"` ส่งผลให้ป้ายสถานะ `Published` กลายเป็นสไตล์ soft fill `bg-emerald-100 text-emerald-800` สอดคล้องกับป้าย `Active Version` บนหน้าเดียวกัน
- **`VersionDetailPage.tsx`**: ปรับปุ่ม "Open SCORM Player" ในตารางให้ใช้ `<AppButton variant="secondary" size="sm" icon={ExternalLink}>`
- **`LearnerGroupEditorPage.tsx` / `LearnerGroupDetailPage.tsx` / `LearnerDirectorySelector.tsx`**: เปลี่ยน span พิมพ์คลาสเองเป็น `<Badge>` ตามกติกา
- **Verification**: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.25s)

