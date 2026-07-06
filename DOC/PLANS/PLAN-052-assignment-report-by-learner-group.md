# PLAN-052: Assignment Report — เปลี่ยนสรุป "By Department" เป็น "By Learner Group"

- **Status:** DONE
- **Assigned:** Antigravity (Gemini) — full-stack ชิ้นเล็ก (backend enrich 1 จุด + UI 1 หน้า, contract sync ในงานเดียวกัน)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-06
- **อ้างอิง:** [PLAN-050](PLAN-050-assignments-learner-mgmt-reporting.md) (Part C เป็นคนเพิ่ม By Department)

> คำขอผู้ใช้ (2026-07-06): หน้า `assignments/:id/report` — การ์ด "By Department" ไม่ได้ใช้งานจริง เพราะการจัดคน ดูรายงาน ทำผ่าน **learner groups** เป็นหลัก → เปลี่ยนมิติสรุปเป็น By Learner Group

---

## บริบทที่วิเคราะห์แล้ว (อ่านโค้ดจริง 2026-07-06)

- หน้า report: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx` — `departmentSummaries` (บรรทัด ~136) group `data.learners` ด้วย `row.department` → การ์ด "By Department" (บรรทัด ~321, icon `Building2`)
- ข้อมูลมาจาก `GET Assignments/dashboard/{id}` → `AssignmentService.BuildAssignmentDashboardAsync` (`iLearn.Application/Services/AssignmentService.cs:815`)
- `LearnerProgressDto` (`iLearn.Application/DTOs/AssignmentDashboardDto.cs:47`) **ยังไม่มีข้อมูลกลุ่ม** ของ learner แต่ละคน — มีแค่ `Division`/`Department` จาก enrichment ภายนอก
- ระดับ assignment มี `LearnerGroupId`/`LearnerGroupName` เดียว (กลุ่มที่ใช้ตอนสร้าง) — **ไม่พอ** เพราะ learner ถูกเพิ่มทีหลังรายคน/bulk ได้ และคนหนึ่งอยู่ได้หลายกลุ่ม
- โครงสร้างกลุ่ม: `LearnerGroupMember { LearnerGroupId, LearnerCode }` → join `LearnerGroup.Name` ได้ตรง ๆ (`iLearn.Domain/Entities/LearnerGroupMember.cs`)
- `AssignmentService` ยังไม่มี repo ของ `LearnerGroupMember` — ต้อง inject `IGenericRepository<LearnerGroupMember>` เพิ่ม (pattern เดียวกับ repo อื่นใน constructor)

---

## Scope

### 1. Backend — enrich `LearnerProgressDto` ด้วยชื่อกลุ่ม

- [ ] `AssignmentDashboardDto.cs`: เพิ่ม `public List<string> LearnerGroups { get; set; } = new();` ใน `LearnerProgressDto`
- [ ] `AssignmentService`: inject `IGenericRepository<LearnerGroupMember>`
- [ ] ใน `BuildAssignmentDashboardAsync` (หลังได้ `uniqueLearnerCodes` ~บรรทัด 909): query เดียว —
  `members.Where(m => uniqueLearnerCodes.Contains(m.LearnerCode) && !m.IsDeleted)` join `LearnerGroup` (กรอง `!IsDeleted` + **division isolation แบบเดียวกับ `LearnerGroupService.GetAllAsync`** — ดู logic isolation ที่นั่นแล้ว mirror ให้ตรง) → select `{ m.LearnerCode, g.Name }` → group เป็น `Dictionary<string, List<string>>` (OrdinalIgnoreCase)
- [ ] ตอน map `LearnerProgressDto` (~บรรทัด 982): เติม `LearnerGroups = groupsByCode.GetValueOrDefault(row.LearnerCode) ?? []` (เรียงชื่อ A→Z)
- ❌ ห้าม N+1 — ต้องเป็น query เดียวครอบทุก code; ห้ามโหลด `Members` collection ผ่าน Include ทั้ง entity

### 2. Frontend — `AssignmentReportPage.tsx`

- [ ] `LearnerRow` type: เพิ่ม `learnerGroups?: string[] | null` (อัปเดตคอมเมนต์ `// Mirrors LearnerProgressDto (...)`)
- [ ] แทนที่ `departmentSummaries` + การ์ด "By Department" ด้วย **"By Learner Group"**:
  - bucket ต่อชื่อกลุ่ม; learner ที่อยู่หลายกลุ่มนับใน **ทุกกลุ่ม** ที่สังกัด; ไม่อยู่กลุ่มใดเลย → bucket `"Ungrouped"` (แสดงท้ายสุด)
  - คอลัมน์เดิม: Learners / Enrollments / Completed / Overdue / Completion % (ใช้ `formatPercent` เดิม)
  - icon เปลี่ยนจาก `Building2` → `Users` (lucide)
