# PLAN-126: Require Closed Status for Course Deletion & Unify CourseStatusText to Soft Badge

- **Assigned to:** Gemini
- **Status:** DONE
- **Created At:** 2026-07-22
- **Completed At:** 2026-07-22

---

## 1. Problem Statement & User Directives

### User Feedback & Requirements
1. **Require Closed Status Before Course Deletion:**
   The user specified: *"ก่อนที่จะ Delete Course ได้ ควรต้อง Close Course ก่อน"* (Before a course can be deleted, it MUST be Closed first).
2. **Badge UI Alignment (`CourseStatusText`):**
   The user noted the CSS classes produced by `<CourseStatusText>` (`<span class="... border-emerald-300 bg-emerald-50 text-emerald-700">Open</span>`).
   `CourseStatusText` was using `variant="outline"`. Per PLAN-123 design system unification, status badges should default to `variant="soft"` (`bg-emerald-100 text-emerald-800`), consistent with `StatusBadge` and `ReadinessBadge`.

---

## 2. Proposed Changes

### Backend (.NET)

#### [MODIFY] [CourseService.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Application/Services/CourseService.cs)
- In `DeleteCourseAsync(int id, bool force = false)`:
  - Add guard check: If `course.Status != CourseStatus.Closed` and `force == false`, throw `InvalidOperationException("Cannot delete this course because it is not Closed. Please close the course first before deleting.")`.

### Frontend (React)

#### [MODIFY] [CourseStatusBadge.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx)
- Add optional `variant?: BadgeVariant` prop to `CourseStatusText`, defaulting to `'soft'` so status text renders as a soft filled pill badge consistent with `ReadinessBadge` and `StatusBadge`.

#### [MODIFY] [CourseDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx)
- In `CourseControls`:
  - Update `Delete Course` control button to be `disabled={!isClosed || mutatingStatus}` with `title={!isClosed ? 'Course must be Closed before it can be deleted' : undefined}`.

---

## 3. Verification Plan

### Automated Tests
1. Run lint, build, and test suite:
   ```powershell
   npm run lint
   npm run build
   dotnet build iLearn.Tests -o artifacts\verify-test
   dotnet test artifacts\verify-test\iLearn.Tests.dll
   Remove-Item -Recurse -Force artifacts\verify-test
   ```

### Manual Verification
1. Verify on `CourseDetailPage.tsx`:
   - When Course Status is `Open` or `Draft`, `Delete Course` button is disabled with tooltip *"Course must be Closed before it can be deleted"*.
   - When Course Status is `Closed`, `Delete Course` button is enabled.
2. Verify `CourseStatusText` renders using `variant="soft"` (`bg-emerald-100 text-emerald-800`).

---

## 4. Implementer Notes

- **Fixes Applied:**
  - Added Closed status guard in `CourseService.cs` preventing deletion of non-Closed courses unless `force` is set.
  - Updated `CourseControls` in `CourseDetailPage.tsx` to disable the `Delete Course` button when the course is not `Closed`, with tooltip explanation *"Course must be Closed before it can be deleted"*.
  - Updated `CourseStatusText` in `CourseStatusBadge.tsx` to default to `variant="soft"`, unifying badge styling with `StatusBadge` and `ReadinessBadge`.
- **Verification Results:**
  - `npm run lint` passed (0 errors)
  - `npm run build` passed (0 errors, built in 1.75s)
  - `dotnet test` passed (222/222 tests passed, 0 failures)
