# PLAN-013: ปิดช่องโหว่ FileStoragesCRUD ที่ดัมพ์ SCORM blob ทั้งหมด

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: ไฟล์ถูกลบ, grep FileStoragesCRUD = 0 refs, build/test 116 ผ่าน, IGenericRepository<FileStorage> ยังใช้ใน ContentItems ปกติ)
- **Assigned:** GPT
- **Priority:** High
- **Estimated scope:** 1 ไฟล์ (ลบ `FileStoragesCRUDController.cs`) — backend ล้วน

## Problem

`iLearn.API/Controllers/Base/FileStoragesCRUDController.cs` สืบทอด `GenericController<FileStorage>` **โดยไม่ override method ใด ๆ** — จึง expose endpoint CRUD อัตโนมัติที่ `api/admin/FileStoragesCRUD/*` (policy `AdminOnly` ตาม base)

ปัญหาร้ายแรงที่ `GET api/admin/FileStoragesCRUD/Get`:
- `GenericController.Get` เรียก `_repository.GetAllAsync()` ซึ่ง = `_dbSet.ToListAsync()` (ยืนยันใน `iLearn.Infrastructure/Repositories/GenericRepository.cs:27-30`)
- `FileStorage` มีคอลัมน์ `Data` เป็น `byte[]` เก็บ **ZIP ของ SCORM ทั้งก้อน** (`iLearn.Domain/Entities/FileStorage.cs`)
- ผลคือโหลด **ทุกแถวพร้อม blob ทั้งหมดเข้า memory** แล้ว serialize เป็น JSON (base64) — memory spike / timeout / DoS เชิงปฏิบัติ จาก request เดียว
- ขัด CLAUDE.md โดยตรง: "`FileStorage.Data` เก็บ SCORM ZIP ทั้งก้อนเป็น `byte[]` ใน DB — **ห้าม Include/โหลด entity นี้ใน query รายการเด็ดขาด** ใช้ `ContentItem.CachedFileLength` แทน"

**ตรวจแล้วว่า controller นี้ไม่มีใครใช้:** `grep "FileStoragesCRUD\|FileStorages"` ใน `iLearn.Admin.React/src` และ `iLearn.Admin/wwwroot` = 0 ที่ — ฝั่ง client ไม่เรียก endpoint นี้เลย การเข้าถึงไฟล์จริงทำผ่าน `ContentItemsController` (เช่น `ContentItems/{id}/content`) ไม่ใช่ผ่าน CRUD นี้

## Scope (ทำแค่นี้)

1. **ลบไฟล์ `iLearn.API/Controllers/Base/FileStoragesCRUDController.cs` ทั้งไฟล์**
2. ตรวจว่าไม่มีที่อื่นอ้างถึง class `FileStoragesCRUDController` (grep ทั้ง solution) — คาดว่าไม่มี เพราะ controller ถูก auto-discover ไม่ต้อง register มือ
3. ยืนยันว่า `IGenericRepository<FileStorage>` ยังถูก register/ใช้งานปกติ (ContentItemsController ใช้ผ่าน repository นี้ — **ห้ามแตะ** ลบแค่ controller ไม่ใช่ repository/entity)

## Out of scope (ห้ามแตะ)

- ห้ามแตะ `FileStorage` entity, `IGenericRepository<FileStorage>`, หรือการ register repository
- ห้ามแตะ `ContentItemsController` หรือ path เสิร์ฟไฟล์จริง (`ContentItems/{id}/content`)
- ห้ามแตะ `GenericController<T>` (base) — controller อื่นยังใช้อยู่
- ถ้าพบว่ามีโค้ด/เทสอ้างถึง `FileStoragesCRUD` จริง (ไม่คาดว่ามี) ให้หยุดและจดใน Implementer Notes แทนการลบทั้งดุ้น

## Acceptance criteria

- [x] ไฟล์ `FileStoragesCRUDController.cs` ถูกลบ
- [x] `grep -r FileStoragesCRUD` ทั้ง solution = 0 ที่ (ไม่มี reference ค้าง)
- [x] `dotnet build` ผ่าน (ไม่มี reference พัง)
- [x] `dotnet test` ผ่านครบเท่าเดิม (115/115)
- [x] endpoint อื่นของ FileStorage ที่ใช้จริงผ่าน ContentItems ยังทำงาน (build ผ่าน = สัญญาณพอ; ถ้ารัน API ได้ลองเปิด content library)

## Verification

```powershell
# Backend — ถ้า API รันใน VS อยู่ bin ถูกล็อก ให้ build ออก artifacts
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
```

## Implementer Notes

- ลบไฟล์ `iLearn.API/Controllers/Base/FileStoragesCRUDController.cs` ตาม scope โดยไม่แตะ `FileStorage` entity, `IGenericRepository<FileStorage>`, `GenericController<T>` หรือ logic ของ `ContentItemsController`
- ตรวจ reference ซ้ำหลังลบ:
	- `FileStoragesCRUD`/`FileStoragesCRUDController` ในโค้ด `.cs` = 0
	- ไม่พบไฟล์ `FileStoragesCRUDController.cs` แล้ว
	- `IGenericRepository<FileStorage>` ยังถูกใช้งานตามเดิมใน `ContentItemsController`, `ContentItemsCRUDController`, และ services ฝั่ง Application (Course/CourseVersion/ContentPublication)
- Verification ที่รันแล้ว:
	- `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน
	- `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (115/115)
	- ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว
