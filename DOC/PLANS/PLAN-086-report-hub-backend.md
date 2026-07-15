# PLAN-086: Report Hub Phase 1 — Backend (ReportsController + 4 aggregate endpoints)

- **Status:** DONE → VERIFIED — Finding 1+2 FIXED (Claude Code 2026-07-14: effective-schedule dates ใน BuildVisibleEnrollmentRowsQuery + 2 regression tests)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **คู่ขนานกับ:** [PLAN-087](PLAN-087-report-hub-frontend.md) (Gemini ทำ React จาก contract ในแผนนี้) — **API contract ด้านล่างถูก freeze แล้ว ห้ามเปลี่ยน shape โดยไม่อัปเดตทั้ง 2 แผน + AGENT_LOG**

> ผู้ใช้สั่ง (2026-07-14): พัฒนา Report Hub ให้เป็นระบบรายงานจริง — เลือกครบ 4 รายงาน: Compliance/Overdue (org-wide), Learner Transcript, Course Completion Summary, Training Activity รายเดือน

---

## บริบท (ยืนยันจากโค้ดแล้ว)

- ปัจจุบันมี aggregate endpoint เดียว: `Assignments/dashboard/{id}` — รายงานข้าม assignment ไม่มีเลย
- ข้อมูล learner (name/division/department) มาจาก EmployeeHub — **ใช้ `ILearnerApiService.GetLearnersByCodesAsync(codes)` bulk เท่านั้น** (1 HTTP + cache 24h — ห้าม `GetLearnerByCodeAsync` วนลูป = N+1)
- Status ของ learner ต้องคำนวณผ่าน `AssignmentStatusKeys.GetScheduledLearnerStatus(isCompleted, progress, startDate, dueDate, now)` — **ใช้ field ชุดเดียวกับที่ `AssignmentService` (~บรรทัด 1008) ใช้** เพื่อให้ตัวเลขตรงกับหน้า assignment ทุกหน้า
- `Enrollment`: LearnerCode, CourseId, IsCompleted, Progress, TotalScore, TotalTimeSpent, StartDate/DueDate/CompletedDate, AssignmentLinks
- `LearningLog`: LearnerCode, EnrollmentId, Status, Score, TotalSecondsPlayed, CreatedAt (BaseEntity)

## Scope

### 1. ไฟล์ใหม่ `iLearn.Application/DTOs/ReportDtos.cs` — contract (freeze)

```csharp
// ── GET api/Reports/compliance ──
public class ComplianceReportDto
{
    public DateTime GeneratedAt { get; set; }
    public int TotalLearners { get; set; }          // distinct LearnerCode ของ enrollment ที่นับทั้งหมด
    public int OpenEnrollments { get; set; }        // ยังไม่ completed
    public int CompletedEnrollments { get; set; }
    public int OverdueEnrollments { get; set; }
    public int OverdueLearners { get; set; }        // distinct
    public double ComplianceRate { get; set; }      // Completed / (Completed + Open) * 100
    public List<ComplianceGroupRow> ByDivision { get; set; } = new();
    public List<ComplianceGroupRow> ByDepartment { get; set; } = new(); // แถวละ dept, ระบุ division กำกับ
    public List<ComplianceOverdueRow> OverdueRows { get; set; } = new();
}
public class ComplianceGroupRow
{
    public string GroupName { get; set; } = string.Empty;   // ชื่อ division หรือ department
    public string? ParentDivision { get; set; }             // เฉพาะแถว department
    public int Learners { get; set; }                       // distinct
    public int Enrollments { get; set; }
    public int Completed { get; set; }
    public int Overdue { get; set; }
    public double CompletionRate { get; set; }
}
public class ComplianceOverdueRow
{
    public string LearnerCode { get; set; } = string.Empty;
    public string? LearnerName { get; set; }
    public string? Division { get; set; }
    public string? Department { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public string? AssignmentNo { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public double Progress { get; set; }
}

// ── GET api/Reports/transcript/{learnerCode} ──
public class TranscriptReportDto
{
    public DateTime GeneratedAt { get; set; }
    public string LearnerCode { get; set; } = string.Empty;
    public string? LearnerName { get; set; }
    public string? Division { get; set; }
    public string? Department { get; set; }
    public List<string> LearnerGroups { get; set; } = new();
    public int TotalCourses { get; set; }
    public int CompletedCourses { get; set; }
    public List<TranscriptRow> Rows { get; set; } = new();
}
public class TranscriptRow
{
    public int EnrollmentId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public string? AssignmentNo { get; set; }        // จาก AssignmentLinks ตัวแรก (null ได้)
    public string Status { get; set; } = string.Empty; // AssignmentStatusKeys.Learner key
    public double Progress { get; set; }
    public int TotalScore { get; set; }
    public int TotalTimeSpentSeconds { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

// ── GET api/Reports/course-summary ──
public class CourseSummaryReportDto
{
    public DateTime GeneratedAt { get; set; }
    public List<CourseSummaryRow> Rows { get; set; } = new();
}
public class CourseSummaryRow
{
    public int CourseId { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? CategoryName { get; set; }
    public int AssignmentCount { get; set; }         // จำนวน assignment ที่มีคอร์สนี้
    public int EnrolledLearners { get; set; }        // distinct LearnerCode
    public int CompletedCount { get; set; }
    public int OverdueCount { get; set; }
    public double AvgProgress { get; set; }
    public double CompletionRate { get; set; }
    public double? AvgScore { get; set; }            // null เมื่อไม่มีคะแนน
}

// ── GET api/Reports/activity?months=12 ──
public class ActivityReportDto
{
    public DateTime GeneratedAt { get; set; }
    public List<ActivityMonthRow> Months { get; set; } = new(); // เรียงเก่า→ใหม่ ครบทุกเดือน (เดือนไม่มีข้อมูล = 0)
}
public class ActivityMonthRow
{
    public string Month { get; set; } = string.Empty; // "2026-07" (yyyy-MM)
    public int Completions { get; set; }              // enrollment ที่ CompletedDate ในเดือนนั้น
    public int ActiveLearners { get; set; }           // distinct LearnerCode ที่มี LearningLog ในเดือน
    public int NewEnrollments { get; set; }           // enrollment CreatedAt ในเดือน
    public double TotalHoursPlayed { get; set; }      // sum TotalSecondsPlayed / 3600 ปัดตาม double
}
```

