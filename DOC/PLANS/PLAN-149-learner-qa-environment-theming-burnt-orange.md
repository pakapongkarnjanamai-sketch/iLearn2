# PLAN-149 — แยกโทนสี QA vs PROD ฝั่ง Learner (iLearn.User) — QA = burnt orange + ป้าย QA

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (code + deploy QA/PROD + smoke)
- **Reviewer:** Claude Code
- **Author:** Claude Code (planner)
- **Priority:** Medium-High (กันผู้เรียน/ผู้ทดสอบสับสนว่าอยู่ QA หรือ PROD — ต่อยอดจาก PLAN-073 ที่ทำ admin ไปแล้ว)
- **สร้างเมื่อ:** 2026-07-24
- **ที่มา:** ผู้ใช้ต้องการให้หน้า Learner `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` เปลี่ยน "สีหลักทั้งหมด" เป็นสีคู่ตรงข้าม เพื่อแยก QA ออกจาก PROD ให้ชัด ไม่ให้ผู้ใช้สับสน

---

## ข้อเท็จจริงสำคัญ (ยืนยันจากโค้ด/สคริปต์แล้ว)

- **PLAN-073 (VERIFIED)** ทำ environment theming ไปแล้ว**เฉพาะ `iLearn.Admin.React` + `iLearn.Admin` MVC** (QA=amber ตรงข้าม PROD=indigo) โดยใช้ **runtime hostname detection** — **ยังไม่เคยครอบ `iLearn.User`** งานนี้คือส่วนที่ขาดของ PLAN-073 ไม่ใช่งานซ้ำ
- **สีหลักของ Learner คุมจากตัวแปรเดียว** ใน [`user-theme.css`](../../iLearn.User/wwwroot/css/user-theme.css):
  - `--brand-color: #027d83` (teal), `--brand-dark: #004d40`, `--brand-light: #e0f2f1`, `--brand-lighter: #f0f9f9`
  - navbar / ปุ่ม / ลิงก์ / badge / progress / focus ทุกที่อ้างผ่านตัวแปรเหล่านี้ ⇒ **override ตัวแปร = เปลี่ยนสีหลักทั้งเว็บพร้อมกัน** (นี่คือสิ่งที่ user ขอ) โดยคอนทราสต์ยังปลอดภัย (ขาวบนส้มเผาอ่านออกเท่าขาวบน teal)
- **ยกเว้น** มีค่าลิเทอรัล `rgba(2, 125, 131, x)` ฝังตรง (focus glow + shadow) ที่ **ไม่ผูกกับตัวแปร** — บรรทัด 80, 123, 133, 400, 404, 408 — ถ้าไม่แก้ QA จะยังมี glow สี teal จาง ๆ ค้าง (จุดเล็กแต่ให้ทำให้ครบ)
- **env pin:** `tools/deploy-user.ps1` ตั้ง `SetEnvironmentName='Staging'` บน QA ส่วน PROD='Production' — แต่ **แผนนี้เลือกใช้ hostname detection ตาม precedent PLAN-073** (reviewer คุ้นแล้ว + ไม่พึ่งการ pin env ใน deploy) ไม่ใช้ env name
- ไฟล์ layout ที่ต้องแตะ ([`_DevExtremeLayout.cshtml`](../../iLearn.User/Views/Shared/_DevExtremeLayout.cshtml)) **เพิ่งถูกแก้โดย PLAN-148** (บล็อก favicon บรรทัด 12–20) — **ห้าม revert บล็อก favicon นั้น** แตะเฉพาะ `<title>`, ส่วน override ใน `<head>`, และ navbar-brand

## หลักการออกแบบ (คงตาม PLAN-073)

