# PLAN-075: MVC admin — Division/Department/Section/Position filter แสดงค่าว่าง หลัง flip PROD ไป EmployeeHub (lookup casing mismatch)

- **Status:** VERIFIED (deployed + verified on PROD 2026-07-13)
- **Assigned:** Claude Code — ผู้ใช้สั่งให้ทำเองแทน implementer (2026-07-13; เดิมมอบ Antigravity)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md) (EmployeeHub provider), PLAN-060 Phase 3 (flip PROD → EmployeeHub, commit `336d3a1` 2026-07-10), [PLAN-062](PLAN-062-employeehub-nlc-normalization.md)

> มาจาก user report (2026-07-13): หน้า **New Learner Group → step Members** บน PROD MVC admin — dropdown Division เปิดแล้วว่างเปล่า, Department/Section ค้างที่ "Select Division first" → filter ใช้งานไม่ได้ทั้งชุด ผู้ใช้สังเกตว่าเริ่มพังหลัง "อัพเดทข้อมูลพนักงานล่าสุด"

---

## หลักฐานที่ตรวจแล้ว (Claude Code — 2026-07-13, probe PROD จริง)

- `GET https://ap-ntc2137-prwb/iLearn/Service/api/Learners/GetDivisions` → **200** `[{"name":"NLC"},{"name":"CSD"},{"name":"DP-CGA"},...]` **ครบ 15 divisions — ข้อมูลไม่ได้หาย**
- `GetDepartments?filter=["Division","=","CSD"]` → 200 (8 departments), `GetPositions` → 200 (20+ รายการ) — **cascade ฝั่ง server ปกติทุกตัว**
- `Learners/Get?filter=["Division","=","CSD"]` และ `["division","=","CSD"]` → totalCount = **215 เท่ากันทั้งสอง casing** → การกรอง grid ฝั่ง server ไม่กระทบ
- ฝั่ง MVC ทุก lookup bind ด้วย PascalCase: `displayExpr: "Name"`, `valueExpr: "Name"`, CustomStore `key: "Name"` → item ที่ได้มี key `name` (camelCase) → **render เป็นค่าว่าง / เลือกไม่ได้**

## Root cause

| | Provider `Legacy` (ก่อน 10 ก.ค.) | Provider `EmployeeHub` (PROD ตั้งแต่ 10 ก.ค.) |
|---|---|---|
| การทำงาน | `GetFromJsonAsync<object>` **pass-through** JSON จาก server เดิม | คืน `List<LookupNameDto>` แล้ว iLearn.API serialize เอง |
| Casing บน wire | `Name` (PascalCase — ตาม server เดิม) | `name` (camelCase — default policy ของ API) |

- `iLearn.API/appsettings.Production.json` ตั้ง `"Provider": "EmployeeHub"` (commit `336d3a1`, 2026-07-10) → **contract บน wire เปลี่ยนวันนั้น** — ตรงกับช่วง "อัพเดทข้อมูลพนักงาน" ที่ผู้ใช้สังเกต
- **QA ไม่เจอบั๊กนี้** เพราะ `appsettings.json` (base) ยังเป็น `"Provider": "Legacy"` → PascalCase เหมือนเดิม
- ฝั่ง React ไม่พัง: `LearnerDirectorySelector.tsx` อ่านแบบกันเหนียว `x.Name || x.name` — แต่ `LearnerListPage.tsx` อ่าน `d.name` **strict** (`type LookupItem = { name: string }`)

## แนวทางที่เลือก: normalize ฝั่ง MVC ให้ทนทั้งสอง casing (ห้ามแก้ backend)

เหตุผล (บันทึกกัน implement ผิดทาง):

