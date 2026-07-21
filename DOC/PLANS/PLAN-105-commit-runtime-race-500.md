# PLAN-105: HOTFIX — CommitRuntime 500 จาก race ตอน insert แถวแรก (duplicate key)

- **Status:** DONE → REVIEWED (code+tests ผ่าน — รอ QA deploy + manual smoke ก่อน VERIFIED)
- **Assigned:** GitHub Copilot (§2 server + §1 client, took over §1 from Gemini by user request)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ความรุนแรง:** 🟠 HIGH — commit หลุดเป็นครั้งคราว (เวลาเรียน/สถานะบางส่วนหาย) + error 500 โผล่ให้ผู้เรียนเห็นใน console
- **ที่มา:** ผู้ใช้ reproduce ได้บน QA หลัง deploy 101-104 (13:43) — 500 ยังเกิด เพราะ 104 ไม่ได้แตะเรื่องนี้
- **อ่าน CLAUDE.md หัวข้อ Backend ก่อนเริ่ม**

---

## หลักฐาน

**Response ของ 500:**
```json
{"title":"An unexpected error occurred.","status":500,
 "detail":"An error occurred while saving the entity changes. See the inner exception for details.",
 "instance":"/api/LearningLogs/commit-runtime"}
```
= `DbUpdateException` จาก EF SaveChanges (ไม่ใช่ logic error)

**Stack ฝั่ง client (ชี้ชัดที่สุด):**
```
LMSCommit → flushSelectedContentItemRuntime → commitRuntimeContentItems → $.ajax  ⇒ 500
  ← commit @ lms.js:43 ← commit @ lms.js:45 ← Q.commit @ lms.js:35
  ← O.initialize @ lms.js:31   ← startupApplication @ index.html:60
```
⇒ เกิดตอน **content initialize** และ lms.js เรียก `commit` **ซ้อนกันหลายชั้น** = ยิง commit รัว ๆ ในเสี้ยววินาที

**DB (learner 430339 / enrollment 18217, course 540):** item 366 มี state + log บันทึกสำเร็จ (request ที่ชนะ) — **ไม่มี duplicate ค้างในตาราง** เพราะตัวที่แพ้ถูก DB ปฏิเสธไปแล้ว (นั่นคือ 500)

## Root cause

1. `commitRuntimeContentItems` ยิง `$.ajax` โดย**ไม่รอ request ก่อนหน้าจบ** ⇒ 2 requests วิ่งพร้อมกัน
2. ทั้งคู่เข้า `ScormRuntimeStateService.UpsertAsync` → query `existingStates` → **ยังไม่มี row** (commit แรกของ item นั้น) → ต่างคนต่างสร้าง entity ใหม่ → `AddWithoutSaveAsync` → `SaveChangesAsync`
3. ตัวที่สอง INSERT ชน unique index `IX_ScormRuntimeStates_EnrollmentId_ContentItemId` (unique, `WHERE [IsDeleted]=0`) ⇒ **duplicate key → `DbUpdateException` → 500**

**เกิดเฉพาะ commit แรกของแต่ละ item** — พอมี row แล้วทั้งสอง request ไปทาง UPDATE ซึ่งไม่ชน จึงเป็น intermittent

**ไม่ใช่ regression ของ 096-104** — `UpsertAsync` ไม่เคย concurrency-safe มาแต่เดิม แต่ PLAN-097 เพิ่มจุด flush + content ตัวนี้ commit ถี่ตอน init ⇒ โอกาสชนสูงขึ้นมาก

## Scope

### §1 (client, `Player.cshtml`) — ห้ามยิง commit ซ้อนกัน

`commitRuntimeContentItems` ปัจจุบันยิงทันทีทุกครั้งที่ถูกเรียก ⇒ ทำเป็น **serialize + coalesce**:

- เก็บ promise ของ request ที่ค้างอยู่ (เช่น `let inFlightCommit = null`)
- ถ้ามี request ค้าง: **อย่ายิงซ้อน** — ต่อคิวให้ทำงานหลังตัวปัจจุบันจบ (chain) หรือรวบเป็นครั้งเดียว (ค่าใน `cmiModel`/`runtimeState` ถูกอ่านตอนสร้าง payload อยู่แล้ว ⇒ commit รอบถัดไปจะได้ค่าล่าสุดเสมอ)
- **ข้อยกเว้น:** เส้นทาง `useBeacon: true` (pagehide/visibilitychange/beforeunload) **ห้ามเข้าคิว** ต้องยิงทันทีเสมอ (หน้าอาจถูกปิดก่อน) — beacon ไม่ใช่ `$.ajax` จึงไม่ชนกลไกนี้อยู่แล้ว
- คง `hasPendingRuntimeCommit` และ flush ทุกจุดของ 097 ไว้ครบ — งานนี้แค่กันซ้อน ไม่ตัด flush

### §2 (server — สำคัญกว่า) — `UpsertAsync` ต้องทน race

แม้ §1 จะกันฝั่ง client ได้ แต่ยังชนได้จาก 2 แท็บ/2 อุปกรณ์/beacon+ajax ⇒ server ต้องไม่ 500

**(ก) เพิ่ม Detach ใน `IUnitOfWork` + impl** (ปัจจุบันมีแค่ SaveChanges/BeginTransaction/AddRange — ไม่มีทางถอน entity ที่ Added ค้างใน tracker):
```csharp
/// <summary>ถอน entity ออกจาก change tracker (ใช้ตอน retry หลัง insert ชน unique key)</summary>
void Detach<T>(T entity) where T : BaseEntity;   // _context.Entry(entity).State = EntityState.Detached;
```

**(ข) `ScormRuntimeStateService.UpsertAsync` — retry once เป็น update:**
1. ทำงานตามเดิม แต่**จำ entity ที่เพิ่ง Add** ไว้ในลิสต์
2. ครอบ `SaveChangesAsync` ด้วย `try/catch (DbUpdateException ex) when (IsUniqueViolation(ex))`
3. ใน catch: `Detach` ทุก entity ที่เพิ่ง Add → **query `existingStates` ใหม่** (ตอนนี้ row ของอีก request มีแล้ว) → `ApplyCommit` ทับเป็น update (`UpdateWithoutSave`) → `SaveChangesAsync` อีกครั้ง → สร้าง `touchedStates` จาก entity ที่ reload มา
4. **retry ได้ครั้งเดียว** ถ้ายังพังให้ throw ต่อ
5. `IsUniqueViolation`: ตรวจ inner exception เป็น `SqlException` `Number` 2601/2627 — **ถ้าไม่ใช่ unique violation ห้ามกลืน ต้อง rethrow** (อย่าปิดบัง error จริง เช่น truncation)

**หมายเหตุ:** `ApplyCommit` มีกติกา sticky merge ของ 104 อยู่แล้ว ⇒ การ merge ทับ row ที่อีก request สร้างไว้จะไม่ทำให้ผลที่สำเร็จหาย

### §3 — สังเกตการณ์ (ไม่ต้องแก้ในแผนนี้)

`LearningLogs` **ไม่มี** unique index บน (EnrollmentId, ContentItemId) ⇒ race เดียวกันสร้าง log ซ้ำได้เงียบ ๆ (ตรวจ QA แล้ว**ยังไม่พบ**). **ห้ามเพิ่ม unique index** — โมเดลตั้งใจให้มีหลาย log ต่อ item ข้าม reset boundary (กรองด้วย `CreatedAt >= ResetAt`). ถ้าอนาคตพบ log ซ้ำ ให้แก้ที่การ serialize ไม่ใช่ที่ schema

## Contract ที่เปลี่ยน

- **ใหม่:** `IUnitOfWork.Detach<T>` (additive)
- API shape / DB schema / migration: **ไม่มี**
- พฤติกรรม: commit ที่เคย 500 จะสำเร็จเป็น update แทน

## นอก Scope (ห้ามทำ)

