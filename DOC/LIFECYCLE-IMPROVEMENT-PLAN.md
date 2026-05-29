# iLearn Lifecycle Improvement Plan

Last updated: 2026-05-29

## Purpose

ไฟล์นี้สรุป recommendations และแผนปรับปรุงสถาปัตยกรรม lifecycle ของระบบ iLearn2 โดยแยกส่วนที่กำลังดำเนินงาน (Active Roadmap) ออกจากส่วนที่เสร็จสมบูรณ์แล้ว เพื่อช่วยให้ทีมวิศวกรทำงานต่อได้อย่างเป็นระเบียบและปลอดภัย

---

## Active Roadmap (Open Priorities)

### 1. Lock Lifecycle Status Invariants With Focused Tests

**Priority:** High  
**Status:** Open  
**Goal:** ให้ lifecycle rules สำคัญถูกล็อกด้วย tests ป้องกัน regression ก่อนขยาย feature work รอบถัดไป

**Why it matters:**
- พฤติกรรมหลักของระบบได้รับการ centralized ในชั้น domain policy แล้ว แต่บางกฎเชิงธุรกิจ (invariants) ยังพึ่งการอ่านเอกสารและโค้ดเป็นหลัก หากเขียน test ล็อกไว้ จะช่วยให้การดูแลรักษาระบบปลอดภัยสูงสุด

**Implementation Plan:**
1. เพิ่ม focused tests สำหรับ course status transitions และ retire/open edge cases
2. เพิ่ม tests สำหรับ assignment status priority และ `No Learners` baseline behavior
3. เพิ่ม tests สำหรับ content readiness / publish-state invariants และ SCORM precedence ที่ยังเหลือ

---

### 2. Add NoLearners Display Bucket For Assignment Batches

**Priority:** Medium  
**Status:** Open  
**Goal:** ปรับปรุงแผงควบคุมแอดมินให้แยกความแตกต่างของ Assignment ที่พึ่งสร้างแต่ไม่มีนักเรียนออกจากการเรียนที่กำลังดำเนินการจริง

**Why it matters:**
- ปัจจุบัน API คืนสถานะ `InProgress` เมื่อ assignment ไม่มี enrollments ซึ่งไม่ผิดในเชิงสถาปัตยกรรม แต่อาจนำไปสู่ความสับสนเมื่อแอดมินดูประวัติย้อนหลังหรือตรวจการใช้งาน

**Implementation Plan:**
1. กำหนดรูปแบบ client-side display bucket หรือ backend computed helper field
2. แก้ไข dashboard/list/report ให้แยก `No Learners` ออกจาก `InProgress`
3. เพิ่ม tests ป้องกันและรับรองพฤติกรรมนี้

---

### 3. Harden Master Data Impact Checks And Active Helper Text

**Priority:** Medium-Low  
**Status:** Open  
**Goal:** เพิ่มความปลอดภัยในการกด Deactivate/Delete Master Data ที่มีผลกับข้อมูลประวัติของระบบ

**Why it matters:**
- ข้อเสนอแนะของ master data ส่วนใหญ่เน้นเรื่องความถูกต้องด้าน Data Governance และความปลอดภัยในการลบข้อมูลที่เชื่อมโยงกับ Entity อื่น

**Implementation Plan:**
1. ตรวจสอบ (Audit) Master Data Controllers ที่รองรับการทำงานปิดหรือลบข้อมูล
2. ใส่ pre-check และ error contract ที่เป็นมาตรฐานเดียวกันเมื่อมีการลบข้อมูลที่มี reference อยู่
3. ปรับเปลี่ยนรายงานและ UI helper text ให้แสดงความหมายของสถานะ Inactive ให้ชัดเจน

---

### 4. Remove Display Name As Business Key And Finalize Category Deletion Rules

**Priority:** Low  
**Status:** Open  
**Goal:** ปรับแก้การใช้ชื่อแสดงผล (Display Name) เป็น Business Key และกำหนดกติกาการลบข้อมูลแบบโครงสร้างต้นไม้ (Tree structures)