- [ ] เพิ่ม **dropdown filter "Group"** ข้าง course filter (ค่า: All + ชื่อกลุ่มที่พบใน data + Ungrouped) — filter แถวตาราง learners ด้วย
- [ ] search: เพิ่ม match ชื่อกลุ่ม (`row.learnerGroups`)
- [ ] CSV export: เพิ่มคอลัมน์ `Learner Groups` (join ด้วย `"; "`) — คง Division/Department ไว้ตามเดิม
- [ ] คอลัมน์ division · department ใต้ชื่อในตาราง learners **คงไว้** (มีประโยชน์ระดับ row) — เอาออกเฉพาะการ์ดสรุป

### 3. กติกาที่ต้องตาม (README React)

- ใช้ `Card`, `formatPercent`, `DETAIL_TABLE_CHUNK_SIZE` เดิม — ห้าม hardcode pill/spinner/format
- Contract sync: type mirror comment ต้องชี้ DTO + path จริง

---

## Constraints

- ❌ ห้ามแตะ endpoint อื่น / เปลี่ยน shape ของ field ที่มีอยู่ — **เพิ่ม field ใหม่เท่านั้น** (backward compatible; หน้า AssignmentDetailPage ใช้ dashboard DTO เดียวกัน ต้องไม่พัง)
- ❌ ห้ามเพิ่ม HTTP call ใหม่จาก frontend — ใช้ endpoint dashboard เดิม
- ✅ ถ้า `learnerGroups` ไม่มีในการตอบกลับ (API เก่า) frontend ต้องไม่พัง (optional field + fallback `[]`)

## Decision points — ✅ ผู้ใช้ยืนยันแล้ว (2026-07-06)

1. การ์ด By Department: **ลบทิ้งเลย** (ไม่ต้องมี toggle ใด ๆ)
2. Learner ที่ไม่อยู่กลุ่มไหน: **แสดง bucket "Ungrouped"** ท้ายตาราง
3. ป้ายชื่อการ์ด: **"By Learner Group"**

## Verification

```powershell
# React (จาก iLearn.Admin.React)
npm run lint ; npm run build
# Backend
dotnet build iLearn.Tests -o artifacts\verify-test ; dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

- [x] E2E บน QA: `https://ap-ntc2138-qawb/iLearn/admin-react/assignments/274/report` — การ์ด By Learner Group แสดงกลุ่มถูกต้อง, filter Group ทำงาน, CSV มีคอลัมน์กลุ่ม, learner หลายกลุ่ม/ไม่มีกลุ่มนับถูก
- [x] หน้า `assignments/274` (detail) ยังทำงานปกติ (ใช้ dashboard endpoint เดียวกัน)

## Implementer Notes

- พัฒนาเสร็จสิ้นตามขอบเขตงานในแผน 100%
- ได้ทำการตรวจสอบด้วย .NET Unit Tests (118/118 passed) และ npm run lint/build บน React เรียบร้อย (ผ่านทั้งหมดไม่มี error)
- ได้อัปเดตโมเดลทั้งในฝั่ง API/DTOs และโมเดลฝั่ง React ทั้งสองหน้าจอ (Detail & Report) ให้ Sync กันอย่างสมบูรณ์แบบ

## Reviewer Sign-off (Claude Code — 2026-07-06)

ตรวจ diff + รัน verification ซ้ำเอง + deploy — **ผ่าน อนุมัติปิดงาน**

- **Backend:** query เดียวไม่มี N+1, กรอง `IsDeleted` ทั้ง member และ group, division isolation ตรงกับ `LearnerGroupService.GetAllAsync` เป๊ะ, เรียงชื่อ A→Z, additive field ไม่กระทบ consumer เดิม (Detail page sync type แล้วด้วย) ✅
- **Frontend:** การ์ด By Learner Group + Ungrouped ท้ายสุด + Group filter (All/กลุ่ม/Ungrouped) + search จับชื่อกลุ่ม + CSV column + `isFiltered`/`visibleRows` ครบ, ใช้ `Card`/`formatPercent` ตาม conventions ✅
- **Verify ซ้ำเอง:** eslint clean, vite build ผ่าน, dotnet test 118/118, MVC admin build 0 errors ✅
- **Deploy + E2E:** QA + PROD (API stamp `20260706120855`/`20260706121657`, React robocopy OK, admin stamp `20260706121204`/`20260706122023`) — `dashboard/274` ตอบ `learnerGroups` แล้วทั้งสอง env ✅
- **ข้อสังเกต (ไม่ blocking):** ทั้ง QA และ PROD DB ยังไม่มี learner group เลย (`LearnerGroups` = 0) → ตอนนี้ทุกคนขึ้น "Ungrouped" ตามสเปก — การ์ดจะมีประโยชน์เต็มที่เมื่อเริ่มสร้างกลุ่มและใส่สมาชิก