### 2. ไฟล์ใหม่ `iLearn.API/Controllers/ReportsController.cs` + service

- Route `api/Reports`, ทุก endpoint คืน wrapper `Ok(new { success = true, data = <dto> })` (pattern เดียวกับ `Assignments/dashboard/{id}`)
- แยก logic ลง `iLearn.Application/Services/ReportService.cs` + `IReportService` (DI ตาม pattern service อื่น) — controller บาง
- **Endpoints:**
  - `GET api/Reports/compliance` — org-wide; enrollment ที่นับ = ทุก enrollment ไม่ถูกลบ (soft-delete filter ตาม pattern repo เดิม)
  - `GET api/Reports/transcript/{learnerCode}` — 404 `{ success = false, message }` เมื่อไม่พบ learner (ไม่มี enrollment เลย + EmployeeHub ไม่รู้จัก)
  - `GET api/Reports/course-summary`
  - `GET api/Reports/activity?months=12` — clamp 3–24, default 12
- **Authorization:** `[Authorize]` + division scoping แบบเดียวกับ `AssignmentsController` (`EnsureDivisionAccess` / divisionId claim ของ admin ปกติ — super admin เห็นทั้งหมด) — ดู precedent ใน AssignmentsController บรรทัด ~93/123/165 แล้วทำให้ semantics เดียวกัน: admin ที่ถูก scope division เห็นเฉพาะข้อมูล assignment ใน division ตัวเอง

### 3. กติกา performance (บังคับ)

- Aggregate (count/group) ทำใน SQL ผ่าน EF `GroupBy` ก่อน `ToList()` ให้มากที่สุด — **ห้ามดึง enrollment ทั้งบริษัทมานับใน memory** ยกเว้นส่วนที่ต้อง join EmployeeHub (division/department) ซึ่ง SQL ทำไม่ได้ → ดึงเฉพาะ field ที่ใช้ (projection) แล้ว join ใน memory
- Learner name/division: `GetLearnersByCodesAsync` **ครั้งเดียวต่อ request** ด้วย distinct codes ที่ปรากฏในผลลัพธ์
- **ห้าม Include/โหลด `FileStorage` เด็ดขาด** (กติกา CLAUDE.md)
- `activity`: filter `CreatedAt >= cutoff` ก่อน GroupBy; ถ้าช้าให้ log duration + จดใน Implementer Notes (index เป็นงานแยก)
- ห้าม N+1 ทุกกรณี

### 4. Unit tests (`iLearn.Tests/ReportServiceTests.cs`)

- compliance: นับ open/completed/overdue + distinct learners ถูก (mock repo in-memory ตาม pattern test เดิม), overdue ใช้ status key เดียวกับ AssignmentService
- transcript: learner มี 3 enrollment → rows ครบ + สถิติหัวถูก; learner ไม่มีข้อมูล → KeyNotFoundException/404
- activity: เดือนที่ไม่มีข้อมูลต้องมี row ค่า 0 (ครบทุกเดือนตาม months), completions นับจาก CompletedDate
- course-summary: distinct learners + avg ถูก

## Contract ที่เปลี่ยน

