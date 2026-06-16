# PLAN-034: ยุบ Course เหลือ 3 สถานะ (Draft/Open/Closed) — ตัด Retired(3) ที่ตายแล้ว + แก้ label ให้ตรง

- **Status:** DONE
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Medium
- **Estimated scope:** enum + ~6 backend refs + frontend CourseDetailPage/CourseStatusBadge + tests

## Problem (จาก lifecycle audit)

`CourseStatus` มี 4 ค่า `Draft=0, Open=1, Closed=2, Retired=3` แต่ **wired ไม่ตรงกัน**:
- **Frontend** ใช้ `2 = "Retired"` (`CourseDetailPage`: `isRetired = status === 2`, ปุ่ม "Retire Course" → `onStatusChange(2)`) — **ผิด** เพราะ backend `2 = Closed`
- **Retired(3) ตายสนิท** — UI ไม่เคยส่ง 3, ไม่มี flow set 3, ไม่มี DB row; guard `if (status == Retired)` ใน `CourseService:434` (บล็อกเมื่อมี open enrollment) จึงไม่เคยทำงาน
- **Closed(2) ใช้งานจริง** — `CanLearnerAccess = Open || Closed` (learner เข้า Closed ได้, Retired เข้าไม่ได้) ใน `Course.cs:21`, `EnrollmentsController:83/204/358`, `CoursesCRUDController`, `CourseVersionService:687`
- **Badge tone เพี้ยน** — `CourseStatusBadge`: `statusCode 2 → danger(แดง)`, `3 → neutral(เทา)` + string `'closed'→neutral`, `'retired'→danger` → status 2 แสดง text "Closed" แต่สีแดง

**ผู้ใช้ตัดสินใจ:** ยุบเหลือ 3 สถานะ — **ตัด Retired(3) (ตัวที่ตาย), เก็บ Closed(2)** เป็นสถานะที่ 3 (พฤติกรรม learner-access เดิมไม่เปลี่ยน)

## เป้าหมาย: Draft(0) / Open(1) / Closed(2) — ป้าย+โค้ดตรงกันทั้ง FE/BE

## Scope (ทำแค่นี้)

### Backend (ตัด Retired(3))
1. `iLearn.Domain/Enums/CourseStatus.cs` — ลบ `Retired = 3` (เหลือ Draft/Open/Closed)
2. ลบ/แก้ทุก reference ของ `CourseStatus.Retired`:
   - `CourseService.cs:434` — **ลบ guard block `if (status == Retired)`** (dead — ไม่เคยทำงานเพราะ UI ไม่ส่ง 3); behavior ไม่เปลี่ยน
   - `CourseService.cs:83,97` (`GetAllCoursesAsync` filter) — `{Draft, Closed, Retired}` → `{Draft, Closed}`
   - `Course.cs:22` `IsRetired => Status == Retired` — ลบ (dead) หรือถ้ามีผู้ใช้ ให้ repoint
   - `CoursesCRUDController:45,121` branch `Status == Retired ? "Retired"` — ลบ branch
   - `CourseVersionService:687` `(Closed || Retired) && IsActive` → `Closed && IsActive`
   - **คงไว้:** `CanLearnerAccess = Open || Closed` และทุกที่ที่ใช้ Closed (พฤติกรรม learner-access เดิม)
3. tests ที่อ้าง `CourseStatus.Retired` (`LifecycleContractDtoTests:84`) → เปลี่ยนเป็น `Closed` หรือปรับให้ผ่าน

### Frontend (แก้ label ที่ผิด + badge)
4. `CourseDetailPage.tsx`:
   - `isRetired = status === 2` → **rename เป็น `isClosed`** (ค่า 2 = Closed ตาม backend) + อัปเดต prop/ปุ่ม
   - ปุ่ม "Retire Course" → **"Close Course"** (ส่ง `onStatusChange(2)` เหมือนเดิม — แค่ป้ายตรง), title "already Retired" → "already Closed"
   - (ค่าที่ส่ง 0/1/2 ถูกแล้ว ไม่ต้องเปลี่ยน — แก้แค่ป้าย/ตัวแปร)
