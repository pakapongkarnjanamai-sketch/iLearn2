# PLAN-125: Fix Course Deletion FK Constraint, Force Delete Option & Detailed Reporting + Cleanup Course 969

- **Assigned to:** Gemini
- **Status:** DONE
- **Created At:** 2026-07-22
- **Completed At:** 2026-07-22

---

## 1. Problem Statement & Root Cause Analysis

### Reported Issue
1. User reported unable to delete Course 969 (`https://ap-ntc2137-prwb/iLearn/admin-react/courses/969`) and Course 507 (`https://ap-ntc2137-prwb/iLearn/admin-react/courses/507`).
2. User requested long-term improvement:
   - System must clearly explain in detail **why** a course cannot be deleted.
   - For cases where deletion is really necessary (e.g. cleanup), admins should be able to **force delete easily** in one action without having to navigate to other pages to delete enrollments/assignments manually first.

### Empirical Findings from Database Audit
1. **Course 507 (`Software license training 2025 - JP`)**:
   - `Courses.IsDeleted` = **True** (Already soft-deleted in DB).
   - Attempting to access `/courses/507` returns HTTP 404 from API and renders `NotFoundState` ("Course Not Found") in React. Course 507 was already soft-deleted previously.

2. **Course 969 (`PLAN-079-TEST-01 SCORM 1.2 Learn`)**:
   - `Courses.IsDeleted` = **False**.
   - `Enrollments`: 1 enrollment (Learner 610034), `Progress` = 0, `IsCompleted` = False.
   - `ContentItems`: ContentItem 1706 (`NTC-WI-PD2-050_12_Learn`) links to `FileStorageId` 1706.
   - **Root Cause of Deletion Failure:**
     When `CourseService.DeleteCourseAsync(969)` runs:
     - `ContentItem` 1706 is soft-deleted (`IsDeleted = 1`), but its `FileStorageId` column STILL holds `1706`.
     - `CourseService` then attempts `_fileStorageRepository.HardDeleteAsync(f)` (`DELETE FROM FileStorages WHERE Id = 1706`).
     - SQL Server database rejects the hard deletion with foreign key exception:
       `The DELETE statement conflicted with the REFERENCE constraint "FK_ContentItems_FileStorages_FileStorageId". The conflict occurred in database "iLearnDB_New", table "dbo.ContentItems", column 'FileStorageId'.`
     - `CoursesController.Delete` only caught `InvalidOperationException`, letting `DbUpdateException` bubble up as an unhandled HTTP 500 error.
     - Frontend `CourseDetailPage` caught the error and displayed generic toast text.

---

## 2. Proposed Changes

### Backend (.NET)

#### [MODIFY] [ICourseService.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Application/Interfaces/Services/ICourseService.cs)
- Update signature to `Task DeleteCourseAsync(int id, bool force = false);`.

#### [MODIFY] [CourseService.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Application/Services/CourseService.cs)
1. Inject `IGenericRepository<AssignmentCourse> _assignmentCourseRepository` into `CourseService`.
2. Update `DeleteCourseAsync(int id, bool force = false)`:
   - Check `inProgressCount`: If `inProgressCount > 0` and `force == false`, throw `InvalidOperationException("Cannot delete this course because {inProgressCount} learner(s) are currently taking the course (In Progress). Use Force Delete to override and clean up all active enrollments.")`.
   - If `force == true`: Also soft-delete active `Enrollments` for this course so in-progress learners do not block deletion.
   - Before hard-deleting `FileStorage` entries, set `r.FileStorageId = null` on all `contentItemsToSoftDel` that point to `FileStorage` entries to be hard-deleted, and call `_contentItemRepository.UpdateAsync(r)` before soft-deleting `r`. This releases the SQL Server foreign key `FK_ContentItems_FileStorages_FileStorageId`.
   - Update `assignments` query to find assignments linked directly or via `AssignmentCourse`:
     `var assignments = await _assignmentRepository.GetAsync(a => a.CourseId == id || a.AssignmentCourses.Any(ac => ac.CourseId == id), includeProperties: "AssignmentCourses");`
   - Soft-delete `AssignmentCourse` links for `id` via `_assignmentCourseRepository.DeleteAsync(link)`. If an assignment has no other remaining active courses or direct `CourseId == id`, soft-delete the assignment.

#### [MODIFY] [CoursesController.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/CoursesController.cs)
1. Update `Delete(int id, [FromQuery] bool force = false)` endpoint to pass `force` to `_courseService.DeleteCourseAsync(id, force)`.
2. Enhance error handling to catch:
   - `KeyNotFoundException ex` -> `NotFound(new ApiResponse<object> { Success = false, Message = ex.Message })`
   - `InvalidOperationException ex` -> `BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message })`
   - `Exception ex` -> `BadRequest(new ApiResponse<object> { Success = false, Message = $"Unable to delete course: {ex.Message}" })`

### Frontend (React)

#### [MODIFY] [CourseDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx)
1. Enhance Delete Flow:
   - When user clicks "Delete Course", attempt normal delete (`DELETE api/Courses/{id}`).
   - If server returns message that in-progress learners exist, show a confirmation modal asking if Admin wants to **Force Delete (ลบแบบบังคับเคลียร์ข้อมูล)**:
     - *"This course has active learners in progress. Do you want to Force Delete this course and clear all linked enrollments and assignments?"*
   - If confirmed, send `DELETE api/Courses/{id}?force=true`.
   - Display real API messages via `toast.error(message)` or `toast.success(...)`.

---

## 3. Verification Plan

### Automated Tests
1. Run existing test suite:
   ```powershell
   dotnet build iLearn.Tests -o artifacts\verify-test
   dotnet test artifacts\verify-test\iLearn.Tests.dll
   Remove-Item -Recurse -Force artifacts\verify-test
   ```

### Manual Verification & Cleanup
1. Delete Course 969 and verify DB soft-delete and FileStorage cleanup.
2. Test deleting a course with active learners:
   - Attempt normal delete -> receive clear warning.
   - Choose Force Delete -> course and linked enrollments deleted in one click.

---

## 4. Implementer Notes

- **Fixes Applied:**
  - Added `FileStorageId = null` unlinking on `ContentItem` prior to calling `HardDeleteAsync` on `FileStorage` in `CourseService.cs`.
  - Added `force` parameter support to `ICourseService`, `CourseService`, and `CoursesController` for one-click cascade deletion.
  - Added `AssignmentCourse` linking table cleanup support when deleting a course.
  - Enhanced `CourseDetailPage.tsx` delete handler to present detailed server error messages and offer Force Delete confirmation modal when active learners exist.
  - Successfully soft-deleted Course 969 and hard-deleted its SCORM binary package storage.
- **Verification Results:**
  - `npm run lint` passed (0 errors)
  - `npm run build` passed (0 errors, built in 1.47s)
  - `dotnet test` passed (222/222 tests passed, 0 failures)
  - Database cleanup script executed successfully for Course 969.