- ใหม่ทั้งหมด (DTOs + 4 endpoints ตามข้อ 1–2) — **frozen สำหรับ PLAN-087**; ถ้าจำเป็นต้องเบี่ยง shape: อัปเดต section contract ของทั้ง 2 แผน + ลง AGENT_LOG ทันที ก่อน Gemini ปิดงาน
- DB schema: **ไม่เปลี่ยน**

## นอก Scope (ห้ามทำ)

- ห้ามแตะ React (PLAN-087 ของ Gemini ทำคู่ขนาน)
- ห้ามแตะ MVC admin เดิม / endpoint เดิมทุกตัว
- ไม่มี migration / ไม่เพิ่ม index ในแผนนี้ (ถ้าเจอ query ช้า จดไว้)
- ไม่ทำ xlsx / scheduled email (Phase ถัดไป)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

ทดสอบมือ (Swagger/curl กับ API local):

1. `GET api/Reports/compliance` → ตัวเลขรวม + ByDivision/ByDepartment สอดคล้องกัน (sum ลูก = แม่), OverdueRows ตรงกับที่เห็นในหน้า assignment ที่มี overdue
2. `GET api/Reports/transcript/{code}` ด้วย learner จริง (เช่นจาก TEST-03/04 ที่ใช้ทดสอบ PLAN-079) → rows ครบ
3. `GET api/Reports/activity?months=6` → 6 rows ครบทุกเดือน
4. `GET api/Reports/course-summary` → คอร์สที่มี enrollment ปรากฏครบ, ไม่มี timeout
5. วัดเวลา response ทุก endpoint กับข้อมูล PROD-scale (ถ้ามี QA data) — จดตัวเลขลง Implementer Notes

## Implementer Notes

- DTO shape ตรง contract ทุกประการ — ไม่มีการเบี่ยง
- Division scoping: enrollment ไม่มี DivisionId โดยตรง → scope ผ่าน EnrollmentAssignment → Assignment.DivisionId (Join in `BuildDivisionScopedEnrollmentQuery`). SuperAdmin (null divisionId) เห็นทั้งหมด
- Activity endpoint: learning log scoped ผ่าน enrollment IDs เมื่อ divisionId != null. ถ้า enrollment จำนวนมาก อาจช้าเพราะ `Contains(enrollmentId)` → ถ้าเจอปัญหาควรเพิ่ม index หรือ subquery
- Course summary: ใช้ EF GroupBy ใน SQL สำหรับ aggregate (distinct count, avg) ก่อน ToListAsync — ไม่ดึง enrollment ทั้งหมดมานับใน memory
- Compliance: ต้องดึง enrollment projection มา join กับ EmployeeHub (division/department) ใน memory เนื่องจาก SQL ทำ join กับ external API ไม่ได้ — ใช้ `GetLearnersByCodesAsync` bulk ครั้งเดียว
- Tests: ใช้ custom async query provider (TestAsyncEnumerable/TestAsyncQueryProvider) เพื่อให้ in-memory IQueryable รองรับ EF ToListAsync
- Verified: `dotnet build` 0 errors, `dotnet test` 185 passed 0 failed (7 new tests)
- Manual testing: ยังไม่ได้ทดสอบกับ API running (ไม่มี dev server ในขณะทำงาน) — ต้องทดสอบมือภายหลัง

## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็ม + build/test อิสระ (`dotnet build` 0 warn/err, `dotnet test` **185 passed** — 7 test ใหม่ครบเคสตามแผน §4):

- **Contract:** `ReportDtos.cs` ตรง contract freeze §1 ทุก field ✅
- **Performance:** projection ทุก query (ไม่โหลด entity เต็ม/ไม่มี FileStorage), `GetLearnersByCodesAsync` bulk ครั้งเดียว, course-summary + activity GroupBy ใน SQL, compliance ดึง projection มา join EmployeeHub ใน memory (ตามที่แผนอนุญาต) ✅
- **Auth:** `[Authorize(Policy = "AdminOnly")]` (มีจริงใน AuthorizationExtensions.cs:25) + `_currentUser.DivisionId` scoping ผ่าน `BuildDivisionScopedEnrollmentQuery` (subquery EXISTS ผ่าน link→assignment.DivisionId) — semantics ตรง precedent ✅
- **Controller:** wrapper `{success, data}`, 404 transcript, clamp months ✅ DI ลงทะเบียน ✅