5. `CourseStatusBadge.tsx`:
   - ลบเคส `statusCode === 3` (ไม่มีแล้ว); ให้ `statusCode 2` map tone ให้ตรงความหมาย Closed (เลือก `neutral` เทา ให้ตรงกับ string `'closed'→neutral` หรือคงสีเดิมแต่ให้สอดคล้องกันทั้ง code+string)
   - string fallback: คง `'closed'→...`, ลบ/คง `'retired'` ตามที่เหมาะ (statusName จะไม่เป็น "Retired" อีก)
6. grep `iLearn.Admin.React/src` หา `'Retired'` / `=== 3` ของ course ที่เหลือ → แก้ให้ตรง

> หมายเหตุ label: ถ้าธุรกิจอยากคงคำว่า **"Retired"** บนจอแทน "Closed" → ให้ **rename enum `Closed=2` → `Retired=2`** (แทนการ rename FE) แล้วลบ Retired(3) — แต่จะ touch backend ref ~6 จุด (`CanLearnerAccess` ฯลฯ) มากกว่า; default ของแผนนี้คือยึดชื่อ backend ปัจจุบัน ("Closed")

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยนพฤติกรรม learner-access (Closed ยังเข้าได้) / publish toggle (Open↔Closed)
- ห้ามเพิ่ม guard ใหม่ (การลบ Retired guard = ลบ dead code ไม่ใช่เพิ่ม)
- ห้ามแตะ Course Draft/Open logic / readiness guard (→Open)
- ห้ามแตะ status ของ entity อื่น

## Acceptance criteria

- [x] `CourseStatus` เหลือ 3 ค่า (ไม่มี Retired)
- [x] ไม่มี reference `CourseStatus.Retired` เหลือ (grep = 0) + build ผ่าน
- [x] FE: status 2 แสดงป้าย/ปุ่ม/สี **สอดคล้องกัน** (ไม่มี "Closed" สีแดง, ไม่มีปุ่ม "Retire" ที่ส่ง 2); `isClosed` ถูกต้อง
- [x] learner-access ของ Closed course ยังทำงานเหมือนเดิม (player-info เข้าได้)
- [x] publish/close/revert-to-draft ยังทำงาน (CourseDetail controls)
- [x] `dotnet test` ผ่าน (อัปเดต test ที่อ้าง Retired) + `npm run build`/`lint` (0/0)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```
ทดสอบ manual: `/courses/:id` — เปลี่ยน Draft↔Open↔Closed (ป้าย/สี/ปุ่มตรง), badge ใน `/courses` explorer ตรง, learner เข้า Closed course ได้

## Implementer Notes

- เลือกแนวทางตามแผน: ใช้ label สถานะที่ 3 เป็น **Closed** (ไม่ rename เป็น Retired)
- Backend:
   - ลบ `Retired = 3` ออกจาก `CourseStatus`
   - ลบ reference `CourseStatus.Retired` จาก `CourseService`, `CourseVersionService`, `CoursesCRUDController`
   - ลบ `IsRetired` property ใน `Course` entity
   - ปรับ test `LifecycleContractDtoTests` ให้ยืนยัน `Closed` + `CanLearnerAccess = true`
- Frontend:
   - `CourseDetailPage`: เปลี่ยน `isRetired` เป็น `isClosed` และเปลี่ยนปุ่มเป็น `Close Course`
   - `CourseStatusBadge`: ปรับ `statusCode === 2` เป็น tone `neutral` และลบกรณี `statusCode === 3`
   - อัปเดตคอมเมนต์ใน `CourseListPage` ให้เหลือ Draft/Closed
- ตรวจซ้ำด้วย grep: `CourseStatus.Retired` = 0 matches, และไม่มี `isRetired`/`statusCode === 3` ใน React source แล้ว
- Verification:
   - `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน (warnings เดิม)
   - `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน: Passed 118, Failed 0
   - `npm run lint` ผ่าน
   - `npm run build` ผ่าน