- ห้ามเพิ่ม/แก้ index หรือ migration
- ห้ามแตะ merge policy ของ 104 / reset paths ของ 099/101
- ห้ามตัดจุด flush ของ 097 (แก้แค่ "ไม่ยิงซ้อน")
- ห้ามใช้ global lock/semaphore ค้างข้าม request (แก้ที่ต้นเหตุ ไม่ใช่ serialize ทั้งระบบ)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Tests ที่ต้องเพิ่ม:
1. **§2:** จำลอง `SaveChangesAsync` โยน `DbUpdateException` (unique violation) ครั้งแรก แล้วสำเร็จครั้งที่สอง → `UpsertAsync` **ไม่ throw** และคืน state ที่ merge แล้วถูกต้อง
2. **§2:** `DbUpdateException` ที่**ไม่ใช่** unique violation → **rethrow** (ไม่กลืน)
3. **§2:** retry แล้วยังพัง → throw (ไม่วนไม่รู้จบ)
4. **§1:** (ถ้าเทสฝั่ง client ไม่ได้ ให้ manual ตามข้อ 2 ด้านล่าง)

Manual (QA — deploy API + learner):
1. **เปิด Player คอร์สที่ยังไม่เคยเล่นด้วย learner ที่ยังไม่มี runtime state** (เคสที่พังคือ commit แรก) → เล่นให้ content initialize → **console ต้องไม่มี 500 / `Runtime commit failed`**
2. เปิด `?debug` แล้วดู `[DIAG] ajax→/ajax←` — ต้องไม่เห็น CommitRuntime 2 ตัวซ้อนเวลาเดียวกัน (§1 ทำงาน)
3. ทำซ้ำหลาย ๆ item/หลายคอร์ส ให้มั่นใจว่า commit แรกไม่พังแล้ว
4. ตรวจ DB: state/log ของ item ที่เพิ่งเล่นถูกสร้างครบ ไม่มีซ้ำ

## Deploy note

§2 = **API**; §1 = **learner**. ไม่มี migration. PROD รอผู้ใช้ยืนยัน QA

## Implementer Notes

### GitHub Copilot (§1 client, took over from Gemini) — 2026-07-21 14:11

- ปรับ `Player.cshtml` ให้ non-beacon runtime commits serialize ผ่าน `runtimeCommitQueue` จึงไม่ยิง `$.ajax` ซ้อนกันในเสี้ยววินาที
- เส้นทาง `useBeacon: true` bypass queue และยังยิงทันทีตามเดิมสำหรับ `beforeunload`/`pagehide`/`visibilitychange hidden`
- แยก prepare/send: snapshot payload ทันทีตอน flush ถูกเรียก แล้ว queue เฉพาะการส่ง ajax เพื่อไม่ให้ `cmiModel` ของ content item เดิมหายตอนผู้ใช้สลับบทก่อน request ได้คิวส่ง
- เพิ่ม `runtimeCommitVersion` ต่อ content item เพื่อกัน request เก่าที่เพิ่งสำเร็จ clear `hasPendingRuntimeCommit` ทิ้ง ถ้ามี dirty ใหม่เกิดระหว่าง request ค้าง
- ไม่แตะ PLAN-106 auto-summary, SCORM finish behavior, flush lifecycle จุดเดิม, API shape, DB schema หรือ migration
- Verified: `dotnet build iLearn.User/iLearn.User.csproj -o artifacts\verify-user` ผ่าน; full `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน 217/217; ลบ temp artifacts แล้ว
- Manual QA ยังต้องทำตาม checklist ด้านบน: เปิด Player ด้วย `?debug` และยืนยันว่าไม่เห็น CommitRuntime ajax ซ้อนกันเวลา content initialize

### GitHub Copilot (§2 server) — 2026-07-21 14:05

- เพิ่ม `IUnitOfWork.Detach<T>` แบบ additive และ implement ใน EF `UnitOfWork` ด้วย `EntityState.Detached`; อัปเดต fake unit-of-work ที่ compile จาก contract นี้
- ปรับ `ScormRuntimeStateService.UpsertAsync` ให้ stage add/update แล้ว catch เฉพาะ `DbUpdateException` ที่ inner `SqlException.Number` เป็น 2601/2627 และมี entity ที่เพิ่ง add; retry หนึ่งครั้งโดย detach added entities, reload state, apply commit ใหม่, save อีกครั้ง
- ไม่แตะ merge policy ของ PLAN-104, reset paths, index/migration, API shape หรือ client `Player.cshtml`
- เพิ่ม regression tests 3 ตัวตาม §2: unique violation ครั้งแรกแล้ว retry สำเร็จ, non-unique `DbUpdateException` rethrow, retry แล้วยังพัง rethrow หลัง save ครั้งที่สอง
- Verified: focused `ScormRuntimeStateServiceTests` ผ่าน 17/17; full `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน 217/217; ลบ `artifacts\verify-test` และ focused artifacts แล้ว

