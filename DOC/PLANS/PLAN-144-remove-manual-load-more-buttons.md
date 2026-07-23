# PLAN-144 — Replace Detail-Table Load More Buttons with Scroll Loading

- **สถานะ:** DONE
- **Assigned:** GitHub Copilot (GPT)
- **วันที่:** 2026-07-23
- **หน้าที่กระทบ:** React Admin detail/report tables ที่ยังมีปุ่ม `Load more`

## เป้าหมาย

ผู้ใช้ต้องการให้ตาราง Members ในหน้า Learner Group และหน้าอื่น ๆ ที่ยังใช้ปุ่ม `Load more` เปลี่ยนเป็นโหลดเพิ่มเมื่อ scroll ถึงท้ายตารางแทน

## Scope

- กวาด `iLearn.Admin.React/src` หา `Load more` / `loadMore` / `visibleRows`
- เปลี่ยน local chunked tables ให้ใช้ `onScroll` auto-increment rows
- Footer แสดง count และข้อความ hint `UI_LABELS.scrollToLoadMore` เท่านั้น ไม่มีปุ่ม manual load more

## In Scope Pages

- LearnerGroupDetailPage members table
- AssignmentDetailPage courses/learners tables
- AssignmentReportPage learner rows
- CourseDetailPage versions/learners/assignments tables
- VersionDetailPage content table
- NotificationsPage notification list

## Out of Scope

- `AppTable` และ report pages ที่ใช้ scroll loading อยู่แล้ว
- `LearnerDirectorySelector` hint text ที่บอกผู้ใช้ให้ scroll ลงโหลดเพิ่มอยู่แล้ว

## Verification

- `npm run lint` ✓
- `npm run build` ✓
- `dotnet build iLearn.Tests -o artifacts\verify-test` ✓
- `dotnet test artifacts\verify-test\iLearn.Tests.dll` → **275/275 passed** ✓
- Commit `9a25676` ✓
- QA deploy: API stamp `20260723160518` ✓ · Admin React `RobocopyExitCode=3` ✓ · QA note: group id 36 does not exist in QA DB, so only SPA route fallback was checked for that URL
- PROD deploy: API stamp `20260723160824` ✓ · Admin React `RobocopyExitCode=3` ✓ · `/admin-react/learner-groups/36` 200 · `GET /Service/api/LearnerGroups/36` 200 · group `3. Production`, members 303 ✓

## Implementer Notes

- เพิ่ม helper `shouldLoadMoreOnScroll(...)` ใน `src/lib/tableStandards.ts`
- ถอดปุ่ม manual `Load more` จาก local detail/report tables แล้วใช้ scroll-to-bottom loading + footer hint `UI_LABELS.scrollToLoadMore`
- ตรวจด้วย grep แล้วไม่เหลือปุ่ม manual `Load more` หรือ `loadMore` click handler ใน `iLearn.Admin.React/src/**`