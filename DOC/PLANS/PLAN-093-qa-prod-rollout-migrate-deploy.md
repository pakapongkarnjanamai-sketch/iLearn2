# PLAN-093: Rollout — รัน migration + deploy ขึ้น QA แล้วต่อ PROD (Notifications P1+P2, Report Hub, PLAN-092 index fix)

- **Status:** READY (Phase 1 เริ่มได้เมื่อผ่าน Gate 0)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-17

> ผู้ใช้สั่ง (2026-07-17): ให้ Copilot รัน migration บน QA และ PROD + deploy บน QA และ PROD

---

## Gate 0 — เงื่อนไขก่อนเริ่ม (ยังไม่ผ่าน ณ ตอนเขียนแผน)

- [ ] **PLAN-090/091 ต้องผ่านรีวิว (Claude Code) และถูก commit เข้า master ก่อน** — ตอนนี้งานทั้งสองเสร็จแต่ยังอยู่ใน working tree ไม่ถูก commit ⇒ ถ้า deploy ตอนนี้จะได้ build ที่ไม่มี Notifications P2
- เริ่ม Phase 1 ได้เมื่อ: AGENT_LOG มี entry รีวิว 090/091 + `git log` มี commit งานทั้งสอง
- Build ที่จะ deploy ครั้งนี้รวม (นับจาก build ปัจจุบันบนแต่ละ server): **QA** ได้ PLAN-090/091/092 (+sidebar 3ed57f7); **PROD** ได้ทั้งหมดตั้งแต่ PLAN-084 เป็นต้นมา (1GB SCORM, Report Hub 086/087, Notifications 088/089/090/091, index fix 092) — **PROD เป็น jump ใหญ่ ต้องผ่าน QA ก่อนเท่านั้น**

## กติกาความปลอดภัย (บังคับทุก Phase)

1. **ลำดับตายตัว: migrate DB ก่อน → ค่อย deploy app** — migration ทุกตัวเป็น additive (เพิ่มตาราง/คอลัมน์/สลับ index) build เก่าอยู่กับ schema ใหม่ได้ แต่ build ใหม่อยู่กับ schema เก่าไม่ได้ (บทเรียน bell 500 = deploy โดยไม่ migrate)
2. Connection string ดูจาก `appsettings.json` (QA = `AP-NTC2138-QADB`) / `appsettings.Production.json` (PROD = `AP-NTC2139-COSS`) — **ห้าม copy รหัสผ่านลงไฟล์แผน/log**
3. ห้ามเริ่ม Phase 2 (PROD) จนกว่า **ผู้ใช้ยืนยันผล QA ในแชท** — ไม่มีข้อยกเว้น
4. Rollback: deploy script auto-rollback web.config เมื่อ health fail อยู่แล้ว; migration ไม่ต้อง revert (additive — build เก่าไม่รู้จักของใหม่ก็ทำงานต่อได้)
5. แตะ PROD ช่วงคนใช้น้อย + แจ้งผู้ใช้ก่อนกด (app restart ชั่วครู่)

## Phase 1 — QA

```powershell
# 1) sync master ล่าสุด (ต้องมี commit 090/091 แล้วตาม Gate 0)
git pull

# 2) ดู migration ค้างจริง (คาด: AddNotifications + SoftDeleteFilteredUniqueIndexes)
dotnet ef migrations list --project iLearn.Infrastructure --startup-project iLearn.API --connection "<QA conn จาก appsettings.json>"

# 3) MIGRATE ก่อน
dotnet ef database update --project iLearn.Infrastructure --startup-project iLearn.API --connection "<QA conn>"

# 4) ยืนยัน: ข้อ 2 ซ้ำ → ไม่มี (Pending) เหลือ; bell บน QA ควรหาย 500 ทันทีแม้ยังไม่ deploy (build ปัจจุบันมี controller แล้ว)

# 5) DEPLOY
./tools/deploy-api.ps1            # API → QA (side-by-side + health + Sync-RequestLimits จะพา 1GB limit ขึ้น web.config อัตโนมัติ)
./tools/deploy-admin-react.ps1    # React admin → QA
```

### Smoke QA (รวมของค้างจากทุกแผนที่รอ live test — โอกาสปิดหนี้ทั้งหมด)

| # | ทดสอบ | ปิดหนี้ของ |
|---|---|---|
| 1 | bell ไม่ 500, badge ถูก, dropdown **อยู่หน้า grid** | 088/089 + z-index fix |
| 2 | `/assignments/306` → Add Courses → "Software back up (Re.3)" → **สำเร็จ** | **PLAN-092 (เคสที่ผู้ใช้เจอ)** |
| 3 | `/notifications` หน้าเต็ม: Load more / All-Unread / Mark all read → badge sync | 091 |
| 4 | Restart app pool → digest ออก **ครั้งเดียว** (ถ้ามีของเข้าเกณฑ์); restart ซ้ำ → ไม่ซ้ำ | 090 (idempotency) |
| 5 | `/reports` ทั้ง 4 หน้าเปิดได้ ตัวเลขไม่ error — **จุดพิสูจน์ EF SQL translation ที่ค้างจาก 086** โดยเฉพาะ course-summary | 086/087 |
| 6 | อัป SCORM 50MB (KSN.zip) สำเร็จ + เห็น upload progress UI; **ถ้าทำได้: ไฟล์ ~1GB + watch memory w3wp** (เกณฑ์: RAM ไม่พุ่งตามไฟล์) | 084/085 Phase 4 |
| 7 | admin 2 คน: คนที่ 1 อัป SCORM → **เฉพาะคนที่ 1 ได้ notification** ขณะอยู่หน้าอื่นที่ไม่ใช่ Dashboard | 088/089 (per-user targeting) |
| 8 | Dashboard activity feed realtime + จุด connected ปกติ; Network tab เหลือ hub connection **เส้นเดียว** | 091 |

- ผลทุกข้อ (ผ่าน/ไม่ผ่าน + หลักฐานสั้น ๆ) ลง Implementer Notes + AGENT_LOG แล้ว**หยุดรอผู้ใช้ไฟเขียว**
- ข้อไหน fail → หยุด ไม่ไป PROD, log อาการ + ปรึกษาใน plan

## Phase 2 — PROD (หลังผู้ใช้ยืนยันผล QA ในแชทเท่านั้น)

```powershell
# 1) ดู migration ค้างจริง (คาด 3 ตัว: AddStoragePathToFileStorage, AddNotifications, SoftDeleteFilteredUniqueIndexes — ยืนยันก่อนเสมอ)
dotnet ef migrations list --project iLearn.Infrastructure --startup-project iLearn.API --connection "<PROD conn จาก appsettings.Production.json>"

# 2) MIGRATE ก่อน (additive ทั้งหมด — build เก่าที่ยังรันอยู่ไม่กระทบ)
dotnet ef database update --project iLearn.Infrastructure --startup-project iLearn.API --connection "<PROD conn>"

# 3) DEPLOY (แจ้งผู้ใช้ก่อนกด)
./tools/deploy-api-prod.ps1          # Sync-RequestLimits พา maxAllowedContentLength=1GB ขึ้น PROD (ตั้งใจ — PLAN-084)
./tools/deploy-admin-react-prod.ps1
```

- **ไม่ต้อง deploy `iLearn.User` / MVC admin** — ไม่มีการเปลี่ยนแปลงตั้งแต่ rollout ก่อน (งานทั้งหมดอยู่ API + admin-react)

### Smoke PROD (เบา — ห้าม destructive test)

1. `GET /iLearn/Service/api/health` → 200 pass ครบ 3 checks
2. เปิด admin-react: Dashboard โหลด, bell ทำงาน (ไม่ 500), dropdown อยู่หน้า content
3. `/reports` เปิดได้ + compliance โหลดตัวเลขจริง (ครั้งแรกบน PROD data scale — จับเวลา response จดไว้)
4. เปิดหน้า assignment เดิม 1 หน้า → ปกติ
5. อัป SCORM ตัวเล็ก 1 ไฟล์บนคอร์สทดสอบ (ถ้ามีคอร์ส sandbox) → สำเร็จ + ได้ notification

## หลังจบ

- อัปเดตสถานะแผนนี้ + Implementer Notes (ผล smoke ทุกข้อ, เวลา migrate/deploy จริง, ปัญหาที่เจอ)
- ลง AGENT_LOG ตาม format
- ถ้าทุกอย่างผ่าน: แจ้งให้ push commits ขึ้น `nikon/master` ด้วย (ตอนนี้ local ahead หลาย commits)

## Implementer Notes

*(เติมหลังทำเสร็จ)*
