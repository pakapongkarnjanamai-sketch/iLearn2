# PLAN-019: กัน `wwwroot/**` ใน .gitignore ไม่ให้ ignore ไฟล์ source ที่ track อยู่ของ iLearn.Admin

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: check-ignore ยืนยัน admin-view-utils.js ไม่ ignore / devextreme/dx.all.js ยัง ignore, 7 ไฟล์ source ไม่หลุด)
- **Assigned:** GPT
- **Priority:** Low
- **Estimated scope:** 1 ไฟล์ (`.gitignore`)

## Problem

`.gitignore` มี rule `iLearn.Admin/wwwroot/**` (ตั้งใจ ignore ไฟล์ generated/vendored เช่น DevExtreme libs) แต่มันกลืนไฟล์ **source เขียนมือ** 7 ไฟล์ที่ track อยู่ก่อนเพิ่ม rule ด้วย:
- `iLearn.Admin/wwwroot/css/admin-minimal.css`, `admin-tokens.css`, `admin-wizard.css`, `site.css`
- `iLearn.Admin/wwwroot/js/admin-layout.js`, `admin-view-utils.js`, `site.js`

ตอนนี้ไฟล์เหล่านี้ยัง track อยู่ (ทำงานได้) แต่ rule ทำให้ **การแก้/เพิ่มไฟล์ใหม่ในโฟลเดอร์นี้ถูก ignore เงียบ ๆ** (เสี่ยงโค้ดหายเวลาทำงานกับ admin เก่า)

> ตรวจด้วย `git ls-files | git check-ignore --no-index --verbose --stdin` — 7 ไฟล์นี้แสดง rule `iLearn.Admin/wwwroot/**` ที่ match (เหลือกลุ่มนี้กลุ่มเดียว)

## Scope (ทำแค่นี้)

แก้ `.gitignore` — เพิ่ม negation ให้ไฟล์ source เขียนมือที่ track อยู่ใต้ `iLearn.Admin/wwwroot` ไม่ถูก ignore โดย **ยังคง ignore ของ vendored/generated** (เช่น `wwwroot/js/devextreme/**`, `wwwroot/lib/**`) ไว้เหมือนเดิม

แนวทางแนะนำ (negate เฉพาะที่ track จริง — แม่นและปลอดภัยสุด):
```gitignore
# (หลังบรรทัด iLearn.Admin/wwwroot/** เดิม)
# Keep hand-written admin assets that predate the wwwroot ignore (vendored libs stay ignored)
!iLearn.Admin/wwwroot/css/
!iLearn.Admin/wwwroot/css/admin-minimal.css
!iLearn.Admin/wwwroot/css/admin-tokens.css
!iLearn.Admin/wwwroot/css/admin-wizard.css
!iLearn.Admin/wwwroot/css/site.css
!iLearn.Admin/wwwroot/js/
!iLearn.Admin/wwwroot/js/admin-layout.js
!iLearn.Admin/wwwroot/js/admin-view-utils.js
!iLearn.Admin/wwwroot/js/site.js
```
(ต้อง un-ignore โฟลเดอร์ `css/` `js/` ก่อน git ถึงจะ recurse เข้าไปเจอ negation ของไฟล์ได้ — แต่ **อย่า** un-ignore `js/devextreme/` หรือ vendored อื่น)

## Out of scope (ห้ามแตะ)

- ห้าม un-ignore โฟลเดอร์ vendored: `iLearn.Admin/wwwroot/js/devextreme/**`, `lib/**`, หรือไฟล์ generated อื่น
- ห้ามแตะ rule `iLearn.API/wwwroot/**`, `iLearn.User/wwwroot/**`
- ห้าม `git add` ไฟล์ vendored ใหม่เข้ามา (แค่กันไฟล์ที่ track อยู่ 7 ตัวไม่ให้หลุด)

## Acceptance criteria

- [x] `git check-ignore --no-index iLearn.Admin/wwwroot/js/admin-view-utils.js` → **ไม่** ถูก ignore
- [x] `git check-ignore --no-index iLearn.Admin/wwwroot/js/devextreme/dx.all.js` → **ยัง** ถูก ignore (vendored คงเดิม)
- [x] `git status` ไม่มีไฟล์ vendored โผล่เป็น untracked ก้อนใหญ่
- [x] 7 ไฟล์ source ยัง track เหมือนเดิม (ไม่มี deletion)

## Verification

```powershell
git check-ignore --no-index iLearn.Admin/wwwroot/js/admin-view-utils.js   # คาดว่าไม่พิมพ์อะไร (ไม่ ignore)
git check-ignore --no-index iLearn.Admin/wwwroot/js/devextreme/dx.all.js  # คาดว่าพิมพ์ path (ยัง ignore)
git status --short                                                         # ไม่มี vendored untracked ก้อนใหญ่
```

## Implementer Notes

- แก้ `.gitignore` โดยเพิ่ม negation หลัง rule `iLearn.Admin/wwwroot/**` เพื่อ keep เฉพาะไฟล์ source ที่เขียนมือและ track อยู่เดิม:
	- `iLearn.Admin/wwwroot/css/admin-minimal.css`
	- `iLearn.Admin/wwwroot/css/admin-tokens.css`
	- `iLearn.Admin/wwwroot/css/admin-wizard.css`
	- `iLearn.Admin/wwwroot/css/site.css`
	- `iLearn.Admin/wwwroot/js/admin-layout.js`
	- `iLearn.Admin/wwwroot/js/admin-view-utils.js`
	- `iLearn.Admin/wwwroot/js/site.js`
- ยังคง ignore vendored/generated เดิมไว้ (เช่น `iLearn.Admin/wwwroot/js/devextreme/**`) โดยไม่ un-ignore โฟลเดอร์ vendored
- Verification ที่รันแล้ว:
	- `git check-ignore --no-index iLearn.Admin/wwwroot/js/admin-view-utils.js` → ไม่มี output (ไม่ถูก ignore)
	- `git check-ignore --no-index iLearn.Admin/wwwroot/js/devextreme/dx.all.js` → แสดง path (ยังถูก ignore)
	- `git ls-files` ยืนยัน 7 ไฟล์ source ยัง track ครบ
	- `git status --short` ไม่พบ vendored untracked flood
	- `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน
	- `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)
	- ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว
