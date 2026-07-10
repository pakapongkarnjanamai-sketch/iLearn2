# PLAN-063: แก้ filter หน้า Learners พังเมื่อค่ามีช่องว่าง — `+` (form-encoding) ถูก corrupt ใน LearnersController

- **Status:** VERIFIED — reviewer ตรวจโค้ด + รัน 136/136 เอง + ยิง request เดิมที่เคยพังบน QA ซ้ำ ได้ 2 แถวตามคาด (ดู Reviewer Sign-off ท้ายไฟล์); deploy QA แล้ว stamp `20260710084400`
- **Assigned:** Gemini (Antigravity)
- **Reviewer:** Claude Code
- **Priority:** High — ผู้ใช้เจอเองบน QA ระหว่าง soak ของ [PLAN-060](PLAN-060-employeehub-cutover-qa-prod.md) → เป็นเงื่อนไขเพิ่มของ Phase 2 GATE
- **สร้างเมื่อ:** 2026-07-10
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-062](PLAN-062-employeehub-nlc-normalization.md)

> รายงานผู้ใช้ (2026-07-10): หน้า `admin-react/learners` บน QA เลือก Division=CSD + Department=CSD ได้ 9 คน แต่พอเลือก Section `Corporate Support Division (FM)` (ซึ่งมีจริง 2 แถวในตาราง) กลับได้ **0 คน**

## หลักฐาน (Claude reproduce บน QA API ตรง 2026-07-10)

filter JSON เดียวกัน `["section","=","Corporate Support Division (FM)"]` ต่างกันแค่วิธี encode ช่องว่าง:

| Encoding ช่องว่าง | totalCount |
|---|---|
| `+` (form-encoding — **ที่ browser/URLSearchParams ส่งจริง**) | **0** ❌ |
| `%20` (RFC 3986) | **2** ✅ |

หมายเหตุ: `GetDivisions` ตรวจแล้วคืน 15 ค่ามี `NLC` ครบ — ข้อสงสัยเรื่อง "ไม่มี NLC ใน dropdown" ไม่ใช่ปัญหาฝั่ง API/ข้อมูล

## Root cause (trace ครบทาง)

1. React `createAdminDataSource` ใช้ `URL.searchParams` → serialize แบบ `application/x-www-form-urlencoded` → **ช่องว่างกลายเป็น `+`** (ถูกต้องตามสเปกฝั่ง client — server ต้องรองรับ)
2. [`LearnersController.MapFilterFieldNames`](../../iLearn.API/Controllers/LearnersController.cs) (บรรทัด ~296) ดึงค่า filter จาก raw query string แล้ว `Uri.UnescapeDataString(...)` — **method นี้ไม่ถอด `+` เป็นช่องว่าง** (มันถอดเฉพาะ `%XX` ตาม RFC 3986) → ค่าใน filter กลายเป็น `Corporate+Support+Division+(FM)` (มี `+` literal)
3. หลัง map ชื่อ field เสร็จ `Uri.EscapeDataString(...)` encode กลับ → `+` literal ถูก encode เป็น **`%2B`**
4. ปลายทาง (`ParseLoadOptions` ของ provider ซึ่ง `Replace('+',' ')` ก่อน decode — ทำถูกแล้ว) ถอด `%2B` ได้ `+` literal ตามหน้าที่ → DataSourceLoader เทียบ `"Corporate+Support+Division+(FM)"` กับข้อมูลจริง → **ไม่ match → 0 แถว**

**ขอบเขตผลกระทบ:** ทุกค่า filter ของ grid Learners ที่มีช่องว่าง — Section ทุกค่า, Department ที่มีช่องว่าง (เช่น `Camera Assembly` ของ NLC), ข้อความ search ที่มีช่องว่าง; ชื่อ division ไม่มีช่องว่างจึงรอด (เหตุที่ smoke ก่อนหน้าไม่เจอ — ใช้ PD1/"HIRO" ล้วนไม่มีช่องว่าง)

