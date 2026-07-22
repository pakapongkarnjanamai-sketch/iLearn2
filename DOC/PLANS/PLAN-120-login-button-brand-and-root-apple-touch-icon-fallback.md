# PLAN-120: ปุ่ม login กลับเป็นสีแบรนด์ + root apple-touch-icon fallback

- **Status:** DEPLOYED (รอ manual QA บน iPad จริง)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** follow-up จาก PLAN-119 — ผู้ใช้ขอให้ปุ่ม "เข้าสู่ระบบ" กลับเป็นพื้นเขียวตัวหนังสือขาว และรายงานว่า shortcut บน iPad ยังไม่ขึ้นไอคอน แม้ `/iLearn/apple-touch-icon.png` ตอบได้แล้ว

## วินิจฉัย

- ปุ่ม login ต้อง revert จากพื้นขาวกลับไปเป็น `--brand-color` + ตัวหนังสือขาวตามรูปแบบเดิมของ Learner UI
- PLAN-119 เพิ่ม icon ใต้ app path (`/iLearn/apple-touch-icon.png`) และ link ใน layout แล้ว แต่ iOS ยัง fallback เป็น screenshot จึงต้องเพิ่ม app-scoped cache-busted filename (`/iLearn/apple-touch-icon-180.png`) สำหรับ Safari/iOS ที่ cache icon URL เดิมไว้
- `tools/deploy-side-by-side.ps1` sync static assets ไปที่ app root (`/iLearn/wwwroot`) เท่านั้น ไม่ได้ copy asset ไป site root จึงต้องเติม step เฉพาะ `deploy-user*.ps1`

## Scope

1. `Home/Index.cshtml`: `.btn-login` กลับเป็นพื้น `var(--brand-color)` ตัวขาว; hover เป็น `var(--brand-dark)` ตัวขาว
2. `_DevExtremeLayout.cshtml`: เพิ่ม app-scoped `apple-touch-icon-180.png` + `apple-touch-icon-precomposed.png` links โดยไม่มี query string และคง app-scoped link เดิมไว้
3. `tools/deploy-user.ps1` และ `tools/deploy-user-prod.ps1`: หลัง deploy สำเร็จ copy `iLearn.User/wwwroot/apple-touch-icon.png` ไป site root เป็นทั้ง `apple-touch-icon.png` และ `apple-touch-icon-precomposed.png` เป็น best-effort fallback; smoke พบว่า IIS site root ยังตอบ 401 จึงไม่ใช้ root URL เป็น primary link
4. Deploy เฉพาะ `iLearn.User` ไป QA และ PROD แล้ว smoke test root/app icon URLs

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

หลัง deploy:
- `https://ap-ntc2138-qawb/apple-touch-icon.png` = 401 `text/html` (IIS site-root auth; not used by final page links)
- `https://ap-ntc2138-qawb/apple-touch-icon-precomposed.png` = 401 `text/html` (IIS site-root auth; not used by final page links)
- `https://ap-ntc2138-qawb/iLearn/apple-touch-icon-180.png` = 200 `image/png`
- `https://ap-ntc2138-qawb/iLearn/apple-touch-icon-precomposed.png` = 200 `image/png`
- `https://ap-ntc2138-qawb/iLearn/apple-touch-icon.png` = 200 `image/png`
- ทำซ้ำบน PROD `ap-ntc2137-prwb`
- view-source `/iLearn/` มี app-scoped `apple-touch-icon-180`, `apple-touch-icon-precomposed`, และ fallback `apple-touch-icon` links

## Implementer Notes

- ปุ่ม login กลับเป็นพื้น `var(--brand-color)` และตัวอักษร `#fff`; hover เป็น `var(--brand-dark)` + ตัวขาว
- เพิ่มไฟล์ app-scoped cache-busted: `apple-touch-icon-180.png` และ `apple-touch-icon-precomposed.png` (สำเนา PNG 180x180 เดิม) และ re-include ใน `.gitignore`
- Layout ชี้ icon ลำดับแรกไปที่ `/iLearn/apple-touch-icon-180.png`, ต่อด้วย `/iLearn/apple-touch-icon-precomposed.png`, แล้วคง `/iLearn/apple-touch-icon.png` เป็น fallback เดิม — ทั้งหมดไม่มี query string
- เพิ่ม best-effort root icon sync ใน `tools/deploy-user.ps1`/`tools/deploy-user-prod.ps1`, แต่ smoke พบว่า site-root URLs (`/apple-touch-icon*.png`) ตอบ 401 ในทั้ง QA/PROD; final page links จึงไม่ใช้ root URL
- Verified local: `dotnet build iLearn.User\iLearn.User.csproj -o artifacts\verify-user` ผ่าน 0 errors; PowerShell parser สำหรับ deploy scripts ผ่าน; ลบ artifacts แล้ว
- **QA deploy:** final stamp `_user_deploy_20260722132157`; health check `https://ap-ntc2138-qawb/iLearn/` = HTTP 200; `AutoRolledBack=False`; smoke: page มี `apple-touch-icon-180` + `precomposed` links, ปุ่มเขียวตัวขาว, icon URLs ใต้ `/iLearn/` ทั้ง 3 เส้น = HTTP 200 `image/png`
- **PROD deploy:** final stamp `_user_deploy_20260722132250`; health check `https://ap-ntc2137-prwb/iLearn/` = HTTP 200; `AutoRolledBack=False`; smoke: page มี `apple-touch-icon-180` + `precomposed` links, ปุ่มเขียวตัวขาว, icon URLs ใต้ `/iLearn/` ทั้ง 3 เส้น = HTTP 200 `image/png`
- **คงเหลือ:** manual QA บน iPad จริง ต้องลบ shortcut เก่าออกก่อน แล้ว Add to Home Screen ใหม่ เพื่อบังคับ Safari อ่าน URL ใหม่ (`apple-touch-icon-180.png`)

## Manual QA

- iPad: ลบ shortcut เดิมก่อน แล้ว Safari → Add to Home Screen ใหม่ ต้องเห็นไอคอน iL สีแบรนด์และชื่อ `iLearn`