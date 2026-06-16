# System Analysis — iLearn (ภาพรวมทั้งระบบ)

สังเคราะห์ภาพรวมทั้งระบบ iLearn — สถาปัตยกรรม, สุขภาพรายด้าน, สิ่งที่ปิดไปแล้วในรอบนี้, และความเสี่ยง/หนี้ที่ยังเหลือ

> เขียนโดย Claude Code (planner/reviewer) 2026-06-15 — เป็น **synthesis** ของ `api_analysis.md`, `division_isolation_analysis.md`, `ux_ui_analysis.md` + งานทั้ง session (รายละเอียดเชิงลึกอยู่ใน 3 ไฟล์นั้น)

---

## 1. ภาพรวมระบบ (8 projects)

| Project | บทบาท | สถานะ |
|---|---|---|
| **iLearn.API** | Backend API (.NET 9, Clean Arch) — 32 controllers, Windows auth + HMAC learner proxy | active |
| **iLearn.Application** | Use cases/services (9), DTOs (33), interfaces, policies | active |
| **iLearn.Domain** | Entities (22), enums, BaseEntity, ApiResponse | active |
| **iLearn.Infrastructure** | EF Core, repositories, external integrations (LearnerApiService) | active |
| **iLearn.Admin.React** | Admin SPA (React 19 + Vite 8 + Tailwind 4) — 27 pages, 25 components | active (หลัก) |
| **iLearn.Admin** | Admin เดิม (ASP.NET MVC + DevExtreme) | **frozen** (ห้ามแก้เว้นแต่สั่ง) |
| **iLearn.User** | Learner-facing (SCORM player) — เรียก API ผ่าน HMAC proxy | active |
| **iLearn.Tests** | xUnit — **118 tests ผ่านทั้งหมด** | active |

**Data flow:** Admin.React → (Windows auth) → iLearn.API → EF/SQL + external EmployeeService; iLearn.User → (HMAC-signed) → iLearn.API (learner endpoints)

---

## 2. Health Scorecard รายด้าน

| ด้าน | คะแนน | สรุป |
|---|---|---|
| **Security / Auth** | 🟢 แข็งแรง | secure-by-default (FallbackPolicy), HMAC learner proxy (constant-time), ProblemDetails middleware, secret validation ตอน boot, policy audit ตอน startup |
| **Division isolation** | 🟢 ครบ | entity ที่มี division isolate ครบทุก service/controller; ตัวที่ไม่มี กั้นด้วย SuperAdminOnly — **ไม่มีข้อมูลรั่วข้าม division** (ดู `division_isolation_analysis.md`) |
| **Frontend มาตรฐาน** | 🟢 ดี | list = AppTable (infinite scroll), detail = shared components (DetailLayout/Fact), editor = AppWizard, explorer = useExplorer ร่วม 2 หน้า; lint **0/0**, console debug = 0 |
| **Backend structure** | 🟡 ปานกลาง | Clean Arch ดี แต่ controller ใหญ่ยังเหลือ (Assignments 1316, ContentItems 1179, LearnerGroup ~1100); refactor→service เริ่มแล้ว (Enrollments, Courses) |
| **API contract** | 🟡 ปานกลาง | response ปนกัน typed DTO/anonymous + หลาย route convention; contract sync พึ่งวินัย (กติกา `// Mirrors`) |
| **Test coverage** | 🟡 ปานกลาง | 118 unit/integration ผ่าน แต่ไม่ครอบ frontend + dashboard query กับ SQL จริง (เคยพลาด EF translation) |
| **Tech debt / dead code** | 🟢 สะอาดขึ้น | dead CSS/component ถูกเก็บ (PLAN-029/030), lint baseline 0, ลด class ซ้ำ (PLAN-031 กำลังทำ) |

---

## 3. สิ่งที่ปิด/แข็งแรงขึ้นในรอบนี้ (จาก plan VERIFIED)

- 🔴→✅ **FileStoragesCRUD** ดัมพ์ SCORM blob ทั้งหมด — **ลบ controller** (PLAN-013)
- 🟠→✅ **LearnerApiService กลืน exception** — ใช้ ILogger + propagate + แยก 4xx/5xx→ProblemDetails/502 (PLAN-014)
- ✅ **Search ทั้งระบบ** — แก้ field casing/projection (Learners NID, Users enrich, enrollments/roles) (PLAN-009/011/012/016)
- ✅ **Division ให้ SuperAdmin เลือกได้** ตอน create/edit learner group/category (PLAN-022/023)
- ✅ **Learning Logs** UI ↔ API SuperAdmin-only สอดคล้อง (PLAN-021)
- ✅ **Courses → Explorer** + shared useExplorer (PLAN-020/026)
- ✅ **Detail pages มาตรฐาน** (shared components) + AppTable flicker + dead code (PLAN-007/008/028/029/030)

---

## 4. ความเสี่ยง / หนี้ที่ยังเหลือ (consolidated — เรียงตามความสำคัญ)

### 🟠 MEDIUM
1. **Content Library ไม่มี division** — admin ทุกคนเห็น content ทั้งหมด ถ้าตั้งใจเป็นคลังกลาง = OK; ถ้าต้องการแยก ต้องเพิ่ม division dimension (งานใหญ่) — **open decision** (`division_isolation_analysis.md` §5.2)
2. **Controller ใหญ่** — Assignments/ContentItems/LearnerGroup ยังไม่ refactor (pilot ทำแค่ Enrollments/Courses) — ทยอยต่อ
3. **Global records visibility** — record `DivisionId = null` ที่ SuperAdmin สร้าง division-admin มองไม่เห็น (strict equality) — policy decision

### 🟡 LOW
4. **API style fragmentation** — หลาย route convention + response shape ปนกัน → ควรมี API style guide กลาง
5. **anonymous → DTO** — ทยอยทำ (pilot Dashboard/Courses เสร็จ) controller อื่นยังปน
6. **Test coverage gap** — ไม่มี integration test ที่ยิง endpoint กับ SQL จริง (เคยพลาด EF translation ใน dashboard) + ไม่มี frontend test
7. **`--admin-*` CSS design tokens** ที่ utility usage = 0 (palette ยังไม่ใช้ครบ) — design decision ว่าจะคงหรือตัด

---

## 5. ข้อเสนอลำดับถัดไป (priority)

1. **ตัดสินใจ Content Library division** (open decision ที่ค้างนานสุด — กระทบ data model)
2. **เดินหน้า refactor controller ใหญ่** ทีละตัว (Assignments เป็นตัวต่อไป) + ทยอย anonymous→DTO
3. **เพิ่ม integration test** สำหรับ endpoint ที่ frontend ใช้บ่อย (กัน EF/contract regression แบบ dashboard)
4. **API style guide** กลาง (route + envelope + error shape) ลด fragmentation
5. (ต่อเนื่อง) PLAN-031 shared detail primitives + tech-debt cleanup

---

## 6. แฟ้มวิเคราะห์เชิงลึก (sub-analyses)

- `DOC/api_analysis.md` — backend API ภาพรวม + endpoint surface
- `DOC/division_isolation_analysis.md` — data isolation ทั้งระบบ
- `DOC/ux_ui_analysis.md` — UX/UI patterns + conventions ฝั่ง React
- `DOC/PLANS/` — แผนงาน 020–031 (สถานะ: 10 VERIFIED, 1 DONE, 1 READY)
- `DOC/AGENT_LOG.md` — บันทึกงานทุก agent