### ⚠️ Finding 1 (MEDIUM-HIGH — ต้องแก้ก่อนใช้จริง): overdue ใช้ `Enrollment.DueDate` ดิบ → ตัวเลขเพี้ยนหลัง Extend Due Date
- แผนกำหนด "ใช้ field ชุดเดียวกับ AssignmentService เพื่อให้ตัวเลขตรงกับหน้า assignment" แต่ ReportService ใช้ `Enrollment.StartDate/DueDate` ตรง ๆ ทั้ง compliance/transcript/course-summary
- **หลักฐาน divergence:** `ExtendDueDateAsync` (ทั้ง 2 ตัว) อัปเดต `Assignment.DueDate` + `EnrollmentAssignment.DueDate` แต่**ไม่เคยแตะ `Enrollment.DueDate`** (เซ็ตครั้งเดียวตอนสร้าง); หน้า assignment dashboard อ่านจาก link rows (AssignmentService:897) และ learner side ใช้ `GetEffectiveSchedule` (EnrollmentsController: active links → `Max(link.DueDate)` fallback enrollment) — ดังนั้นหลัง admin กด Extend Due Date รายงานจะยังนับ learner เป็น **Overdue** ทั้งที่หน้า assignment/ฝั่ง learner บอก In Progress — ผิดตรงประชากรที่ admin สนใจที่สุดพอดี
- **แก้:** projection ใน ReportService คำนวณ effective dates ตาม semantics ของ `GetEffectiveSchedule`: `EffectiveDueDate = AssignmentLinks(ที่ไม่ deleted).Max(DueDate) ?? e.DueDate`, `EffectiveStartDate = Min(StartDate) ?? e.StartDate` แล้วใช้แทนทุกจุดที่เช็ค overdue/status (แนะนำย้าย/แชร์ logic effective schedule เป็น helper กลางเพื่อไม่ duplicate)
- หมายเหตุ: hand-rolled `!IsCompleted && DueDate < now` เทียบเท่า status Overdue จริง (Upcoming เป็นไปไม่ได้เมื่อ DueDate < now เพราะ validation StartDate ≤ DueDate) — ประเด็นไม่ใช่สูตร แต่คือ **แหล่งข้อมูล date**

### Finding 2 (MINOR/side observation): enrollment ที่ assignment ถูกลบทั้งหมด
`GetEffectiveSchedule` ฝั่ง learner ซ่อน enrollment ที่เหลือแต่ link ของ assignment ที่ถูกลบ (`hadDeletedAssignmentOnly → ShouldBeVisible=false`) แต่รายงานยังนับ — ตัวเลข TotalLearners/Open อาจสูงกว่าที่ learner เห็นจริง พิจารณา exclude ตอนแก้ Finding 1 (ใช้ helper เดียวกันจะได้ semantics นี้ฟรี)

**สรุป: สถาปัตยกรรม/performance/contract ผ่านหมด — ติด Finding 1 เรื่องแหล่ง DueDate ที่ต้องแก้ก่อนเปิดใช้รายงานจริง (ตัวเลขจะขัดกับหน้า assignment เมื่อมีการ extend due date)**

## Fix Findings (Claude Code, 2026-07-14 — ผู้ใช้สั่งแก้เอง)

- **Finding 1+2 แก้แล้วใน `ReportService.cs`:** เพิ่ม `VisibleEnrollmentPredicate` + `BuildVisibleEnrollmentRowsQuery(divisionId, learnerCode?)` — projection กลางที่ (ก) ซ่อน enrollment ที่ links ชี้ assignment ถูกลบทั้งหมด (semantics เดียวกับ `GetEffectiveSchedule` ฝั่ง learner) (ข) คำนวณ effective dates: มี active links → `Min(link.StartDate)`/`Max(link.DueDate)`, ไม่มี → fallback enrollment columns. compliance/transcript/course-summary เปลี่ยนมาใช้ projection นี้ทั้งหมด; activity เพิ่ม visibility filter (completions/newEnrollments)
- **หมายเหตุ transcript:** enrollment ของ assignment ที่ถูกลบหายจาก transcript ด้วย (ตาม visibility เดียวกับ learner) — ถ้า audit ต้องการเห็นประวัติของ assignment ที่ลบแล้ว เปิดเป็น decision แยก
- **Regression tests เพิ่ม 2 ตัว** (`Compliance_UsesLinkDueDate_ExtendedLearnerIsNotOverdue`, `Compliance_ExcludesEnrollmentWhoseOnlyAssignmentIsDeleted`) → `dotnet test` **187 passed** (build 0 errors; warnings ที่เห็นเป็น nullable เดิมของ project ไม่ใช่ไฟล์ report — ยืนยันด้วย grep)
- **⚠️ ยังต้อง smoke บน SQL Server จริง (QA):** ทุก endpoint — โดยเฉพาะ course-summary ที่ GroupBy ทับ projection ที่มี correlated subquery (EF9 ควร translate ได้ แต่ unit test เป็น LINQ-to-objects พิสูจน์ SQL translation ไม่ได้) — เป็นข้อแรกของ verification checklist เดิมอยู่แล้ว