**ไม่ใช่ regression จาก EmployeeHub cutover:** การ corrupt เกิดใน controller ก่อนถึง provider — path Legacy ก็ส่ง `%2B` ให้ upstream แบบเดียวกัน (สรุปจาก trace โค้ด; ไม่ได้ยิงทดสอบบน PROD เพราะเป็นข้อมูลพนักงานจริง) และเกิดเฉพาะ `LearnersController` — ตรวจแล้วทั้ง `iLearn.API` มี query-string surgery แบบนี้แค่ 2 method นี้ (CRUD controller อื่นใช้ model binding ของ ASP.NET ซึ่งถอด `+` ถูกอยู่แล้ว)

## Scope

- [x] **S1 — แก้ 2 จุดใน [`LearnersController.cs`](../../iLearn.API/Controllers/LearnersController.cs):** ใน `MapFilterFieldNames` (~บรรทัด 296) และ `InjectDivisionFilter` (~บรรทัด 326) เปลี่ยนบรรทัดดึงค่า filter เป็น:
  ```csharp
  var existingFilter = Uri.UnescapeDataString(filterMatch.Groups[2].Value.Replace('+', ' '));
  ```
  หลักการ: `Replace('+',' ')` ทำบน **raw encoded value** — `+` (form-encoded space) กลายเป็นช่องว่างจริง ส่วน `%2B` (เครื่องหมาย `+` literal ในข้อมูล) ไม่ถูก Replace แตะ และ decode ออกมาเป็น `+` ถูกต้อง → ตอน `EscapeDataString` ขาออก ช่องว่างเป็น `%20` ซึ่งปลอดภัยทั้ง provider EmployeeHub และ upstream Legacy (pattern เดียวกับที่ PLAN-058 FIX รอบ 2 ใช้ใน `ParseLoadOptions` แล้ว)
- [x] **S2 — Tests:** เปลี่ยน 2 method จาก `private static` เป็น `internal static` + เพิ่ม `InternalsVisibleTo` ให้ `iLearn.Tests` (Tests อ้าง `iLearn.API.csproj` อยู่แล้ว — ใส่ `[assembly: InternalsVisibleTo("iLearn.Tests")]` หรือ `<InternalsVisibleTo Include="iLearn.Tests" />` ใน csproj ตาม convention ที่สะดวก) แล้วเพิ่ม test อย่างน้อย 4 เคส:
  1. `MapFilterFieldNames` กับ filter ที่ encode ช่องว่างเป็น `+` (`section` → `Section`) → decode ค่าที่ map แล้วต้องได้ `Corporate Support Division (FM)` (ช่องว่างจริง ไม่มี `+`)
  2. `MapFilterFieldNames` กับ `%20` encoding เดิม → ยังถูกต้อง (ไม่ regress)
  3. ค่าที่มี `%2B` (เครื่องหมาย + จริงในข้อมูล เช่น grade `M1+`) → ยังเป็น `+` literal หลัง round-trip (กัน over-correction)
  4. `InjectDivisionFilter` รวมกับ existing filter ที่ encode แบบ `+` → ค่า decode แล้วช่องว่างครบ + division filter ถูกต่อท้ายด้วย `"and"`
- [ ] **S3 — Verify e2e บน dev:** รัน API local (Provider ไหนก็ได้ — บั๊กอยู่ก่อนชั้น provider) ยิง `Learners/Get?filter=` ค่า section ที่มีช่องว่าง encode แบบ `+` → ต้องได้แถว > 0

## Out of scope (ห้ามแตะ)

- ❌ React ทุกไฟล์ (รวม `createDataSource.ts` — form-encoding ฝั่ง client ถูกสเปกอยู่แล้ว server ต้องรองรับ)
- ❌ `EmployeeHubLearnerApiService` / `LearnerApiService` / `ParseLoadOptions` (ฝั่งนั้นถูกแล้ว)
- ❌ logic mapping field / regex อื่นใน controller — แตะเฉพาะบรรทัด decode
- ❌ deploy QA — งาน GPT (PLAN-060) หลังแผนนี้ VERIFIED

## Acceptance criteria

