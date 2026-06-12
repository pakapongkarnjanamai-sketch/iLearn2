# PLAN-009: Refine Detail Pages Design - Remove Header & Back Button, Restrict Sidebar to Actions, Support Tabs & Overview

- **Status:** DONE
- **Assigned:** GPT
- **Priority:** High
- **Estimated scope:** แก้ไข 2 shared components (`src/components/ui/detail/index.tsx`, `src/components/ui/ControlsSidebar.tsx`) + ปรับปรุง 2 หน้าอ้างอิงเดิม + กำหนดเกณฑ์สำหรับย้ายหน้าเหลือใน PLAN-008

## Problem

หน้าแสดงรายละเอียด (Detail Pages) ได้รับสเปคดีไซน์ใหม่เพื่อความเป็นระเบียบและลดความซับซ้อน:
1. **ยกเลิก Page Header** (eyebrow + h1 title) ที่เคยอยู่ด้านบนของกริดออก เพื่อหลีกเลี่ยงความซ้ำซ้อนและสะอาดตาขึ้น
2. **ยกเลิกปุ่ม Back** ลิงก์สีเทาด้านล่างสุดของ `ControlsSidebar` ทุกหน้า
3. **จำกัดหน้าที่ของ ControlsSidebar** ให้มีเฉพาะปุ่มคำสั่งการดำเนินการ (`ControlAction`) เท่านั้น ห้ามนำข้อมูลรายละเอียด ข้อมูลชี้วัด หรือคุณสมบัติสรุปที่ไม่ได้คลิกสั่งงาน (Metadata) ไปฝั่งในแถบนี้อีกต่อไป
4. **ย้ายข้อมูลคุณสมบัติ** ทั้งหมดในข้อ 3 ไปเรนเดอร์รวมกันในการ์ด **Overview** ของแท็บแรกแทน
5. หน้าที่มีข้อมูลหลายส่วน เช่น Assignment หรือ Learner Group ต้องแสดงผลในรูปแบบ **Tabs** (Overview + ส่วนข้อมูลย่อยแยกแท็บกัน)

## Scope (ทำแค่นี้)

### 1. แก้ไข Shared Components

#### [MODIFY] [src/components/ui/detail/index.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/detail/index.tsx)
- ลบฟังก์ชันและ type ของ `DetailPageHeader` ออกทั้งหมด

#### [MODIFY] [src/components/ui/ControlsSidebar.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx)
- ลบพร็อพ `backTo` และ `backLabel` ออกจาก `ControlsSidebarProps`
- ลบการเรนเดอร์ปุ่ม Back `{backTo && ( ... )}` ที่อยู่ด้านล่างสุดของการการ์ด

### 2. ปรับปรุงหน้าอ้างอิงชุดแรก (ที่ถูก migrate แล้ว)

#### [MODIFY] [UserDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserDetailPage.tsx)
- ลบการเรียกใช้งานและการนำเข้า `<DetailPageHeader ... />`
- ลบพร็อพ `backTo` ออกจาก `<ControlsSidebar>`

#### [MODIFY] [ContentItemDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx)
- ลบการเรียกใช้งานและการนำเข้า `<DetailPageHeader ... />`
- ลบพร็อพ `backTo` ออกจาก `<ControlsSidebar>`

### 3. ปรับปรุงข้อกำหนดสำหรับหน้าที่จะ migrate ใน PLAN-008

#### [MODIFY] [AssignmentDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx)
- ลบแถบ KPI Grid (`auto-cols-fr grid-flow-col`) ด้านบนออกทั้งหมด
- ย้ายข้อมูลกำหนดเวลา (Start Date, Due Date, Learner Group) ออกจาก `ControlsSidebar` (ลบ `<ControlsDivider>`)
- นำระบบสลับแท็บมาใช้: **Overview**, **Courses**, **Learners**
- แท็บ **Overview**: แสดง `DetailCard` (ข้อมูลสรุป) ประกอบด้วย `FactGrid`/`Fact` แสดงผล Metrics เดิม (Learners, Completed, Completion Rate, Status) ร่วมกับข้อมูลกำหนดการเรียนที่ย้ายมาจาก sidebar
- แท็บ **Courses**: แสดงตาราง/รายการหลักสูตร
- แท็บ **Learners**: แสดงตารางผู้ลงทะเบียนเรียน

#### [MODIFY] [LearnerGroupDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx)
- ย้ายข้อมูล LMS Category และ Owner / Creator ออกจาก `ControlsSidebar` (ลบ `<ControlsDivider>`)
- นำระบบสลับแท็บมาใช้: **Overview**, **Members**
- แท็บ **Overview**: แสดงข้อมูล LMS Category และ Owner / Creator ร่วมกันในการ์ดสรุปคุณสมบัติกลุ่ม
- แท็บ **Members**: แสดงตารางรายชื่อสมาชิกเดิม

#### `CourseDetailPage.tsx`, `MasterDataDetailPage.tsx`, `LearnerProfilePage.tsx`
- ย้ายข้อมูลตามเกณฑ์ดีไซน์ใหม่ (ไม่มี Header, ไม่มีปุ่ม Back ใน Sidebar)

## Out of scope (ห้ามแตะ)
- ห้ามแก้ CSS หรือ layout ส่วนอื่น ๆ ของระบบ
- ห้ามแตะหน้า list / editor / wizard อื่นนอกจาก detail

## Acceptance criteria
- [x] grep `DetailPageHeader` ใน `src/` เป็น 0 (ลบการนำเข้าและคอมโพเนนต์ออกหมด)
- [x] grep `backTo` ใน `src/components/ui/ControlsSidebar.tsx` เป็น 0 (ลบปุ่ม Back ใน sidebar สำเร็จ)
- [x] `AssignmentDetailPage.tsx` และ `LearnerGroupDetailPage.tsx` ใช้ระบบแท็บ และไม่มี metadata แสดงผลค้างอยู่ในแถบข้างขวา
- [x] ฟังก์ชันดึงข้อมูล คลีนอัพ และบันทึกของทุกโมดูลทำงานปกติ

## Verification
```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes
- ลบ `DetailPageHeader` ออกจาก shared detail primitives (`src/components/ui/detail/index.tsx`) และลบการเรียกใช้งานที่หน้า detail
- ปรับ `ControlsSidebar` ให้เป็น action-only โดยตัด `backTo`/`backLabel` และลบปุ่ม Back รวมถึงลบ `ControlsDivider` helper เพื่อกัน metadata หลุดเข้า sidebar
- ปรับ `UserDetailPage.tsx` และ `ContentItemDetailPage.tsx` ให้ไม่มีการเรียกใช้ Page Header และไม่มี back props
- รีแฟกเตอร์ `AssignmentDetailPage.tsx` เป็นแท็บ `Overview/Courses/Learners` พร้อมย้าย metrics + schedule (Start Date, Due Date, Learner Group) มาอยู่แท็บ Overview และลบ KPI strip ด้านบน
- รีแฟกเตอร์ `LearnerGroupDetailPage.tsx` เป็นแท็บ `Overview/Members` พร้อมย้าย `LMS Category` และ `Owner / Creator` มาแสดงใน Overview และคงตารางสมาชิกไว้ที่แท็บ Members
- ยืนยันการพอร์ตหน้าที่เหลือใน PLAN-008 ตามเกณฑ์ดีไซน์ใหม่ครบแล้ว (สถานะ PLAN-008 = DONE)
- Verification ผ่าน: `npm run lint` (0 errors, warnings baseline), `npm run build` ผ่าน