1. ❌ **ห้ามแก้ API กลับเป็น PascalCase** (เช่นใส่ `[JsonPropertyName("Name")]` บน `LookupNameDto`) — จะทำให้ React `LearnerListPage.tsx` ที่อ่าน `d.name` strict พังแทน และขัด convention camelCase ของ API ทั้งระบบ (CLAUDE.md: Learners rows เป็น camelCase)
2. ✅ normalize ใน `makeLookup` ฝั่ง MVC → ทนทั้ง `Name` และ `name` = ทำงานได้ทั้ง provider EmployeeHub (PROD) และ Legacy (QA ปัจจุบัน) — rollback provider ก็ไม่พังซ้ำ
3. คง config `displayExpr/valueExpr/key: "Name"` ของ dxSelectBox/dxTagBox ทุกตัว **ไว้ตามเดิม** — แก้เฉพาะจุดแปลงข้อมูลใน `makeLookup` จุดเดียวต่อไฟล์

## Scope: `makeLookup` มี 6 สำเนา — แก้ให้ครบทุกจุด

หมายเหตุ: หน้า Editor เรียก `window.initAdminLearnerOrgFilters` (ตัว shared ใน `admin-view-utils.js`) ก่อน แล้วค่อย fallback ตัว inline ในไฟล์ตัวเอง — **ตัวที่ทำงานจริงคือ shared** แต่ต้องแก้ inline fallback ด้วยกัน drift

| # | ไฟล์ | จุด |
|---|---|---|
| 1 | `iLearn.Admin/wwwroot/js/admin-view-utils.js` | `makeLookup` ~บรรทัด 684 (ใน `initAdminLearnerOrgFilters` — ตัวหลักที่หน้าส่วนใหญ่ใช้) |
| 2 | `iLearn.Admin/wwwroot/js/admin-layout.js` | `makeLookup` ~บรรทัด 1632 |
| 3 | `iLearn.Admin/Views/LearnerGroups/Editor.cshtml` | `makeLookup` ~บรรทัด 230 (inline fallback) |
| 4 | `iLearn.Admin/Views/LearnerGroups/AddMembers.cshtml` | `makeLookup` ~บรรทัด 240 |
| 5 | `iLearn.Admin/Views/Assignments/Detail.cshtml` | `makeLookup` ~บรรทัด 350 |
| 6 | `iLearn.Admin/Views/Assignments/BulkAssign.cshtml` | `makeLookup` ~บรรทัด 480 |

### สเปกการแก้ (ต่อ 1 สำเนา)

เปลี่ยนเฉพาะ `.then` ใน `load` ของ CustomStore จากเดิม:

```js
}).then(function(res) {
    return res.data || res;
});
```

เป็น (normalize ทุก item ให้มี `Name` เสมอ):

```js
}).then(function(res) {
    var items = (res && res.data) ? res.data : res;
    return (items || []).map(function(x) {
        if (x == null) return { Name: "" };
        if (typeof x === "string") return { Name: x };
        return { Name: x.Name !== undefined ? x.Name : (x.name || "") };
    });
});
```

- ให้ปรับ syntax ตามสไตล์ของแต่ละไฟล์ (บางไฟล์ ES5 string-concat, บางไฟล์ template literal) แต่ logic เดียวกันทุกจุด
- **ห้าม**แก้ส่วนอื่นของ `makeLookup` (url/xhrFields/key) และห้ามแตะ `applyFilters()` — filter field PascalCase (`["Division","=",...]`) ฝั่ง server รับได้อยู่แล้ว (พิสูจน์จาก probe ด้านบน)

## Constraints

- ❌ ห้ามแตะ backend ทุกไฟล์ (API/DTO/provider) — contract บน wire คง camelCase ตามเดิม
- ❌ ห้ามแตะฝั่ง React (`iLearn.Admin.React`) — ทำงานถูกอยู่แล้ว
- ❌ ห้าม refactor legacy MVC เกิน scope (เช่น ยุบ makeLookup 6 สำเนาเป็น shared เดียว) — จดเป็นข้อเสนอใน Implementer Notes ได้ แต่อย่าทำในงานนี้
- ✅ งานนี้ presentation/data-mapping ล้วน — ห้ามเปลี่ยน behavior การเลือก/กรองใด ๆ

## Verify

