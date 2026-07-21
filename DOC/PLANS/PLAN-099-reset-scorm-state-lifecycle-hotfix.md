# PLAN-099: HOTFIX — Reset Progress ไม่ล้าง ScormRuntimeState → สถานะ/คะแนนเก่าฟื้นคืนชีพ (+ เวลาเรียนโป่งจาก flush ถี่)

- **Status:** DONE → REVIEWED (code + tests ผ่านสะอาด — รอ deploy QA API + Reset ซ้ำ + iPad smoke ก่อน VERIFIED)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-17
- **ความรุนแรง:** 🔴 CRITICAL — สถานะผ่าน/ไม่ผ่านของ learner ผิด (Exam ไม่ได้ทำแต่ผ่าน / Learn ดูจบแต่ไม่ผ่าน) บน QA
- **อ่าน CLAUDE.md หัวข้อ Backend ก่อนเริ่ม** — โดยเฉพาะ IDateTime, unique index บนตาราง soft-delete, side-effect ห้ามล้ม request หลัก

> **ไม่ใช่ regression จากโค้ด iPad (096/097/098)** — logic ฝั่ง server ไม่ถูกแตะเลย (git ยืนยัน; แก้ล่าสุด = PLAN-079). เป็นบั๊กแฝงของ Reset Progress ที่ถูกกระตุ้นด้วย pattern "reset → เปิดไล่ดูทุก item เร็ว ๆ" ระหว่าง smoke build ใหม่ + flush ที่ถี่ขึ้นจาก PLAN-097

---

## หลักฐาน (query QA DB จริง — enrollment 18201, courseId 968)

Timeline:

| เวลา | เหตุการณ์ | ผลใน DB |
| --- | --- | --- |
| 13 ก.ค. | เรียนครบ ผ่านทั้ง 4 items | LearningLogs เก่า passed หมด, RawScore Exam 80/75; `ScormRuntimeState` 4 rows: SuccessStatus=`passed`, RawScore=100 (Learn) |
| 21 ก.ค. 08:26:03 | กด **Reset Progress** | `Enrollment.ResetAt` ตั้งค่า → LearningLogs เก่าถูกกรองด้วย `CreatedAt >= ResetAt`; **ScormRuntimeState 4 rows ไม่ถูกแตะ** — แค่ถูกซ่อนโดย `GetActiveStatesAsync` (filter `UpdatedAt >= resetAt`) |
| 08:26:45–08:28:22 | เปิดแต่ละ item ดู 5–10 วิ | commit ใหม่เข้า `UpsertAsync` ซึ่ง query existing states **โดยไม่กรอง resetAt** → เจอ row เก่า → merge sticky → **`UpdatedAt` ขยับเป็นวันนี้** → row เก่าหลุด filter กลับมา live |

Snapshot ปัจจุบัน (หลัง commit สั้น ๆ):

```
ContentItem              Type  Ver   Lesson      Comp        Success   RawScore
NTC-...050_12_Learn      1     1.2   incomplete  incomplete  unknown   100.00   ← Learn: บาร์เต็มแต่ไม่มีเครื่องหมายถูก
NTC-...711_2004_Learn    1     2004  incomplete  incomplete  unknown   100.00   ←
NTC-...035_12_Exam       2     1.2   incomplete  incomplete  passed    .00      ← Exam: ติ๊กเขียวทั้งที่เปิด 7 วิ
NTC-...334_2004_Exam     2     2004  incomplete  incomplete  passed    .00      ←
```

**ตรงกับอาการที่ผู้ใช้รายงานเป๊ะ** ผ่านกติกา merge ใน `ScormRuntimeStateService`:
- **Exam ผ่านทั้งที่ยังไม่เสร็จ:** `PreferSuccessStatus` = "`unknown` ห้ามทับ `passed`" → SuccessStatus=`passed` ของ 13 ก.ค. รอด → `ScormContentStatusPolicy.ResolveStatus` คืน `passed`
- **Learn ดูจบแต่ไม่ผ่าน:** commit รอบนี้ทับ lesson/completion เป็น `incomplete` ได้ แต่ `PreferRawScore` = "0 ห้ามทับค่า >0 ตอน placeholder" → RawScore=100 รอด → `ResolvePlayerContentItemActivityProgress` (Learn ใช้ RawScore) คืนบาร์ 100%

## บริบทโค้ด (ยืนยันแล้ว)

