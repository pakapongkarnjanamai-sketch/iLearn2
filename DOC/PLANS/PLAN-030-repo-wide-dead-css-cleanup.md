# PLAN-030: ลบ dead CSS/utility ที่เหลือทั้งโปรเจกต์ (หลัง PLAN-029)

- **Status:** DONE
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Low
- **Estimated scope:** 1 ไฟล์หลัก (`iLearn.Admin/wwwroot/css/admin-wizard.css`) + optional review notes สำหรับ `admin-minimal.css`

## Problem

หลังทำ `PLAN-029` (React `index.css`) ยังพบ dead CSS ฝั่ง Legacy Admin (`iLearn.Admin`) จากการสแกนทั้งโปรเจกต์:

**High-confidence dead (ไม่พบ reference นอกไฟล์ CSS ที่ define):**
| รายการ | ไฟล์ | ชนิด |
|---|---|---|
| `.admin-sidebar-card` | `iLearn.Admin/wwwroot/css/admin-wizard.css` | class |
| `.admin-inline-actions` | `iLearn.Admin/wwwroot/css/admin-wizard.css` | class |
| `.admin-form-card--sm` | `iLearn.Admin/wwwroot/css/admin-wizard.css` | class |
| `.admin-form-card--md` | `iLearn.Admin/wwwroot/css/admin-wizard.css` | class |
| `.admin-form-card--lg` | `iLearn.Admin/wwwroot/css/admin-wizard.css` | class |

**Medium-confidence candidates (ยังไม่ลบในรอบนี้):**
- `iLearn.Admin/wwwroot/css/admin-minimal.css`: `.u-fs-20`, `.u-letter-spacing-05`, `.u-minw-44`, `.u-maxh-150`
- `iLearn.Admin/wwwroot/css/admin-wizard.css`: `.admin-summary-grid--cols-2`, `.admin-summary-grid--cols-3`

หมายเหตุ: ไม่พบ keyframe dead เพิ่มเติมในรอบ scan นี้

## Scope (ทำแค่นี้)

1. ยืนยันซ้ำด้วย `rg` ก่อนลบว่า 5 class ในกลุ่ม High-confidence ไม่มีการใช้งานใน `iLearn.Admin/Views/**/*.cshtml`, `iLearn.Admin/wwwroot/js/**/*.js`, `iLearn.Admin/**/*.cs`
2. ลบ 5 class ข้างต้นจาก `iLearn.Admin/wwwroot/css/admin-wizard.css`
3. ตรวจซ้ำหลังลบ:
   - class ที่ลบไม่ปรากฏในไฟล์ CSS อีก
   - class ที่ลบไม่มี reference ในฝั่ง View/JS/C#
4. เก็บบันทึกสถานะ Medium-confidence ไว้ใน Implementer Notes (ยืนยันว่า “ยังไม่ลบในรอบนี้”)

## Out of scope (ห้ามแตะ)

- ห้ามลบ class กลุ่ม medium-confidence ในแผนนี้
- ห้ามแก้ token/theme ใน `admin-tokens.css`
- ห้ามจัดระเบียบ/reformat ใหญ่ทั้งไฟล์นอกบริเวณ class ที่ลบ
- ห้ามแตะ React CSS (`iLearn.Admin.React/src/index.css`) เพิ่มเติมในแผนนี้

## Acceptance criteria

- [x] ลบ class High-confidence ครบ 5 รายการจาก `admin-wizard.css`
- [x] grep ยืนยัน class ที่ลบไม่มี reference ใน Legacy Admin view/js/c#
- [x] ไม่กระทบ class ที่ใช้งานจริงใน wizard ปัจจุบัน
- [x] บันทึก medium-confidence list ใน Implementer Notes ว่ายังไม่ลบในรอบนี้
- [x] `dotnet build iLearn.Admin/iLearn.Admin.csproj` ผ่าน

## Verification

```powershell
# จาก root repo
rg -n "admin-sidebar-card|admin-inline-actions|admin-form-card--sm|admin-form-card--md|admin-form-card--lg" iLearn.Admin

dotnet build iLearn.Admin/iLearn.Admin.csproj
```

Manual smoke (ขั้นต่ำ):
- เปิดหน้า wizard หลักที่ยังใช้งาน เช่น `Assignments/BulkAssign`, `Courses/VersionForm`, `LearnerGroups/AddMembers` ดู layout ไม่เพี้ยน

## Implementer Notes

- ลบ selectors ตามแผนจาก `iLearn.Admin/wwwroot/css/admin-wizard.css` ครบ 5 รายการ:
   - `.admin-sidebar-card`
   - `.admin-inline-actions`
   - `.admin-form-card--sm`
   - `.admin-form-card--md`
   - `.admin-form-card--lg`
- ยืนยันก่อนลบ: `rg -n "admin-sidebar-card|admin-inline-actions|admin-form-card--sm|admin-form-card--md|admin-form-card--lg" iLearn.Admin` พบเฉพาะตำแหน่ง define ใน `admin-wizard.css`
- ยืนยันหลังลบ: คำสั่ง `rg` เดิมไม่พบผลลัพธ์แล้ว
- Medium-confidence list ที่ **ยังไม่ลบในรอบนี้**:
   - `iLearn.Admin/wwwroot/css/admin-minimal.css`: `.u-fs-20`, `.u-letter-spacing-05`, `.u-minw-44`, `.u-maxh-150`
   - `iLearn.Admin/wwwroot/css/admin-wizard.css`: `.admin-summary-grid--cols-2`, `.admin-summary-grid--cols-3`
- Verification:
   - `dotnet build iLearn.Admin/iLearn.Admin.csproj` ผ่าน (มี warning เดิมของโปรเจกต์, ไม่มี error)