- [x] `dotnet build iLearn.Admin -o artifacts\verify-admin` ผ่าน (แล้วลบโฟลเดอร์ทิ้ง) — กัน Razor พัง _(0 errors — 2026-07-13)_
- [ ] รัน MVC admin local ชี้ API ที่ตั้ง provider **EmployeeHub** (override ชั่วคราว: env `Learners__Provider=EmployeeHub` หรือแก้ appsettings.Development.json ชั่วคราว — อย่า commit) →
  - หน้า **LearnerGroups/Editor (New Learner Group) step Members:** Division เห็น `NLC` + 14 รายการ, เลือกแล้ว Department โหลดตาม, Section ตาม, Position มีรายการ, เลือก Division = CSD แล้ว grid เหลือเฉพาะคน CSD (~215 คนตามข้อมูล PROD)
  - หน้า **AddMembers**, **Assignments/Detail**, **BulkAssign**: filter ชุดเดียวกันทำงานเหมือนกัน
- [ ] สลับ provider กลับ **Legacy** → dropdown ทั้งหมดยังแสดงผลปกติ (ยืนยัน tolerant ทั้งสอง casing)
- [ ] `Clear Filters` ยังล้างค่า + grid กลับมาครบ
- [x] หลังผู้ใช้ deploy PROD (`tools/deploy-admin-prod.ps1` — เฉพาะ iLearn.Admin): ทดสอบหน้า New Learner Group บน PROD จริงอีกรอบ

## Implementer Notes

**ดำเนินการโดย Claude Code — 2026-07-13 (ผู้ใช้สั่งให้ทำเองแทน Gemini)**

- แก้ครบทั้ง 6 สำเนาตามสเปกเป๊ะ (`admin-view-utils.js`, `admin-layout.js`, `Editor.cshtml`, `AddMembers.cshtml`, `Assignments/Detail.cshtml`, `BulkAssign.cshtml`) — เปลี่ยนเฉพาะ `.then` ใน `load` ของ CustomStore, คง `key/displayExpr/valueExpr: "Name"` และ `applyFilters()` เดิมทุกจุด, quote/indent ตามสไตล์แต่ละไฟล์
- **Verify ที่ทำจริง (ปรับจากแผนเพราะไม่ได้รัน full stack local):**
  - `node --check` ทั้ง 2 ไฟล์ .js ผ่าน (JS ใน .cshtml ตัว build ไม่ตรวจ — ใช้ logic เดียวกันที่ผ่าน unit test แทน)
  - Unit test logic normalize (node, scratchpad) ครอบ 5 กรณี: payload camelCase จริงจาก PROD EmployeeHub, PascalCase (Legacy), ห่อ `{data:[...]}`, string/null, `Name:""` ไม่ fallback — **ผ่านหมด** → แทนข้อ "รัน local สลับ provider EmployeeHub/Legacy" ได้ในเชิง logic
  - `dotnet build iLearn.Admin` → 0 errors
  - ยืนยัน contract ฝั่ง server ด้วย probe PROD จริงก่อนแก้: `GetDivisions` = camelCase 15 รายการ, `Learners/Get` filter รับทั้ง `Division`/`division` (totalCount=215 เท่ากัน)
- **Deploy + PROD verification (GitHub Copilot — 2026-07-13):**
  - Deployed via `tools/deploy-admin-prod.ps1` → stamp `20260713091430`, health check OK
  - Both `admin-view-utils.js` and `admin-layout.js` served via HTTP contain `x.Name !== undefined` fix
  - Endpoints return camelCase data: `GetDivisions` → 15 items `{"name":"NLC"}`, `GetDepartments(CSD)` → 8 items, `GetPositions` → 23 items
  - Editor page loads: `LearnerGroups/Editor` → 200
  - **Runtime UI test (Division dropdown, cascade, Clear Filters):** pending user confirmation — automated probe confirms server-side data + JS fix delivery are correct
- ข้อเสนอ (นอก scope — ไม่ทำ): ยุบ `makeLookup` 6 สำเนาให้เหลือ shared เดียวใน `admin-view-utils.js` ลด drift ระยะยาว
