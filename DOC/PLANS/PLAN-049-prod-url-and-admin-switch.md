# PLAN-049: ปรับ prod — student ที่ /iLearn (root) + ปุ่มสลับ Admin 2 เวอร์ชัน

- **Status:** DONE -> VERIFIED (reconciled 2026-07-17)
- **Assigned:** Part A → GitHub Copilot (GPT, IIS infra) · Part B → Antigravity (Gemini, UI) _(ผู้ใช้ route ได้)_
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-02
- **อ้างอิง:** [PLAN-046](PLAN-046-deploy-prod-inplace.md)

> 2 คำขอจากผู้ใช้: (A) student portal จาก `/iLearn/student` → **`/iLearn` (root)**; (B) admin 2 เวอร์ชัน (MVC `/iLearn/admin` ↔ React `/iLearn/admin-react`) มีปุ่มกดสลับหากันได้

---

## Part A — ย้าย student จาก `/iLearn/student` → `/iLearn` (root)

**โครงเป้าหมายบน prod:**
| path | แอป |
|---|---|
| `/iLearn` | **iLearn.User (student)** ← ย้ายมาเป็น root |
| `/iLearn/admin` | iLearn.Admin (MVC) |
| `/iLearn/admin-react` | iLearn.Admin.React (SPA) |
| `/iLearn/Service` | iLearn.API |
| `/iLearn/Courses` | vdir (SCORM content) |

**ข้อดี:** `iLearn.User` **ไม่มี hardcode `/student`** (default route `{controller=Home}/{action=Index}`, asset ใช้ `Context.Request.PathBase`) → **ไม่ต้องแก้โค้ด** แค่ปรับ IIS + deploy target

- [ ] แปลง IIS application `/iLearn` (ปัจจุบันเป็น vdir/container ชี้ `\\ap-ntc2137-prwb\wwwroot\iLearn`) ให้เป็น **ASP.NET Core app** ของ iLearn.User: app pool `iLearnStudent`, physical `\\ap-ntc2137-prwb\wwwroot\iLearn` (root)
- [ ] deploy `iLearn.User` ไปที่ **root** `\\ap-ntc2137-prwb\wwwroot\iLearn` (แทน `\iLearn\student`) — ปรับ prod wrapper `-DeployRoot \\ap-ntc2137-prwb\wwwroot\iLearn`
- [ ] คง sub-apps เดิม (`/iLearn/admin`, `/iLearn/admin-react`, `/iLearn/Service`, `/iLearn/Courses`) เป็น nested applications ใต้ root — **IIS route sub-app ก่อน parent** จึงไม่ชนกับ routing ของ User
- [ ] ลบ/park application `/iLearn/student` เดิม
- [ ] (optional) redirect `/iLearn/student` → `/iLearn` กัน bookmark เก่าพัง
- [ ] **ระวัง (nested ASP.NET Core apps):** web.config ของ root (User) กับ sub-apps อาจ inherit กันงอแง → **ทดสอบทุก sub-app ตอบ 200 หลัง restructure** (admin/admin-react/Service/Courses)
- [ ] **ระวัง LearnerProxy:** pathbase เปลี่ยน `/iLearn/student` → `/iLearn` → signature path เปลี่ยน; User sign + API validate ใช้ path จริง ควร adapt เอง แต่ **ต้อง E2E learner login + play ยืนยัน**

**Verify A:** `https://ap-ntc2137-prwb/iLearn` = student portal (Home/Index); learner login + เล่นคอร์สได้; sub-apps ทั้งหมดยัง 200

---

## Part B — ปุ่มสลับ Admin (MVC ↔ React)

**หลักการ:** derive URL อีกเวอร์ชันจาก base path ปัจจุบัน → **env-aware อัตโนมัติ** (prod `/iLearn/*`, QA `/iLearnNew/*`, dev localhost) ไม่ต้องเพิ่ม env var/config ใหม่

