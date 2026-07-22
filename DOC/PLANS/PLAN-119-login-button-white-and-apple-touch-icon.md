# PLAN-119: ปุ่มเข้าสู่ระบบเป็นสีขาว + apple-touch-icon สำหรับ Add to Home Screen บน iPad

- **Status:** READY
- **Assigned:** GitHub Copilot (iLearn.User ล้วน — CSS + asset + layout head)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้ขอ (1) ปุ่ม "เข้าสู่ระบบ" เป็น**สีขาว** (2) บน iPad กด "เพิ่มไปยังหน้าจอโฮม" แล้ว**ไม่เอาไอคอนแอปไปใช้** — ได้ screenshot/ตัวอักษรแทน

---

## วินิจฉัย (ยืนยันจากโค้ด)

**ข้อ 1:** `.btn-login` (`Home/Index.cshtml:77-90`) พื้น `--brand-color` (#027d83) ตัวขาว — ปุ่มวางอยู่บน **login card สีขาว** ⇒ เปลี่ยนเป็นขาวต้องมี border ไม่งั้นกลืนกับการ์ด

**ข้อ 2 (root cause):** iOS Safari ใช้ **`apple-touch-icon` (PNG เท่านั้น — ไม่รองรับ SVG)** เป็นไอคอน Add to Home Screen; ตอนนี้ `iLearn.User` มีแค่ `favicon.svg`/`favicon.ico` และ**ไม่มี `<link rel="apple-touch-icon">`** เลย ซ้ำแอป host ใต้ path `/iLearn/` ⇒ การ probe อัตโนมัติของ Safari ที่ site root (`/apple-touch-icon.png`) ก็ไม่เจอ → iOS จึง fallback เป็น screenshot ย่อ. ต้องมีไฟล์ PNG 180×180 + `<link>` ชี้ตรงใน `<head>` ของ `_DevExtremeLayout.cshtml` (layout เดียวของแอป)

## Scope (iLearn.User ล้วน)

### §1 ปุ่มเข้าสู่ระบบสีขาว (`Home/Index.cshtml`)

```css
.btn-login {
    background-color: #fff !important;
    color: var(--brand-color) !important;
    border: 1.5px solid var(--brand-color) !important;
    font-weight: 600;
    border-radius: var(--radius-sm);
    height: 50px;
    transition: all 0.3s;
}
.btn-login:hover {
    background-color: var(--brand-lighter) !important;   /* #f0f9f9 */
    color: var(--brand-dark) !important;
    border-color: var(--brand-dark) !important;
    transform: translateY(-2px);
    box-shadow: 0 6px 20px rgba(2, 125, 131, 0.15);
}
```
- แตะเฉพาะ block `.btn-login`/`:hover` — ไม่แตะ markup/JS/floating label ของ PLAN-108
- ขนาด/ตำแหน่ง/ข้อความเดิมทุกอย่าง

### §2 apple-touch-icon (แก้ไอคอน Add to Home Screen)

**2.1 สร้าง `wwwroot/apple-touch-icon.png` (180×180)** — ดีไซน์เดียวกับ `favicon.svg` (พื้น #027d83 + อักษร iL ขาว) แต่**พื้นเต็มจัตุรัส ไม่มีมุมโค้ง/โปร่งใส** (iOS ใส่ mask มุมโค้งเอง — มุมโปร่งใสจะกลายเป็นดำ) — gen ด้วย PowerShell + System.Drawing (พิกัด scale ×2.8125 จาก path ใน favicon.svg):

```powershell
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 180,180
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.ColorTranslator]::FromHtml('#027d83'))
$w = [System.Drawing.Brushes]::White
$g.FillRectangle($w, 50.6, 50.6, 16.9, 78.8)    # i stem  (18,18,6,28)
$g.FillRectangle($w, 87.2, 50.6, 16.9, 78.8)    # L stem  (31,18,6,28)
$g.FillRectangle($w, 104.1, 115.3, 36.6, 14.1)  # L foot  (37,41,13,5)
$g.Dispose()
$bmp.Save('iLearn.User\wwwroot\apple-touch-icon.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
```
(จุดบน i ของ favicon ไม่มีอยู่แล้วในดีไซน์เดิม — path เป็นแท่งตรง ไม่ต้องเพิ่ม)

**2.2 `_DevExtremeLayout.cshtml` `<head>`** (ใต้บรรทัด favicon เดิม):
```html
<link rel="apple-touch-icon" sizes="180x180" href="~/apple-touch-icon.png" />
<meta name="apple-mobile-web-app-title" content="iLearn" />
```
- **ห้ามใส่ `asp-append-version`** กับ apple-touch-icon — iOS บางเวอร์ชันไม่โหลด icon ที่มี query string
- ตรวจว่า `.gitignore` ไม่กัน `wwwroot/apple-touch-icon.png` (มี re-include rules อยู่ — ถ้าโดน ignore ให้เพิ่ม `!` ตาม precedent PLAN-108 Fix 3)

### นอก Scope (ห้ามทำ)

- ห้ามทำ full PWA manifest/service worker (ผู้ใช้ขอแค่ไอคอน)
- ห้ามแตะ favicon.svg/ico เดิม
- ห้ามแตะ Admin/API

## Contract ที่เปลี่ยน

ไม่มี — CSS + static asset + head link

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (QA):
1. หน้า login: ปุ่มเข้าสู่ระบบพื้น**ขาว** ตัวอักษร+ขอบสีแบรนด์ มองเห็นชัดบนการ์ดขาว; hover เป็น brand-lighter; กด login ได้ปกติ
2. `https://<qa>/iLearn/apple-touch-icon.png` เปิดแล้วได้ PNG 180×180 พื้นเต็ม (ไม่มีมุมโปร่งใส)
3. view-source ทุกหน้า learner มี `<link rel="apple-touch-icon">` (layout เดียวครอบหมด)
4. **iPad จริง:** Safari → Add to Home Screen → **ไอคอน iL สีแบรนด์**ขึ้นแทน screenshot; ชื่อ "iLearn" — ⚠️ ถ้าเคย add ไว้ก่อน ต้อง**ลบอันเก่าแล้ว add ใหม่** (iOS cache icon ต่อ URL)
5. console 0 error

## Deploy note

- **iLearn.User เท่านั้น** — QA → verify (ข้อ 4 ต้องผู้ใช้ทดสอบบน iPad จริง) → PROD รอผู้ใช้ยืนยัน

## Implementer Notes

_(เติมโดย implementer)_