- `LearningLogsController.ResetProgress` ([iLearn.API](../../iLearn.API/Controllers/LearningLogsController.cs)) รีเซ็ต `Enrollment` + `EnrollmentAssignment` links + ตั้ง `ResetAt` — **ไม่แตะ `ScormRuntimeState` เลย**
- `ScormRuntimeStateService.UpsertAsync` ([iLearn.Infrastructure](../../iLearn.Infrastructure/Services/ScormRuntimeStateService.cs)) query `existingStates` ด้วย `EnrollmentId` เท่านั้น — merge เข้า row เดิมเสมอ
- **soft-delete ใช้ได้แน่นอน:** `AppDbContext` (บรรทัด 319-332) วน loop ใส่ `HasQueryFilter(e => !e.IsDeleted)` ให้ทุก `BaseEntity` (ScormRuntimeState รวมด้วย) ⇒ row ที่ soft-delete จะซ่อนจากทั้ง `UpsertAsync` และ `GetActiveStatesAsync` อัตโนมัติ; filtered unique index `IX ...(EnrollmentId, ContentItemId) [IsDeleted]=0` (บรรทัด 133-136) รองรับสร้าง row ใหม่ค่าซ้ำ — **precedent PLAN-092**
- `GenericRepository.DeleteWithoutSave(entity)` set `IsDeleted=true` + `DeletedAt` (soft-delete)

## Scope

### §1 (CRITICAL) — Reset ต้อง soft-delete ScormRuntimeState ของ enrollment

**(ก) เพิ่ม method ใน `IScormRuntimeStateService`** ([iLearn.Application/Interfaces/Services](../../iLearn.Application/Interfaces/Services)):

```csharp
/// <summary>Soft-delete runtime states ทั้งหมดของ enrollment (ใช้ตอน Reset Progress) — คืนจำนวนที่ล้าง</summary>
Task<int> ClearForEnrollmentAsync(int enrollmentId, CancellationToken cancellationToken = default);
```

**(ข) impl ใน `ScormRuntimeStateService`:**
- query states ของ enrollment (global filter ตัด row ที่ลบแล้วออกให้เอง) → `DeleteWithoutSave` ทีละตัว → `_unitOfWork.SaveChangesAsync` (SaveChanges จะ stamp `DeletedAt/DeletedBy` ตาม audit เดิม) → คืน count
- ถ้าไม่มี state = คืน 0 ไม่ต้อง save

**(ค) เรียกใน `ResetProgress`** (ต่อจากรีเซ็ต enrollment/links, ก่อน `InvalidateLearningCaches`):
- `await _scormRuntimeStateService.ClearForEnrollmentAsync(enrollment.Id);`
- **ต้องอยู่ใน flow หลัก** (ไม่ใช่ side-effect กลืน error) — ถ้าล้างไม่ได้ reset ถือว่าไม่สมบูรณ์ ควรให้ error เด้ง ไม่ใช่เงียบ

ผลลัพธ์: หลัง reset row เก่า `IsDeleted=1` → commit ครั้งถัดไป `UpsertAsync` มองไม่เห็น → สร้าง row ใหม่สะอาด (unique index ยอมเพราะ filter `[IsDeleted]=0`)

### §2 (HIGH) — เวลาเรียนโป่งจาก flush ถี่ (regression ที่ PLAN-097 ขยาย)

**หลักฐาน:** DB แสดง Learn 1701 เล่นจริง ~6 วิ แต่ `TotalSecondsPlayed=20`; Exam 1704 เล่น ~5 วิ แต่ =15 — บวกซ้ำ 2-3 เท่า

**สาเหตุ:** client ส่ง `session_time` แบบ**สะสมตั้งแต่ startCourse** (monotonic ด้วย max()) แต่ `UpsertLearningLogsAsync` ([LearningLogsController](../../iLearn.API/Controllers/LearningLogsController.cs) ~บรรทัด 340) ทำ `log.TotalSecondsPlayed += sessionSeconds` **ทุก commit ที่มี includeSessionTime**. เดิม (ก่อน 097) includeSessionTime ยิงเฉพาะจุดจบ session (switch/finish/view-result) มักครั้งเดียว → บวกครั้งเดียวพอรับได้; PLAN-097 เพิ่ม `pagehide` + `visibilitychange` (includeSessionTime:true) → บน iPad สลับแอปแต่ละครั้ง = flush = บวกยอดสะสมซ้ำ

**แก้ (contained ใน `UpsertLearningLogsAsync` — บวกเฉพาะ delta):**

