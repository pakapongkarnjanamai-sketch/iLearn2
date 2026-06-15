# PLAN-021: ทำให้ Learning Logs เป็น SuperAdmin-only สม่ำเสมอ (UI ตรงกับ API)

- **Status:** DONE
- **Assigned:** GPT
- **Priority:** Medium
- **Estimated scope:** 2 ไฟล์ frontend (`App.tsx`, `config/navigation.ts`)

## Problem

`LearningLogsCRUDController` ฝั่ง API เป็น `[Authorize(Policy = "SuperAdminOnly")]` (ยืนยันแล้ว) — แต่ฝั่ง React **ไม่ตรง**:
1. route `/learning-logs` ใน `iLearn.Admin.React/src/App.tsx` (บรรทัด ~89) ไม่ได้ครอบ `<RequireRole superAdminOnly>` (ต่างจาก `/enrollments` ที่ครอบ)
2. เมนู "Learning Logs" ใน `iLearn.Admin.React/src/config/navigation.ts` อยู่ใน section **"Operations"** (ไม่ใช่ "Super Admin") → division-admin มองเห็นเมนู

ผล: division-admin เห็นเมนู + เปิดหน้าได้ แต่พอโหลด grid จะโดน API 403 → ตารางว่าง/error (ไม่รั่วข้อมูล แต่ UX สับสน) — ดู `DOC/division_isolation_analysis.md` §5.1

**ทิศที่เลือก:** ทำให้ SuperAdmin-only สม่ำเสมอ (ให้ตรงกับ API ที่ประกาศไว้แล้ว — ไม่ลดระดับความปลอดภัย) ไม่ใช่เปิดให้ division-admin (ซึ่งต้องเพิ่ม isolation = งานใหญ่กว่า)

## Scope (ทำแค่นี้)

1. **`App.tsx`** — ครอบ route `learning-logs` ด้วย `<RequireRole superAdminOnly>` ให้เหมือน `enrollments`:
   ```tsx
   <Route path="learning-logs" element={
     <RequireRole superAdminOnly>
       <EntityListPage config={adminListConfigs.learningLogs} />
     </RequireRole>
   } />
   ```
2. **`config/navigation.ts`** — ย้ายรายการ **Learning Logs** จาก section "Operations" ไปไว้ใน section "Super Admin" (พร้อม `superAdminOnly: true` บน item ให้สอดคล้องของอื่นใน section นั้น เช่น Enrollments)
   - section "Operations" ที่เหลือ (Content Library, Learners) คงไว้ตามเดิม — **ห้ามย้าย/แตะ** (สองตัวนี้ AdminOnly division-admin เข้าได้)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend (`LearningLogsCRUDController` เป็น SuperAdminOnly ถูกแล้ว)
- ห้ามเพิ่ม division isolation ลง LearningLog (ถ้าจะเปิดให้ division-admin เป็นอีกแนวทาง/แผนแยก — ไม่ทำในนี้)
- ห้ามแตะ Content Library / Learners / Enrollments
- ห้ามแตะ learner-facing learning log endpoints (HMAC, คนละ controller)

## Acceptance criteria

- [x] เมนู "Learning Logs" อยู่ใน section Super Admin (division-admin ไม่เห็น)
- [x] เปิด `/learning-logs` ด้วย account ที่ไม่ใช่ SuperAdmin → ถูก redirect/บล็อกแบบเดียวกับ `/enrollments` (ไม่ถึงหน้า grid)
- [x] SuperAdmin ยังเข้า `/learning-logs` และเห็นข้อมูลได้ปกติ
- [x] Content Library / Learners ยังอยู่ Operations และ division-admin เข้าได้เหมือนเดิม

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual (ถ้ามี account 2 บทบาท): division-admin ไม่เห็นเมนู Learning Logs + เปิด URL ตรงถูกบล็อก; SuperAdmin เข้าได้

## Implementer Notes

- แก้ `iLearn.Admin.React/src/App.tsx` โดยครอบ route `learning-logs` ด้วย `<RequireRole superAdminOnly>` ให้สอดคล้องกับ route `enrollments` และ policy ฝั่ง API
- แก้ `iLearn.Admin.React/src/config/navigation.ts` โดยย้ายเมนู `Learning Logs` ออกจาก section `Operations` ไปอยู่ section `Super Admin` พร้อมตั้ง `superAdminOnly: true` ที่ item
- ยืนยันว่า `Operations` ยังคงมีเฉพาะ `Content Library` และ `Learners` (ไม่แตะรายการอื่น)
- Verification ที่รันแล้ว:
  - `npm run lint` ผ่าน (0 errors, 11 warnings baseline)
  - `npm run build` ผ่าน
  - `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน
  - `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)
  - ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว
