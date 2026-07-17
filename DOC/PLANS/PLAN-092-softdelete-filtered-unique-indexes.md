# PLAN-092: Bugfix — unique index ไม่ filter soft-delete ทำให้ add คอร์สที่เคยลบกลับเข้า batch ไม่ได้ (500)

- **Status:** DONE (Claude Code แก้เองตามคำสั่งผู้ใช้ 2026-07-17) — เหลือ apply migration บน QA/PROD DB
- **Assigned/Implementer:** Claude Code (ผู้ใช้สั่งให้แก้เองโดยตรง)
- **สร้างเมื่อ:** 2026-07-17

> ผู้ใช้รายงานจาก QA (`/assignments/306`): คอร์ส "Software back up (Re.3)" เคยอยู่ใน batch แล้วถูกลบออก พอ Add Courses กลับเข้าไป → `POST Assignments/306/courses` **500**

---

## Root cause (ยืนยันจากโค้ดครบทุกข้อ)

1. ลบคอร์สออกจาก batch = **soft delete** (`AssignmentService.RemoveCourseFromAssignmentAsync`: `targetRule.IsDeleted = true` + `link.IsDeleted = true`) — row ยังอยู่ใน DB
2. ตอน add กลับ `AddCoursesToAssignmentAsync` เช็คคอร์สซ้ำผ่าน `LoadBatchAsync` ซึ่งวิ่งผ่าน global query filter `!IsDeleted` → **มองไม่เห็น rule ที่ถูกลบ** → ถือเป็นคอร์สใหม่ → INSERT rule ใหม่ด้วย `(AssignmentNo, CourseId)` เดิม
3. Unique index `IX_Assignments_AssignmentNo_CourseId` filter แค่ NOT NULL — **ไม่ filter `IsDeleted`** → เห็น row ที่ตายแล้ว → duplicate key → SqlException → 500

ระบบเป็น soft-delete ทั้งระบบ แต่ unique index บังคับความซ้ำรวม row ที่ลบแล้ว = ขัดกันเชิงออกแบบ — และ **มี precedent การแก้ที่ถูกต้องอยู่แล้วในโค้ดเอง**: index ของ `ScormRuntimeState (EnrollmentId, ContentItemId)` ใช้ `.HasFilter("[IsDeleted] = 0")` มาก่อน (เคยเจอบั๊กเดียวกันสมัย player reset)

## การแก้

Migration `20260717011356_SoftDeleteFilteredUniqueIndexes` — drop/recreate unique index 3 ตัวให้เป็น filtered `[IsDeleted] = 0` (ตัวที่พังจริง 1 + ตัวที่มีความเสี่ยงแฝงเดียวกันอีก 2):

| Index | ตาราง | Filter ใหม่ |
|---|---|---|
| `IX_Assignments_AssignmentNo_CourseId` | Assignments | `NOT NULL ×2 AND [IsDeleted] = 0` |
| `IX_AssignmentCourses_AssignmentId_CourseId` | AssignmentCourses | `[IsDeleted] = 0` |
| `IX_EnrollmentAssignments_EnrollmentId_AssignmentId` | EnrollmentAssignments | `[IsDeleted] = 0` |

- Fluent config ใน `AppDbContext` อัปเดตตรงกัน (แนวเดียวกับ ScormRuntimeState เดิม) + คอมเมนต์อธิบายเหตุผล
- **ไม่แตะ business logic เลย** — add จะสร้าง rule ใหม่สะอาด ๆ, row เก่าเก็บเป็นประวัติต่อไป
- สร้าง filtered index จากข้อมูลเดิมปลอดภัยเสมอ (uniqueness เดิมเข้มกว่า filter ใหม่)
- Snapshot diff ตรวจแล้ว: มีแค่ 3 filter นี้ ไม่มี model change อื่นปน (แม้ทำขณะ working tree มีงาน PLAN-090 ของ Copilot ค้างอยู่)

## Verified

- `dotnet build` 0 errors + `dotnet test` **203 passed** (รวม tests ของ PLAN-090 ที่อยู่ใน tree)
- `dotnet ef migrations list --connection <QA>` ยืนยันสถานะจริงก่อนแก้: `AddStoragePathToFileStorage` apply แล้ว (SCORM บน QA ไม่ได้พังอย่างที่กังวล), **`AddNotifications` Pending = ต้นเหตุ unread-count 500**

## คงค้าง (ops — ผู้ใช้รันเอง เครื่องมือ agent ถูกบล็อกไม่ให้แก้ schema DB ตรง)

```powershell
# จาก repo root — apply ทั้ง AddNotifications (แก้ bell 500) + SoftDeleteFilteredUniqueIndexes (แก้ add คอร์สกลับ)
dotnet ef database update --project iLearn.Infrastructure --startup-project iLearn.API `
  --connection "Data Source=AP-NTC2138-QADB;Database=iLearnDB_New;User ID=sa;Password=<QA-sa-password>;Trust Server Certificate=True"
```

- ต้อง **deploy build ที่มี migration นี้ขึ้น QA** ด้วย (หรือรันจาก repo local ได้เลย — ตัว update วิ่งตรงเข้า DB ไม่เกี่ยว server)
- PROD: รันแบบเดียวกันด้วย connection `AP-NTC2139-COSS` **ตอน rollout รอบหน้า** (PROD ยังไม่ได้ deploy build ที่มี Notifications จึงยังไม่เดือดร้อน แต่ migration ต้องไปพร้อม build เสมอ)
- ทดสอบหลังรัน: (1) bell บน QA หาย 500 (2) `/assignments/306` → Add Courses → "Software back up (Re.3)" → สำเร็จ
- **กติกา deploy ที่ควรจำ (เพิ่มใน CLAUDE.md แล้วรอบหน้า): deploy build ที่มี migration ใหม่ = ต้องรัน `dotnet ef database update` คู่กันเสมอ** — ครั้งนี้ deploy PLAN-088 โดยไม่รัน migration คือต้นเหตุ bell 500 ทั้งก้อน