## Reviewer Sign-off (Claude Code, 2026-07-21)

- **§2 server (สำคัญสุด) ตรงสเปคเป๊ะ:** `IUnitOfWork.Detach<T>` additive + impl `EntityState.Detached` ✅ `UpsertAsync` แยก staging เป็น local function `StageChangesAsync()` แล้วครอบ `SaveChangesAsync` ด้วย `catch (DbUpdateException ex) when (addedStates.Count > 0 && IsUniqueViolation(ex))` → detach ตัวที่ Add ค้าง → **re-stage (query ใหม่เจอ row ของ request ที่ชนะ → ไปทาง UPDATE)** → save อีกครั้ง ✅ **retry ครั้งเดียวจริง** (ไม่มีลูป — save รอบสองพังคือ throw) ✅
- **ไม่กลืน error อื่น:** `IsUniqueViolation` เดินไล่ inner chain หา `SqlException` 2601/2627 เท่านั้น; ถ้าไม่ใช่ `when` filter ไม่ติด ⇒ exception เดิม propagate ตามปกติ ✅ (ข้อที่กำชับหนักสุด)
- **re-query ได้ entity ที่ tracked:** `GenericRepository.GetAsync` ไม่ใช้ `AsNoTracking` ⇒ `UpdateWithoutSave` หลัง detach ทำงานถูกต้อง ✅
- **§1 client — ทำได้ดีกว่าที่แผนกำหนด:**
  - แยก **prepare (snapshot payload ทันที)** ออกจาก **send (เข้าคิว)** ⇒ `captureSessionTime`/`cmiModel` ถูกจับ ณ เวลาที่ flush ถูกเรียกจริง ไม่ใช่ตอนคิวได้ส่ง และผู้ใช้สลับบทระหว่างรอคิวก็ไม่ทำข้อมูลเพี้ยน ✅
  - `useBeacon: true` **bypass คิว** ยิงทันที ตามที่กำชับ (หน้าอาจถูกปิดก่อน) ✅
  - `runtimeCommitQueue = queuedCommit.catch(() => null)` ⇒ commit ที่ fail **ไม่ทำให้คิวตัน** แต่ caller ยังได้ promise ที่ reject จริง (LMSFinish จับ error ได้) ✅
  - **`runtimeCommitVersion`** (เพิ่มเอง เกินสเปค): clear `hasPendingRuntimeCommit` เฉพาะเมื่อ version ยังตรงกับตอน prepare ⇒ การแก้ที่เกิดระหว่าง request ค้าง**ไม่ถูกกลืนหาย** — เป็นการอุดช่องที่แผนไม่ได้ระบุ ✅
- **Tests:** 3 เคสตามแผนครบ — `..._WhenFirstInsertHitsUniqueViolation_ReloadsAndUpdatesWinningState`, `..._WhenSaveFailsWithNonUniqueUpdateException_RethrowsWithoutRetry`, `..._WhenRetryStillFails_RethrowsAfterSingleRetry` ✅
- **Verify อิสระ:** `dotnet test` **217/217**; build API+learner 0 errors; `node --check` Player 1,547 บรรทัดผ่าน
- **Observation (TRIVIAL):** ถ้า browser ไม่มี `navigator.sendBeacon` เส้นทาง `useBeacon` จะตกไปใช้ `$.ajax` **นอกคิว** — เคสนี้ไม่มีในเบราว์เซอร์ที่รองรับจริง ไม่ต้องแก้
- **คงค้าง:** manual QA ตาม checklist (เปิดคอร์สที่ยังไม่มี runtime state → commit แรกต้องไม่ 500; `?debug` ต้องไม่เห็น CommitRuntime ซ้อนกัน)

**สรุป: ผ่านรีวิว ไม่มี finding**