### B1. React admin → ปุ่มไป MVC
- [ ] `src/config/appConfig.ts`: เพิ่ม `legacyAdminUrl` = `appBasePath` แทน suffix `/admin-react` → `/admin`
  (เช่น `appBasePath.replace(/\/admin-react\/?$/, '/admin')`; เผื่อ dev/ไม่ match → ปล่อยว่าง/ซ่อนปุ่ม)
- [ ] `src/components/layout/Header.tsx`: เพิ่มปุ่ม "เวอร์ชันเดิม (MVC)" → `legacyAdminUrl` (ถ้ามีค่า) — ใช้ **`AppButton`** ตาม UI conventions ใน [README](../../iLearn.Admin.React/README.md) (อย่า hardcode `<button>`/สี)

### B2. MVC admin → ปุ่มไป React
- [ ] `iLearn.Admin/Views/Shared/_DevExtremeLayout.cshtml`: เพิ่มปุ่ม/ลิงก์ "เวอร์ชันใหม่ (React)" ที่ header → href = `@($"{Context.Request.PathBase}-react/")`
  (PathBase ของ MVC = `/iLearn/admin` → ได้ `/iLearn/admin-react/`; QA `/iLearnNew/admin` → `/iLearnNew/admin-react/`)
- [ ] วางให้กลมกลืน layout เดิม (DevExtreme) — MVC เป็น "อย่าแก้เว้นแต่ถูกสั่ง" แต่งานนี้ผู้ใช้สั่งชัด

**Verify B:**
- [ ] `npm run lint && npm run build` (React) ผ่าน · `dotnet build iLearn.Admin` ผ่าน
- [ ] บน prod: ปุ่มใน React พาไป `/iLearn/admin`, ปุ่มใน MVC พาไป `/iLearn/admin-react` — กดสลับไป-กลับได้จริง

---

## Constraints
- ❌ ห้ามแก้ business logic / API contract — งานนี้แค่ IIS routing (A) + ปุ่มนำทาง (B)
- ❌ Part B: ห้าม hardcode URL prod ตรง ๆ (ต้อง derive จาก base path/PathBase ให้ QA/dev ใช้ได้ด้วย)
- ✅ Part A ไม่ต้องแก้โค้ด iLearn.User (IIS/deploy เท่านั้น)

## Decision points (ผู้ใช้)
1. Part A: ต้องการ redirect `/iLearn/student` → `/iLearn` ไหม (กัน bookmark เก่า) หรือปล่อย 404
2. ข้อความบนปุ่ม: "เวอร์ชันเดิม/ใหม่", "Classic/New", หรืออื่น
3. ทำ Part A + B พร้อมกัน หรือ B ก่อน (B ไม่กระทบ IIS, เสี่ยงต่ำกว่า)

## Verification commands
```powershell
npm run lint ; npm run build                      # iLearn.Admin.React
dotnet build iLearn.Admin/iLearn.Admin.csproj -c Release --artifacts-path artifacts/verify-admin
dotnet build iLearn.Tests -o artifacts\verify-test ; dotnet test artifacts\verify-test\iLearn.Tests.dll
```

## Implementer Notes

- 2026-07-03 Part B เสร็จ: React Header มีลิงก์ไป MVC และ MVC layout มีลิงก์กลับ React โดย derive จาก PathBase จึงใช้ได้กับ QA, production และ development โดยไม่ hardcode URL
- 2026-07-03 Part A เสร็จ: ย้าย learner portal ไปที่ `/iLearn` root, รักษา `/Service`, `/admin`, `/admin-react` และ `/Courses` เป็น nested applications; smoke test ครบทุก endpoint และ SCORM content ผ่าน
- 2026-07-06 PLAN-051 ลบ IIS application `/iLearn/student` ที่ค้างอยู่และวาง permanent redirect `/iLearn/student` -> `/iLearn`; ปิด residual 500.35 จากการย้ายครั้งแรก
- ไม่มีงาน implementation หรือ IIS restructure ค้างจากแผนนี้; checklist ด้านบนเก็บไว้เป็น historical runbook
