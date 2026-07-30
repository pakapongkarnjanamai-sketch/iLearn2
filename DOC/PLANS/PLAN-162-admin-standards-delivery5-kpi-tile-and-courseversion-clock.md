# PLAN-162: Admin standards Delivery #5 — shared KPI tile + CourseVersion clock consistency

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** 5 files + focused verification + plan/log updates
- **สร้างเมื่อ:** 2026-07-30

## Problem

ต่อจาก Delivery #4 ใน PLAN-157 ต้องทำสองเรื่องให้ครบ:
1) ลด duplication ของ `KpiTile` ที่เหมือนกันใน 2 report pages
2) แก้ timestamp source ใน `CourseVersionService` จาก `DateTime.UtcNow` ให้ใช้ injected `_dateTime.Now` และเพิ่ม regression test ยืนยัน

## Scope

1. เพิ่ม shared component `ReportKpiTile` ที่ `src/components/ui/ReportKpiTile.tsx` พร้อม semantic tones: `neutral | info | success | danger`.
2. Refactor สอง report pages ให้ใช้ shared component และ mapping โทนเดิมให้เทียบเท่า:
   - `AssignmentSummaryReportPage.tsx`
   - `LearnerGroupSummaryReportPage.tsx`
3. แก้ `iLearn.Application/Services/CourseVersionService.cs` แทนที่ `DateTime.UtcNow` 2 จุดด้วย `_dateTime.Now`:
   - `CourseVersion.CreatedAt`
   - `CourseContentItem.CreatedAt`
4. เพิ่ม/ปรับเทสต์ใน `iLearn.Tests/CourseVersionLearnerPolicyTests.cs`:
   - เพิ่ม test ใหม่ยืนยันทั้ง `CourseVersion` และ `CourseContentItem` ใช้ค่า `Now` จาก `FakeDateTime`
   - ขยาย harness record ให้ expose `CourseContentItems` repository สำหรับ assertion

## Out of scope

- ไม่เปลี่ยนข้อความ label/UI contract ของ report cards
- ไม่แตะ endpoint/API contract
- ไม่เปลี่ยน lint severity เป็น error

## Acceptance criteria

1. สอง report pages ไม่ประกาศ `KpiTile` ภายในไฟล์แล้ว และใช้ shared component เดียว.
2. `CourseVersionService` ไม่มี `DateTime.UtcNow` ในสองจุดที่ระบุ.
3. มีเทสต์ deterministic ยืนยัน `CreatedAt` ของ version และ content link เท่ากับ injected clock.
4. `npm run lint` ผ่าน (0 errors).
5. `npm run build` ผ่าน.
6. `dotnet test` เฉพาะ `CourseVersionLearnerPolicyTests` ผ่าน.
7. `git diff --check` ผ่าน (ยอมรับได้หากมีเพียง CRLF warning).

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

```powershell
cd ..
dotnet build iLearn.Tests -o artifacts\verify-plan162
dotnet test artifacts\verify-plan162\iLearn.Tests.dll --filter FullyQualifiedName~CourseVersionLearnerPolicyTests
Remove-Item -Recurse -Force artifacts\verify-plan162
git diff --check
```

## Implementer Notes

- Added `ReportKpiTile` shared component and replaced duplicated local `KpiTile` functions in both report pages.
- Updated `CourseVersionService` to use `_dateTime.Now` for new `CourseVersion` and `CourseContentItem` creation timestamps.
- Added `CreateVersionAsync_UsesInjectedClockForCreatedAtOnVersionAndContentLink` test and exposed `CourseContentItems` repo in the harness for direct assertions.
- Verification results:
  - `npm run lint` ✓ (0 errors, 6 warnings)
  - `npm run build` ✓
  - `dotnet test ...CourseVersionLearnerPolicyTests` ✓ (Passed 8/8)
  - Removed temporary `artifacts\verify-plan162` folder ✓
  - `git diff --check` ✓ (only CRLF normalization warnings)

## Reviewer Notes (Claude Code, 2026-07-30)

- **VERIFIED.** `ReportKpiTile` map tone ได้ class ตรงของเดิมทุกค่า + markup เหมือนเดิมทุกตัวอักษร ⇒ visible output ไม่เปลี่ยนจริง · **mutation test: revert `_dateTime.Now` ทีละบรรทัด ทำ assertion แดงทั้งบรรทัด 78 และ 79** ⇒ test กันของจริงทั้งสอง entity (ตรวจแล้วว่า `AddEntity` ไม่ stamp `CreatedAt` เอง)
- รายละเอียดการตรวจทั้ง rollout (AC 1-8 + mutation test + ข้อสังเกต 5 ข้อ) อยู่ใน `PLAN-157` หัวข้อ "Reviewer Notes — รอบ implement"