- **PROD = หน้าตาปัจจุบันเป๊ะ (teal) — ไม่เปลี่ยนอะไรเลย** ทุกการเปลี่ยนอยู่หลังเงื่อนไข `!isProd` เท่านั้น
- **QA = burnt orange `#c2410c`** (คู่ตรงข้ามของ teal บนวงล้อสี, เข้าชุด "QA = warm" เดียวกับ admin amber)
- **localhost/dev = non-PROD → ใช้โทน QA แต่ป้ายเขียน `DEV`**
- ไม่ยุ่ง favicon (เลี่ยงปัญหา SVG 401 ของ QA ที่ PLAN-148 ยังค้าง Outstanding) — ใช้ **ป้ายบนจอ + title suffix** เป็นสัญญาณ belt-and-suspenders แทน
- ไม่แตะ deploy scripts / appsettings / web.config / IIS (runtime detection ล้วน, artifact เดียว rollback ง่าย)

---

## Scope (ทำแค่นี้ — 2 ไฟล์)

### ไฟล์ 1 — `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`

**(1) เพิ่ม env detection บนหัวไฟล์** (หลัง `@inject` เดิม, ก่อน `<!DOCTYPE html>`):
```csharp
@{
    var __host = Context.Request.Host.Host ?? "";
    var __isProd = __host.Contains("prwb", StringComparison.OrdinalIgnoreCase);
    var __isDev  = __host.Contains("localhost", StringComparison.OrdinalIgnoreCase) || __host.StartsWith("127.");
    var __envLabel = __isProd ? null : (__isDev ? "DEV" : "QA");   // qawb หรืออื่น ๆ ที่ไม่ใช่ prod/local → ถือเป็น QA (fail-safe)
}
```

**(2) `<title>` suffix** (บรรทัด 10):
- ก่อน: `<title>iLearn</title>`
- หลัง: `<title>iLearn@(__isProd ? "" : $" ({__envLabel})")</title>`

**(3) inject brand override block** — วาง **ท้ายสุดใน `<head>` (หลัง `@await RenderSectionAsync("Styles", ...)` บรรทัด 41)** เพื่อให้ชนะ cascade แน่นอน:
```html
@if (!__isProd)
{
    <style>
        /* PLAN-149: non-PROD brand override (QA/DEV). PROD ไม่โดนแตะ */
        :root {
            --brand-color:   #c2410c;
            --brand-dark:    #7c2d12;
            --brand-light:   #ffedd5;
            --brand-lighter: #fff7ed;
            --brand-shadow-rgb: 194, 65, 12;   /* ให้ focus glow / shadow ตามสี QA (ดูไฟล์ 2) */
        }
    </style>
}
```

**(4) ป้าย environment ใน navbar-brand** (บรรทัด 49–51) — เติมหลังข้อความ `iLearn`:
```html
<a class="navbar-brand" href="@Url.Action("Index", "Home")">
    <i class="fas fa-graduation-cap"></i> iLearn
    @if (!__isProd)
    {
        <span class="env-badge">@__envLabel</span>
    }
</a>
```

**ห้ามแตะ:** บล็อก favicon (บรรทัด 12–20, ของ PLAN-148), ส่วน `<script>`/skeleton/toast, footer

### ไฟล์ 2 — `iLearn.User/wwwroot/css/user-theme.css`

**(1) เพิ่มตัวแปร shadow rgb ใน `:root`** (ในบล็อก colors ราว ๆ บรรทัด 3–9):
```css
--brand-shadow-rgb: 2, 125, 131;   /* PLAN-149: ผูก rgba focus/shadow กับตัวแปร เพื่อให้ QA override ได้ครบ */
```

**(2) แทนที่ลิเทอรัล `rgba(2, 125, 131, x)` ทั้ง 6 จุด** (บรรทัด 80, 123, 133, 400, 404, 408) → `rgba(var(--brand-shadow-rgb), x)` (คง alpha เดิมของแต่ละจุด: .5 / .25 / .25 / .15 / .075 / .175)
- ผลฝั่ง PROD: ค่าที่ได้เท่าเดิมเป๊ะ (2,125,131) — pixel identical
- ผลฝั่ง QA: กลายเป็น 194,65,12 อัตโนมัติจาก override ในไฟล์ 1

