# API Analysis: iLearn.API (Current)

วิเคราะห์ backend (.NET 9, Clean Architecture) ฉบับอัปเดตล่าสุด เพื่อใช้อ้างอิงโครงสร้าง API, รูปแบบ endpoint, และประเด็นคงค้างที่ยังควรติดตาม

> อัปเดตล่าสุด: 2026-06-15 (sync กับ `DOC/API-ENDPOINT-INVENTORY.md`)

---

## 1. Architecture Snapshot

| Layer | Project | Responsibility |
|---|---|---|
| Presentation | `iLearn.API` | Controllers, middleware, auth, SignalR, composition root |
| Application | `iLearn.Application` | Use cases/services, DTOs, interfaces/contracts |
| Domain | `iLearn.Domain` | Entities/enums/shared domain model |
| Infrastructure | `iLearn.Infrastructure` | EF Core, repositories, external integrations |

Host setup ยังคงแข็งแรงในเชิงความปลอดภัยและ maintainability:
- `FallbackPolicy = DefaultPolicy` (secure-by-default)
- `ValidateRequiredSecrets()` (fail-fast)
- `GlobalExceptionMiddleware` + ProblemDetails
- policy validation ตอน startup (`ValidateExplicitControllerAuthorizationPolicies`)

---

## 2. Authentication / Authorization Model

Authentication: Windows Authentication (Negotiate) + claims enrichment middleware

Policy หลัก:
- `AdminOnly`
- `SuperAdminOnly`
- `ManagerOrAbove`
- `UserOrAbove`
- `DomainUser`

Learner proxy endpoints ยังคงใช้ HMAC verification ผ่าน resolver ตาม pattern เดิม (timestamp window + fixed-time signature comparison)

---

## 3. Endpoint Surface (Current)

อิงจาก `DOC/API-ENDPOINT-INVENTORY.md`:
- Controllers with endpoints: 30
- Total endpoints: 165
- SignalR hubs: 1 (`/hubs/admin-activity`)

Route family ที่ใช้งานจริง:
- `api/[controller]`
- `api/admin/[controller]`
- inherited `api/admin/[controller]` จาก `GenericController<T>`
- `api/admin/session` (special case)

หมายเหตุสำคัญ:
- `FileStoragesCRUDController` ถูกลบแล้ว (ตาม PLAN-013) จึงไม่อยู่ใน surface ปัจจุบัน

---

## 4. API Styles ที่ใช้ร่วมกัน

1. DevExtreme CRUD style (`Get`/`Post`/`Put`/`Delete`, `DataSourceLoadOptions`, form-encoded key/values)
2. REST-ish style (`api/[controller]`, item routes, sub-resource routes)
3. Action-oriented endpoints (domain commands เช่น validate, bulk operations, dashboard summaries)
4. Mixed response contracts (typed DTO + anonymous object ยังปะปน)

Frontend ปัจจุบันจึงต้องรองรับหลายรูปแบบ data source (`createAdminDataSource`, `createRestDataSource`, และ page-specific loaders)

---

## 5. Current Findings (Prioritized)

### 5.1 🟠 MEDIUM — API style fragmentation สูง
มีหลาย naming convention และหลาย route shape ในระบบเดียวกัน ส่งผลให้ onboarding และ contract governance ยากขึ้น

### 5.2 🟠 MEDIUM — Controller ขนาดใหญ่ยังมีอยู่
Controllers หลักบางตัวยังค่อนข้างใหญ่ (เช่น Assignments, ContentItems, Courses, Enrollments) แม้ refactor บางส่วนเริ่มเดินแล้ว

### 5.3 🟡 LOW — Response contract ยังไม่ uniform
มีทั้ง typed DTO และ anonymous object ปนกัน ทำให้ frontend contract sync ต้องใช้วินัยสูง

### 5.4 ✅ Resolved Since Initial Audit
- ความเสี่ยง `FileStoragesCRUDController` ดัมพ์ blob ถูกปิดแล้ว
- Learning Logs route/menu ฝั่ง UI ถูกทำให้สอดคล้อง SuperAdmin policy แล้ว
- SuperAdmin สามารถกำหนด division ในงาน Learner Group Category ตามแผนที่ implement แล้ว

---

## 6. Recommended Next Steps

1. กำหนด API style guide กลาง (route naming + payload + envelope + error shape)
2. เดินหน้าแผนทยอย refactor controller ใหญ่แบบทีละโมดูล
3. เพิ่ม coverage ฝั่ง contract/integration สำหรับ endpoint ที่ frontend ใช้บ่อย
4. ใช้ `DOC/API-ENDPOINT-INVENTORY.md` เป็น source of truth สำหรับ endpoint catalog

---

## 7. References

- `DOC/API-ENDPOINT-INVENTORY.md`
- `DOC/division_isolation_analysis.md`
- `iLearn.API/Program.cs`
- `iLearn.API/Controllers/**`
