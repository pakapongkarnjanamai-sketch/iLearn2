# PLAN-141: Deploy QA/PROD — เฟสสองภาษาเต็มรูปแบบ + PLAN-137/139 + smoke สองภาษา

- **Status**: DONE
- **Assigned**: GitHub Copilot (GPT)
- **Created**: 2026-07-23
- **หมายเหตุเลขแผน**: เดิมสร้างเป็น PLAN-140 แต่ชนกับ `PLAN-140-canonical-host-redirect` (Gemini สร้างก่อน) — ย้ายมาเป็น 141 เมื่อ 2026-07-23

## Overview

Deploy งานสะสม 6 commits ที่ยังไม่เคยขึ้น QA/PROD (deploy ล่าสุด = stamp `20260723104219` เนื้อหาถึง `9d88dcb`):

| Commit | เนื้อหา | ผลต่อ deploy |
|---|---|---|
| `da5d1c6` | PLAN-136 P0+A+B: language switcher + nav/shared UI + dashboard | frontend |
| `db507b1` | **PLAN-137 backend**: `PATCH Assignments/{id}/description` + Zone C reports | **backend เปลี่ยน ⇒ full API publish** |
| `c077fd9` + `6fac6ef` | PLAN-138 Zone D–F: bilingual ทั้งแอป | frontend |
| `021608d` | PLAN-139: StatTile overview cards (Gemini, รีวิวแล้ว) | frontend |
| `2ee587d` | fix 413 message เข้า dictionary (รีวิว follow-up) | frontend |

ทุก commit ผ่านรีวิวโดย Claude แล้ว (ดู AGENT_LOG) — lint ✓ build ✓ tests 248/248 ✓ ณ HEAD

## §1 Pre-flight

1. `git status` ต้องสะอาด — ถ้ามีไฟล์ใหม่จาก agent อื่น **หยุด** ตรวจ AGENT_LOG ก่อน ห้ามกวาดเข้า deploy
2. **เครื่องนี้อาจมี dev API รันค้างที่ `localhost:7128`** (bin ล็อก) — ถ้า `dotnet publish` ล้มด้วย MSB3021 ให้หา process `iLearn.API` แล้วหยุดก่อน (`Get-Process | Where-Object ProcessName -Match "iLearn"`) — vite dev server (5173) ไม่เกี่ยว ปล่อยได้
3. Verification gate:

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
# backend (ใช้ artifacts pattern ถ้า bin ล็อก)
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

4. **Migration gate** — PLAN-137 ไม่มี migration แต่ต้องยืนยัน: ตั้ง `ConnectionStrings__DefaultConnection` ให้ design-time factory ก่อน (`AppDbContextFactory` ไม่อ่าน appsettings — ไม่ตั้ง = fallback LocalDB โชว์ pending เทียม, บทเรียน PLAN-135) แล้ว `dotnet ef migrations list` ทั้ง QA/PROD ต้องไม่มี pending

## §2 Deploy QA

1. `tools/deploy-api.ps1` — **full publish** (backend เปลี่ยนจาก db507b1 — ห้าม SkipPublish)
2. `tools/deploy-admin-react.ps1`
3. Health checks: `/Service/api/admin/session/me` = 401 (anonymous), `/admin-react/` = 200, `/` = 200

## §3 QA smoke (จุดสำคัญ = สองภาษา)

**Bilingual core (ครั้งแรกบนเซิร์ฟเวอร์จริง):**

1. เปิด admin-react → default = **ไทย** (sidebar แดชบอร์ด/คอร์สเรียน/งานมอบหมาย, สวิตช์ ไทย|EN ใน Header)
2. กด **EN** → ทั้งหน้าเปลี่ยน (sidebar/หัวตาราง/badge/ปุ่ม) — ไล่เช็ค: Dashboard, `/assignments` (หัวตาราง moduleConfigs ต้องเป็น Assignment No./Status/...), assignment detail, `/courses`, `/master-data/divisions`, `/users`, `/system-config`, `/reports` + เข้า compliance 1 หน้า, `/notifications`
3. Refresh browser → ภาษา EN คงอยู่ (localStorage) → สลับกลับ ไทย → หัวตารางพลิกกลับครบ (จับ regression ค่าค้าง)
4. เช็คว่า**ไม่มีข้อความสองภาษาปนในหน้าเดียว** (ยกเว้นชื่อ technical/ข้อมูล DB) + console 0 errors ทุกหน้าที่เข้า

**PLAN-137 (บน QA เท่านั้น):**

5. Assignment detail ของ batch ทดสอบ → ปุ่ม "แก้ไขคำอธิบาย" → แก้ข้อความ → บันทึก → คำอธิบายอัปเดต + ตรวจว่า batch เดียวกันทุก rule เปลี่ยนตาม (เปิด list ดู) → แก้กลับค่าเดิม

**PLAN-139:**