```csharp
if (log != null)
{
    int prevSessionSeconds = ParseSessionTime(log.SessionTime);
    // client ส่ง session_time สะสมภายใน session; ข้าม session ใหม่ counter จะรีเซ็ตต่ำกว่าเดิม
    int delta = sessionSeconds >= prevSessionSeconds
        ? sessionSeconds - prevSessionSeconds   // session เดิม โตขึ้น → บวกส่วนต่าง
        : sessionSeconds;                        // session/attempt ใหม่ → บวกยอดใหม่ทั้งก้อน
    log.TotalSecondsPlayed += delta;

    if (!string.IsNullOrEmpty(update.SessionTime))
    {
        log.SessionTime = update.SessionTime;   // เก็บ cumulative ล่าสุดไว้เทียบรอบหน้า (เดิมก็ทำอยู่)
    }
    // ...ที่เหลือคงเดิม (Status/Progress/Score/AttemptCount)...
}
```

- commit ที่ไม่มี session time (interim, includeSessionTime:false) → `sessionSeconds=0 < prev` → delta=0, `log.SessionTime` ไม่ถูกแก้ (guard เดิม) → ปลอดภัย ไม่บวก
- log ใหม่ (contentItem แรก) คงเดิม `TotalSecondsPlayed = sessionSeconds` — ถูกอยู่แล้ว
- **ข้อจำกัดที่รับได้:** ถ้า attempt ใหม่ยาวกว่า attempt เก่าก่อน flush แรก จะ under-count เล็กน้อย — ยังดีกว่าการ inflate แบบทวีคูณตอนนี้มาก (จดใน Implementer Notes)

## Contract ที่เปลี่ยน

- **ใหม่:** `IScormRuntimeStateService.ClearForEnrollmentAsync` (additive) — consumer เดียวคือ `ResetProgress`
- `TotalSecondsPlayed` accounting เปลี่ยนจาก "บวกยอดสะสมทุก commit" → "บวก delta" (แก้บั๊ก ไม่ใช่ contract API)
- DB schema / migration: **ไม่มี** (soft-delete ใช้คอลัมน์ + index เดิม)
- API shape / response: **ไม่เปลี่ยน**

## นอก Scope (ห้ามทำ)

- ห้ามแตะกติกา merge ใน `ScormRuntimeStateService.ApplyCommit`/`Prefer*` (sticky merge ถูกต้องสำหรับ session เดียว — ปัญหาคือ row เก่าไม่ถูกล้างตอน reset ไม่ใช่ตัว merge)
- ห้ามแตะ `ScormContentStatusPolicy` / `player-info` resolve logic (ตีความสถานะถูกอยู่แล้วเมื่อ input สะอาด)
- ห้ามแตะ client `Player.cshtml` (096/097/098) — flush lifecycle ของ 097 **ถูกต้องตามเจตนา** (ต้อง save time บน iPad); ที่ต้องแก้คือฝั่ง server รับ cumulative แล้วบวก delta
- ห้ามทำ hard-delete (ใช้ soft-delete ตาม convention + filtered index)
- ห้าม migration

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Tests ที่ต้องเพิ่ม (`iLearn.Tests` — ต่อ `ScormRuntimeStateServiceTests` / `LearningLogs` ที่มี):
1. **§1 core:** enrollment มี states → `ClearForEnrollmentAsync` → states ทั้งหมด `IsDeleted=1`, `GetActiveStatesAsync` คืนว่าง
2. **§1 lifecycle:** clear แล้ว `UpsertAsync` contentItem เดิม → สร้าง row **ใหม่** (Id ต่าง / ค่าเริ่มสะอาด) ไม่ merge ค่าเก่า (จำลอง regression: SuccessStatus เก่า passed ต้องไม่รอด)
3. **§2 no-inflation:** commit includeSessionTime 3 ครั้งใน session เดียว (6s→8s→10s) → `TotalSecondsPlayed=10` ไม่ใช่ 24
4. **§2 cross-session:** session1 จบ 10s, session2 (counter รีเซ็ต) 0→5s → total=15

Manual (QA — ต้อง deploy API):
1. คอร์ส 968: กด Reset Progress → เปิด player: ทุก item สถานะ**ว่าง/ไม่ผ่าน**, ไม่มีติ๊กเขียวค้าง, บาร์ Learn ไม่เต็ม
2. ทำ Exam จริงให้ผ่าน → ผ่านเฉพาะที่ทำ; Learn ดูจบ → progress ขยับตามจริง
3. เล่น item ~1 นาที สลับแท็บ 2-3 ครั้ง → `TotalSecondsPlayed` ใกล้เวลาเล่นจริง ไม่โป่ง

**Remediation ข้อมูลที่เพี้ยนตอนนี้:** หลัง deploy API fix → **กด Reset Progress ซ้ำ 1 ครั้ง** บน enrollment ที่เพี้ยน → รอบนี้ soft-delete จริง สะอาดทันที **ไม่ต้องแก้ DB มือ** (ผู้ใช้ทำเองได้ / หรือระบุ enrollment ให้)

## Deploy note

- **ต่างจาก 096/097/098:** งานนี้แตะ **iLearn.API + iLearn.Infrastructure + iLearn.Application** ⇒ ต้อง deploy **API** ด้วย (ไม่ใช่แค่ learner) — ไม่มี migration จึงไม่ต้องรัน `database update`
- PROD รอผู้ใช้ยืนยันผล QA ในแชท (gate เดิม)

## Implementer Notes

- เพิ่ม `IScormRuntimeStateService.ClearForEnrollmentAsync` และ implementation ที่ soft-delete states active ของ enrollment ทั้งหมดใน transaction/save boundary เดียว; `ResetProgress` เรียกใน main flow ก่อน invalidate cache จึงให้ reset ล้มทันทีหาก clear ไม่สำเร็จ
- เปลี่ยน `TotalSecondsPlayed` ให้บวก delta ระหว่าง `session_time` ล่าสุดกับค่าที่บันทึกไว้; เมื่อ counter ลดลงจะถือเป็น session ใหม่และบวกค่ารอบใหม่เต็มจำนวน
- เพิ่ม regression tests: clear + active state empty, clear แล้ว commit สร้าง state ใหม่ไม่พก passed/score เก่า, reset เรียก clear, cumulative flush 6→8→10 = 10 วินาที, และ session reset 10→5 = 15 วินาที
- Verification: `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน (0 errors, 90 warnings เดิม); `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน 207/207; ล้าง `artifacts\verify-test` แล้ว
- ข้อจำกัดที่ยอมรับตามแผน: หาก attempt ใหม่ส่ง cumulative counter ที่มากกว่า attempt ก่อนหน้าในการ flush แรก จะ under-count ได้เล็กน้อย แต่จะไม่ inflate ซ้ำจากทุก flush

## Reviewer Sign-off (Claude Code, 2026-07-21)

ตรวจ diff เต็ม + verify อิสระ (`dotnet test` **207/207** — 4 tests ใหม่ครบทุกเคสในแผน §5):

- **§1 ClearForEnrollmentAsync:** query states → `DeleteWithoutSave` (soft-delete) → `SaveChangesAsync` → คืน count; ไม่มี state = คืน 0 ไม่ save ✅ เรียกใน `ResetProgress` main flow ก่อน `InvalidateLearningCaches` **ไม่ห่อ try/catch** (ล้างไม่ได้ = reset ล้ม ตามสเปค) ✅
- **§2 delta:** `previousSessionSeconds = ParseSessionTime(log.SessionTime)`; delta = (new >= prev) ? new-prev : new; interim commit ไม่มี session time → sessionSeconds=0 < prev → delta 0 + `log.SessionTime` ไม่ถูกแก้ (guard เดิม) ปลอดภัย ✅ log ใหม่คงเดิม (init = sessionSeconds) ✅
- **Tests แข็งแรงจริง (ไม่กลวง):**
  - `ClearForEnrollmentAsync_SoftDeletes...`: IsDeleted=true, enrollment อื่นไม่โดน, GetActiveStates ว่าง, SaveChanges 1 ครั้ง
  - `ClearForEnrollmentAsync_AllowsNextCommitToCreateCleanRuntimeState`: **จำลอง regression ตรง ๆ** — old (SuccessStatus=passed, RawScore=100) soft-delete แล้ว UpsertAsync สร้าง row ใหม่ incomplete/unknown/0, `repo.Items.Count==2` (เก่ายังอยู่ ลบแบบ soft) → พิสูจน์ค่าเก่าไม่ revive
  - `CommitRuntime_OnlyAddsNewSessionTimeSincePreviousCommit`: 6→8→10 = **10** (ไม่ใช่ 24)
  - `CommitRuntime_AddsFullSessionTimeWhenNewSessionCounterResets`: 10→5 = **15**
  - `ResetProgress_...` เดิม + assert `ClearedEnrollmentId == enrollment.Id`
- **ไม่ล้ำ scope:** ไม่แตะ merge policy/ScormContentStatusPolicy/player-info/client; ไม่มี migration ✅
- **คงค้างก่อน VERIFIED:** deploy **API** (099 แตะ backend — ต่างจาก 096-098) → กด Reset ซ้ำ 1 ครั้งบน enrollment 18201 (courseId 968) เพื่อ remediate ข้อมูลที่เพี้ยน → ยืนยันบน player: ไม่มีติ๊กเขียวค้าง, บาร์ Learn ไม่เต็ม, ทำ Exam จริงถึงผ่าน, เวลาเรียนไม่โป่ง

**สรุป: ผ่านรีวิว ไม่มี finding — hotfix พร้อม deploy**