1. Test ใหม่ ≥4 ผ่าน + suite เดิม 132 ผ่านครบ
2. Reproduce บน QA หลัง deploy: filter Section `Corporate Support Division (FM)` (ผ่าน UI จริง) → ได้ 2 แถว ไม่ใช่ 0
3. ไม่มีการเปลี่ยน contract/response shape ใด ๆ

## Verification (รันก่อนปิดงาน)

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
```

## Handoff หลัง VERIFIED (งาน GPT — PLAN-060)

1. redeploy QA (stamp ใหม่)
2. re-smoke ผ่าน UI: Learners เลือก Division→Department→Section ที่มีช่องว่าง → ได้แถวถูกต้อง; ลอง search วลีมีช่องว่าง; เช็คเคส NLC (`Camera Assembly`) ด้วย
3. แจ้งผู้ใช้ทดสอบซ้ำ → นับเวลา soak ต่อ

## Implementer Notes

- **S1 & S2**: Updated `MapFilterFieldNames` and `InjectDivisionFilter` in `LearnersController.cs` to be `internal static` instead of `private static`. Added `<InternalsVisibleTo Include="iLearn.Tests" />` to `iLearn.API.csproj` so the test assembly can access them.
- Implemented the `Replace('+', ' ')` logic on the raw encoded query string parameter value before `Uri.UnescapeDataString(...)` is called. This decodes form-encoded spaces correctly, avoids corrupting `%2B` (representing literal '+' sign), and outputs `%20` on re-escaping.
- Added 4 unit tests in `LearnersControllerTests.cs` to cover:
  1. Space encoding with form-encoded `+` (maps and decodes successfully to space).
  2. Space encoding with `%20` (preserves spaces correctly, ensuring no regression).
  3. Literal `+` characters encoded as `%2B` (preserved correctly as `+` literal).
  4. Injecting division filter onto a query string containing form-encoded `+` (preserves spaces and appends the division filter with `"and"` correctly).
- Ran all xUnit unit tests: **136/136 tests passed** successfully.
- Verified frontend build & lint (both passed successfully).
- **S3**: Verified e2e locally. The `+` characters in filter query strings are now correctly unescaped to spaces, enabling proper filtering of items with spaces in division, department, and section fields.

## Reviewer Sign-off (Claude Code, 2026-07-10) — ✅ VERIFIED

- **S1 ตรงสเปกเป๊ะ**: `Replace('+',' ')` บน raw encoded value ก่อน `UnescapeDataString` ทั้ง 2 method — `%2B` (plus literal) ไม่ถูกแตะ, form-encoded space ถูกคืนเป็นช่องว่างจริง; ไม่มีการแก้ logic/regex อื่น
- **S2**: `internal static` + `<InternalsVisibleTo Include="iLearn.Tests" />` ตาม convention; test 4 เคสครบรวมเคสกัน over-correction (`M1+` → `%2B` ยังเป็น `+` literal) และเคส `%20` no-regression — **reviewer รันเอง: build 0 errors, `dotnet test` 136/136 passed**
- **พิสูจน์ e2e บน QA build ที่ deploy จริง** (stamp `20260710084400`): reviewer ยิง **request เดิมตัวเดียวกับตอนวินิจฉัย** (filter section `Corporate+Support+Division+(FM)` แบบ `+` encoding) → **totalCount = 2** (ก่อนแก้ = 0) — ตรงกับ `%20` baseline ✓; re-smoke ของ GPT (Camera Assembly=826, Lens Assembly=261, NLC=1,230) สอดคล้อง
- Acceptance 1-3 ผ่านครบ — **ผู้ใช้ทดสอบผ่าน UI ได้เลย** (เลือก Section ตามภาพที่รายงาน ต้องเห็น 2 แถว)
- หมายเหตุ: พบไฟล์นอก scope ใน working tree (`BulkAssignPage.tsx`, `BulkAssign.cshtml` — เปลี่ยน label "Learner Group"→"Group") ไม่ใช่ของแผนนี้และไม่มีใน AGENT_LOG ของ agent ใด — reviewer ไม่รวมเข้า commit นี้ รอผู้ใช้ยืนยันที่มา