**(3) เพิ่ม style ป้าย `.env-badge`** (ต่อท้ายไฟล์ หรือใกล้บล็อก navbar):
```css
/* PLAN-149: environment badge (แสดงเฉพาะ non-PROD ผ่านเงื่อนไขใน layout) */
.env-badge {
    display: inline-block;
    margin-left: 8px;
    padding: 2px 8px;
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.5px;
    line-height: 1.4;
    border-radius: var(--radius-pill);
    background: #fff;
    color: var(--brand-dark);   /* ตาม override → ส้มเข้มบนพื้นขาว บน navbar QA */
    vertical-align: middle;
}
```

**ห้ามแตะ:** ค่าตัวแปร `--brand-color` ฯลฯ เดิม (PROD ต้องคง teal), spacing/radius/type scale, skeleton, toast

---

## Out of scope (ห้ามแตะ)

- favicon ทุกชนิด (ยกให้ PLAN-148 / Part 2 IIS) — งานนี้ใช้ป้าย+title เป็นสัญญาณแทน
- PROD look — ทุกจุดต้องอยู่หลัง `!__isProd` (เกณฑ์รีวิวสำคัญสุด: PROD ต้อง pixel-identical)
- deploy scripts / appsettings / web.config / IIS config
- `iLearn.Admin.React`, `iLearn.Admin` (PLAN-073 ทำแล้ว), `iLearn.API`
- ไฟล์อื่นใน layout ที่ไม่เกี่ยว (favicon block, scripts, footer)

---

## Acceptance criteria

- [x] **PROD (2137):** เปิด `https://ap-ntc2137-prwb.nikonoa.net/iLearn/` → navbar/ปุ่ม/ลิงก์ **เขียว teal เดิมเป๊ะ**, ไม่มีป้าย, `<title>` = `iLearn` (ไม่มี suffix) — เกณฑ์สำคัญสุด
- [x] **QA (2138):** เปิด `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` → navbar/ปุ่ม/ลิงก์/badge/progress **เป็น burnt orange `#c2410c`**, มีป้าย `QA` ขาวข้าง brand, `<title>` = `iLearn (QA)`, tab แสดง `iLearn (QA)`
- [ ] **Local dev:** โทนส้ม + ป้าย `DEV` + title `iLearn (DEV)`
- [x] focus glow ของ input / shadow-brand ฝั่ง QA เป็นโทนส้ม (ไม่มี teal จาง ๆ ค้าง)
- [x] Console 0 errors ทั้งสอง env; หน้า `/iLearn/` = 200 ทั้งสอง env

---

## Verification

**Build (local, ก่อน deploy):**
```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

**จำลอง env ก่อน deploy (local):** เปิดผ่าน `localhost` → เห็นส้ม + ป้าย DEV + title (DEV); ยืนยัน logic `!isProd`

**Deploy:** `tools/deploy-user.ps1` (QA) — จด stamp + health `/iLearn/` = 200. Deploy PROD (`tools/deploy-user-prod.ps1`) เมื่อพร้อม แล้วยืนยัน PROD เหมือนเดิมเป๊ะ

**Browser smoke (หลัง deploy):** เปิด QA เทียบ PROD — แนบ screenshot ทั้งสองใน Implementer Notes (QA ส้ม+ป้าย, PROD teal ไม่มีป้าย); ตรวจ `document.title` และ computed `--brand-color` ของ `:root` ทั้งสอง env

---

## Implementer Notes

- Implementation scope done ตามแผนครบ 2 ไฟล์:
    - `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`
        - เพิ่ม hostname detection (`__isProd`, `__isDev`, `__envLabel`)
        - เปลี่ยน `<title>` เป็น `iLearn`, `iLearn (QA)`, `iLearn (DEV)` ตาม env
        - inject non-PROD `<style>` override ที่ท้าย `<head>` (`--brand-color: #c2410c` + `--brand-shadow-rgb`)
        - เพิ่ม `<span class="env-badge">` แสดง QA/DEV ใน navbar brand
        - ไม่แตะบล็อก favicon ของ PLAN-148
    - `iLearn.User/wwwroot/css/user-theme.css`
        - เพิ่ม `--brand-shadow-rgb: 2, 125, 131` ใน `:root`
        - แทนที่ literal `rgba(2, 125, 131, x)` ครบ 6 จุดเป็น `rgba(var(--brand-shadow-rgb), x)`
        - เพิ่ม style `.env-badge`

