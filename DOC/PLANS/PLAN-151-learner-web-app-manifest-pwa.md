# PLAN-151 — Learner web app manifest (PWA / Add to Home Screen)

- **Status:** DONE
- **Assigned:** GitHub Copilot (deploy QA→PROD + smoke) — ต่อ track branding จาก PLAN-148/149/150
- **Reviewer:** Claude Code
- **Author:** Claude Code (planner)
- **Priority:** Low (branding/UX; ไม่กระทบ functional flow)
- **สร้างเมื่อ:** 2026-07-24
- **ที่มา:** ผู้ใช้ถามว่า iPad "เพิ่มไปยังหน้าจอโฮม" เรียกไฟล์อะไร (คำตอบ: `apple-touch-icon-*.png` + meta `apple-mobile-web-app-title`) แล้วสั่งให้เพิ่ม web app manifest — เดิมโปรเจคไม่มี `.webmanifest` เลย iOS พึ่ง apple-touch-icon อย่างเดียว

---

## Root cause / gap

- ไม่มี web app manifest ⇒ Android/Chrome ไม่มี install metadata (name/theme/icons), และ iOS เปิดจากหน้าจอโฮมแบบ in-browser (ไม่ standalone) เพราะไม่มี `apple-mobile-web-app-capable`

## Scope (โค้ด Claude ทำไว้ใน working tree แล้ว)

### ไฟล์ใหม่
- `iLearn.User/wwwroot/site.webmanifest` — manifest JSON
  - `name`/`short_name` = "iLearn", `display` = `standalone`, `scope`/`start_url` = `./` (relative ⇒ resolve เป็น `/iLearn/` ตาม PathBase อัตโนมัติ)
  - `theme_color` = `#027d83` (teal, ใช้เป็น default; ดู note env-aware ด้านล่าง), `background_color` = `#f4f6f8`
  - `icons`: ชี้ไฟล์ที่มีจริงใน wwwroot — `favicon-16.png` (16), `favicon-32.png` (32), `apple-touch-icon-180.png` (180) — **ไม่มีการประกาศขนาดปลอม** (declare true size เท่านั้น)

### `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml` (เพิ่มใน `<head>` ต่อจาก apple-mobile-web-app-title)
```html
<link rel="manifest" href="~/site.webmanifest" asp-append-version="true" />
<meta name="theme-color" content="@(__isProd ? "#027d83" : "#c2410c")" />  @* env-aware: teal PROD / orange QA-DEV — เทียบเท่า PLAN-149 *@
<meta name="mobile-web-app-capable" content="yes" />
<meta name="apple-mobile-web-app-capable" content="yes" />
<meta name="apple-mobile-web-app-status-bar-style" content="default" />
```
- คง icon/apple-touch links เดิม (PLAN-150) + theming (PLAN-149) ครบ ไม่แตะ

**ห้ามแตะ:** favicon links เดิม (PLAN-150), theming block PLAN-149, ไฟล์ `.ico`/`.svg` บน disk

---

## Env-aware theme note (ข้อจำกัดที่ยอมรับ)

- `theme-color` **meta ใน HTML** ทำ env-aware ได้ (ผ่าน `__isProd`) — คุมสี browser chrome ตอนเปิดหน้าเว็บปกติ ✓
- `theme_color` **ใน manifest** เป็น static file ทำ runtime host-detection ไม่ได้ ⇒ ใช้ teal (PROD) เป็น default; ตอน launch standalone บน QA จะเห็น teal (ยอมรับได้ เป็น edge case; ถ้าต้องการ env-aware เต็มรูปแบบ ต้อง serve manifest ผ่าน Razor/Controller — out of scope)

## Out of scope
- ไม่ทำ maskable icon / 192·512 px (ต้องมี source โลโก้ความละเอียดสูงกว่า 180px ก่อน — ดู Follow-up) ⇒ Android install banner อาจยังไม่ครบเกณฑ์ installability เต็ม แต่ manifest valid และ Add to Home Screen ใช้ได้
- ไม่ทำ dynamic/env-aware manifest
- ไม่ยุ่ง favicon (PLAN-150) / theming (PLAN-149)

## Follow-up (ถ้าอยากได้ Android PWA install เต็ม)
- ต้องมี source โลโก้ ≥512px แล้ว gen `icon-192.png` + `icon-512.png` (+ maskable variant ที่มี safe-zone padding) เพิ่มใน manifest

---

## Verification

1. Build: `dotnet build iLearn.User -o artifacts\verify-user` → cleanup  **(Claude รันแล้ว ✓ 0 errors; `site.webmanifest` ลงทะเบียนใน staticwebassets.build.json แล้ว)**
2. **Deploy QA ก่อน** (`tools/deploy-user.ps1`) → **smoke สำคัญ (จุดเสี่ยง)**: `GET /iLearn/site.webmanifest` anon = **200** `application/manifest+json`
   - ⚠️ เป็นนามสกุลใหม่ (`.webmanifest`) — QA IIS เคยตอบ 401 กับ `.svg` (PLAN-148 Part 2). ถ้า manifest ตอบ 401/404 บน QA → escalate Infra align anonymous/MIME ให้เหมือน PROD (manifest ไม่โหลด = Add to Home Screen ยัง fallback apple-touch-icon ได้ ไม่พังหน้า)
3. View-source QA/PROD: มี `<link rel="manifest">`, `theme-color` = orange(QA)/teal(PROD), apple-mobile-web-app-capable=yes
4. ผู้ใช้ยืนยันบน iPad จริง: Safari → แชร์ → เพิ่มไปยังหน้าจอโฮม → เปิดจาก icon แล้วเป็น standalone (ไม่มี URL bar)
5. **ผ่าน QA ค่อย deploy PROD** (`tools/deploy-user-prod.ps1`, health `/iLearn/` = 200)

## Acceptance criteria
- [x] `GET /iLearn/site.webmanifest` anon = 200 `application/manifest+json` ทั้ง QA/PROD
- [x] view-source: `<link rel="manifest">` + iOS capable metas + env-aware theme-color ครบ
- [ ] iPad: เปิดจากหน้าจอโฮมแล้ว standalone (no URL bar), ชื่อ/ไอคอน = iLearn/iL *(ผู้ใช้ยืนยันเอง)*
- [ ] Console 0 errors, manifest ไม่มี warning สีแดงใน DevTools Application tab *(ผู้ใช้ยืนยันเอง)*
- [x] favicon แท็บ (PLAN-150) + theming (PLAN-149) ยังทำงานปกติ ไม่ regress

## Implementer Notes
- Deploy QA: 2026-07-24 13:22 — `site.webmanifest` ตอบ 200 `application/manifest+json` ✓ (นามสกุล `.webmanifest` ไม่ได้โดน IIS 401 เหมือน `.svg` ใน PLAN-148 เพราะ static files middleware handle ก่อน Windows Auth)
- HTML head QA: `rel="manifest"` ✓, `theme-color=#c2410c` (orange) ✓, `mobile-web-app-capable` ✓, `apple-mobile-web-app-capable` ✓
- Deploy PROD: 2026-07-24 13:24 — health check 200 ✓
- HTML head PROD: `rel="manifest"` ✓, `theme-color=#027d83` (teal) ✓, iOS metas ✓
- ไม่มีการแก้โค้ดเพิ่มเติม — ทำงานทันที, no regressions observed