**Why it matters:**
- ป้องกันการ drift หรือผลกระทบย้อนหลังเมื่อมีการเปลี่ยนชื่อกลุ่มบทบาทการเรียนรู้หรือประเภทหลักสูตร

**Implementation Plan:**
1. ระบุคีย์ที่แท้จริง (Key Owner) ในระดับฐานข้อมูลและ API/Domain
2. ตรวจสอบการคิวรีหรือการกรองที่ยังพึ่งพาข้อมูลฟิลด์ชื่อแสดงผล (Name)
3. ออกแบบและพัฒนา delete/move behavior สำหรับประเภทของกลุ่มผู้เรียน (Learner Group Categories)

---

## Completed Milestones (เสร็จสมบูรณ์)

### 1. Finalize Retired Course Policy
- **Status:** Completed on 2026-05-29
- **Outcome:** ได้ข้อยุติในการเลือกใช้ระบบ **Hard Block** สำหรับกรณีที่จะ Retire คอร์สที่มี open enrollments ค้างอยู่ เพื่อความปลอดภัยสูงสุดของข้อมูลการเรียน และได้แก้ไขหน้าเอกสาร `COURSE-LIFECYCLE-RULES.md` และ `LIFECYCLE-OVERVIEW.md` ให้ตรงกับพฤติกรรมจริงของ `CourseService.cs`

### 2. Centralized SCORM Exam Completion Rule
- **Status:** Completed on 2026-04-30
- **Outcome:** นำ `ScormContentStatusPolicy` มาใช้เป็นจุดศูนย์กลางในการประเมินผลลัพธ์การเรียนและการสอบ โดยใช้กฎเดียวกันทั้งในการ map log ของ `LearningLogsController` และการแสดงผลสถานะบน Player ของ `EnrollmentsController`

### 3. Remove Dangerous LearningLog Completion Defaults
- **Status:** Completed on 2026-04-30
- **Outcome:** กำหนดให้ค่าเริ่มต้นของ `LearningLog.Status` เป็น `incomplete` และ `Progress` เป็น `0` โดยเพิ่ม test ชุดพิเศษตรวจสอบค่าเริ่มต้น เพื่อไม่ให้เกิดข้อมูลผิดพลาดระหว่างรันไทม์

### 4. Eliminate Remaining Raw Entity Or Ambiguous Lifecycle Payloads
- **Status:** Completed on 2026-04-30
- **Outcome:** คอนโทรลเลอร์ระดับ Domain Specific ทั้งหมดจะไม่คืน Entity ดิบโดยตรง และคืนรูปผ่าน Shaped DTO แทน (เช่น `CourseVersionDto`, `CourseDetailDto`, `ContentItemDto`)

### 5. Unify Due Soon Threshold Across Dashboard And Admin Filters
- **Status:** Completed on 2026-04-30
- **Outcome:** รวมศูนย์ค่าขอบเขตวันใกล้ส่ง (Due Soon) ไว้ที่ `AssignmentStatusKeys.DueSoonWindowDays` (7 วัน) โดยใช้ค่าขอบเขตเดียวกันทั้งบนบอร์ดบริหาร รายงาน และฟิลเตอร์การค้นหาของฝั่งแอดมิน

### 6. Externalize Completed Learner History Retention
- **Status:** Completed on 2026-04-30
- **Outcome:** ดึงกฎระยะเวลาเก็บประวัติผู้เรียนย้อนหลัง 1 เดือนออกจากโค้ดระดับ Controller ไปดูแลในชั้น Policy เฉพาะชื่อ `EnrollmentVisibilityPolicy`

### 7. Extend Shared Content Publication Policy With Impact Preview
- **Status:** Completed on 2026-04-30
- **Outcome:** เพิ่มระบบสรุปผลกระทบ (Impact Preview) ก่อนการลบหรือการยกเลิกเผยแพร่เนื้อหาแบบกลุ่ม (Bulk Unpublish) เพื่อแสดงรายการคอร์สและเวอร์ชันหลักสูตรที่จะได้รับผลกระทบให้แอดมินเห็นก่อนกดยืนยันการบำรุงรักษา