- Build verify:
    - `dotnet build iLearn.User -o artifacts\verify-user` ✓
    - `Remove-Item -Recurse -Force artifacts\verify-user` ✓

- Deploy stamps:
    - QA: `tools/deploy-user.ps1` → stamp `20260724092554`
    - PROD: `tools/deploy-user-prod.ps1` → stamp `20260724092705` (health `/iLearn/` = 200 จากสคริปต์)

- Smoke results (browser + runtime checks):
    - QA `https://ap-ntc2138-qawb.nikonoa.net/iLearn/`
        - `document.title` = `iLearn (QA)`
        - computed `--brand-color` = `#c2410c`
        - `.env-badge` = `QA`
        - console errors = `0`
    - PROD `https://ap-ntc2137-prwb.nikonoa.net/iLearn/`
        - `document.title` = `iLearn`
        - computed `--brand-color` = `#027d83`
        - `.env-badge` = `null` (ไม่แสดง)
        - console errors = `0`

- Screenshot smoke: จับภาพ QA (โทน burnt orange) และ PROD (teal เดิม) ใน session นี้แล้ว

- Outstanding:
    - ยังไม่ได้เปิด local `localhost` เพื่อติ๊ก acceptance ข้อ DEV ในรอบนี้

## Reviewer Notes (Claude Code — 2026-07-24)

รีวิว diff จริง 2 ไฟล์ — **ผ่าน ตรงตามแผนครบ**:
- **`_DevExtremeLayout.cshtml`**: env detection (`__isProd` = host มี `prwb`, `__isDev` = localhost/127.*, else QA) ถูกต้อง fail-safe (non-PROD ที่ไม่ใช่ dev → QA); ทุกจุด (title suffix, `<style>` override, `.env-badge`) อยู่หลัง `!__isProd` ⇒ **PROD ไม่โดนแตะแม้แต่จุดเดียว**; override block วางท้าย `<head>` (หลัง Styles section) ⇒ ชนะ cascade แน่นอน; บล็อก favicon ของ PLAN-148 (บรรทัด 18–26) คงเดิมครบ ไม่ถูก revert
- **`user-theme.css`**: `--brand-shadow-rgb: 2, 125, 131` (teal เดิม) เป็น default; แปลง rgba literal ครบ 6 จุด โดย **alpha คงเดิมทุกค่า** (.5 / .25 / .25 / .15 / .075 / .175) ⇒ PROD ได้ค่าเท่าเดิม pixel-identical, QA เปลี่ยนตาม override อัตโนมัติ; `.env-badge` พื้นขาว + `color: var(--brand-dark)` = ส้มเข้มบน navbar QA อ่านออกชัด
- **Smoke (จาก Implementer Notes)**: QA `--brand-color=#c2410c` + badge `QA` + title `iLearn (QA)`, PROD `--brand-color=#027d83` + ไม่มี badge + title `iLearn`, console 0 errors ทั้งสอง env — ตรง acceptance ข้อหลัก (PROD pixel-identical)
- **Outstanding ที่ยอมรับได้**: acceptance ข้อ localhost/DEV ยังไม่ได้ verify ด้วยตา (logic เดียวกับ QA path ต่างแค่ label string — ความเสี่ยงต่ำ) — ไม่บล็อก
- **สรุป:** presentation-only, ไม่มี contract change, PROD ปลอดภัย → VERIFIED