6. Detail pages (assignment/course/user/master-data อย่างละ 1) → overview card แสดง StatTile ปกติ ตัวเลขถูก ทั้งสองภาษา

## §4 Deploy PROD + read-only smoke

1. `tools/deploy-api-prod.ps1` (full publish) + `tools/deploy-admin-react-prod.ps1` + health checks 3 URL
2. Smoke **read-only**: ข้อ 1–4 ของ §3 (**ห้าม**แก้ description/ข้อมูลใด ๆ บน PROD)
3. จุดระวังพิเศษบน PROD: ผู้ใช้จริงใช้ไทยเป็นหลัก — default ต้องเป็นไทยและหน้าตาเหมือนก่อน deploy ทุกจุด (การเปลี่ยนแปลงที่ผู้ใช้เดิมเห็น = มีสวิตช์ภาษาเพิ่ม + หัวตาราง/ปุ่มบางจุดที่เคยเป็นอังกฤษกลายเป็นไทย — คาดหวังไว้แล้ว ไม่ใช่ bug)

## §5 ปิดสถานะ

- PLAN-136, PLAN-137, PLAN-138, PLAN-139: → `VERIFIED`
- PLAN-141: → `DONE` + Implementer Notes (stamp, ผล smoke, ปัญหาที่เจอ)
- ลง `AGENT_LOG.md` (entry ใหม่บนสุด)

## Out of Scope

- ห้ามแก้โค้ดใด ๆ — พบปัญหา = จด Implementer Notes + แจ้งผู้ใช้ (ยกเว้น hotfix ระดับ config/deploy script ที่จำเป็นต่อการ deploy สำเร็จ)
- ห้ามรัน migration ใด ๆ (ไม่มีในรอบนี้ — ถ้า gate เจอ pending = หยุดแล้วแจ้งผู้ใช้ทันที)

## Implementer Notes

- Pre-flight ผ่านครบ: `git status` สะอาด, `npm run lint` ✓, `npm run build` ✓, `dotnet build iLearn.Tests -o artifacts\verify-test` ✓, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ✓; migration gate QA/PROD ไม่มี pending เมื่อกำหนด `ConnectionStrings__DefaultConnection` ให้ design-time factory ตามแผน
- ระหว่าง migration gate พบ `dotnet ef migrations list` ล้มเพราะมี local dev process `iLearn.API.exe` (PID 41724) ล็อก `iLearn.API\bin\Debug\*.dll`; หยุด process นั้นแล้ว rerun gate ผ่านทั้ง QA และ PROD
- QA deploy สำเร็จ: `tools/deploy-api.ps1` stamp `20260723150900` (post-deploy health check `/Service/api/admin/session/me` = 401 บน attempt แรก, auto-rollback = false) + `tools/deploy-admin-react.ps1` copy สำเร็จ (`RobocopyExitCode=3`)
- QA smoke ผ่าน: anonymous `/Service/api/admin/session/me` = 401, authenticated `/admin-react/` = 200, `/` = 200; browser smoke สองภาษาผ่านบน Dashboard, `/assignments`, assignment detail `/assignments/275`, `/courses`, `/master-data/divisions`, `/users`, `/system-config`, `/reports`, `/reports/compliance`, `/notifications`; refresh แล้ว EN คงอยู่ใน `localStorage`, สลับกลับไทยแล้วหัวตาราง assignments กลับเป็น `เลขที่งานมอบหมาย`, console 0 errors ทุกหน้าที่ตรวจ
- PLAN-137 smoke บน QA ผ่าน: แก้ description ของ assignment 275 เป็นค่า test ชั่วคราว, ตรวจว่า detail + list อัปเดตตาม batch เดียวกัน, แล้ว revert กลับค่าเดิม `Training WI_PD2` เรียบร้อย
- PLAN-139 smoke ผ่านบน QA: overview cards ของ assignment/course/user/master-data แสดง `Overview/ภาพรวม` และ StatTile ปกติทั้งสองภาษา
- PROD deploy สำเร็จ: `tools/deploy-api-prod.ps1` stamp `20260723151448` (post-deploy health check `/Service/api/admin/session/me` = 401, auto-rollback = false) + `tools/deploy-admin-react-prod.ps1` copy สำเร็จ (`RobocopyExitCode=3`)
- PROD read-only smoke ผ่าน: anonymous `/Service/api/admin/session/me` = 401, authenticated `/admin-react/` = 200, `/` = 200; เปิด admin-react ผ่าน short host แล้ว redirect ไป FQDN ตาม PLAN-140; default ภาษาไทย, สลับ EN แล้วข้อความเปลี่ยนใน Dashboard, `/assignments`, `/courses`, `/users`, `/system-config`, `/reports/compliance`, `/notifications`; refresh แล้วยังจำ EN, สลับกลับไทยได้, console 0 errors ทุกหน้าที่ตรวจ
