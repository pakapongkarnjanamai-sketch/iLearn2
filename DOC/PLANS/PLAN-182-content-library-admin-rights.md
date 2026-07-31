# PLAN-182: Content Library Admin rights parity

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

ผู้ใช้พบปัญหาสิทธิบน production หน้า `/iLearn/admin-react/content-library/1747` และระบุว่า `Admin` ควรมีสิทธิเท่ากับ `SuperAdmin` สำหรับหน้านี้

## Root Cause

React detail page เปิดให้ Admin เข้าได้ แต่ action สำคัญบนหน้า (`Edit Metadata`, `Publish/Unpublish`, `Delete`) ถูกซ่อนด้วย `isSuperAdmin`

Backend ก็ล็อก mutation endpoint ที่ action เหล่านี้เรียกไว้เป็น `SuperAdminOnly` บางส่วน ได้แก่ content upload/publish/unpublish/delete และ `ContentItemsCRUD` create/update/delete

## Scope

- Frontend content library routes and detail/list action visibility
- Backend content item normal mutation endpoints
- Regression tests for authorization policy attributes

## Changes

- Removed `RequireRole superAdminOnly` from `/content-library/new` and `/content-library/:id/edit`
- Upload SCORM button on content library list now appears for Admin users
- Content item detail actions now render for Admin users: edit metadata, publish/unpublish, delete
- Changed normal content item mutation policies from `SuperAdminOnly` to `AdminOnly`:
  - `ContentItems/upload`
  - `ContentItems/SetPublic`
  - `ContentItems/Unpublish`
  - `ContentItems/{id}` DELETE
  - `admin/ContentItemsCRUD/Post`
  - `admin/ContentItemsCRUD/Put`
  - `admin/ContentItemsCRUD/Delete`
- Left bulk/maintenance endpoints under `ContentItems/Admin/*` as `SuperAdminOnly`

## Contract Changes

API shape / DTO / DB unchanged. Authorization semantics changed for normal Content Library content-item mutations: `Admin` and `SuperAdmin` are now both allowed.

## Verification

- `dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\content-admin-rights --filter "FullyQualifiedName~ContentItems"` ✓ 10/10
- `npm run lint` ✓
- `npm run build` ✓ (existing Vite chunk-size warning)
- QA API deploy: `20260731113853`; QA React deploy: `index-YLZeN2CC.js`, robocopy 3
- PROD API deploy: `20260731114031`; PROD React deploy: `index-YLZeN2CC.js`, robocopy 3
- PROD smoke `/iLearn/admin-react/content-library/1747` = 200 and serves `index-YLZeN2CC.js`
- PROD smoke `/iLearn/Service/api/admin/ContentItemsCRUD/Get/1747` = 200 JSON

## Implementer Notes

- Production API deploy printed the usual remote app-pool topology audit warning (`Access is denied`) but completed and post-deploy health check passed HTTP 401 as expected for unauthenticated Windows-auth endpoint
- Could not impersonate a non-SuperAdmin Admin account in this session; policy coverage is validated by reflection tests and frontend route/action removal of the SuperAdmin guard