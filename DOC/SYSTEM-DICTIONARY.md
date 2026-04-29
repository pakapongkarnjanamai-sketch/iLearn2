# iLearn System Dictionary

Last updated: 2026-04-29

เอกสารนี้เป็น dictionary/glossary กลางของระบบ iLearn2 สำหรับใช้คุยงาน ออกแบบ UI เขียนเอกสาร API และตั้งชื่อในโค้ดให้ตรงกัน โดยสรุปจาก Domain entities, DTOs, controllers, Admin/Learner UI, และกติกาที่ใช้จริงในระบบปัจจุบัน

## Naming Rules

| Rule | Preferred Term | Meaning / Usage |
| --- | --- | --- |
| Course UI content | Content / Content item | ใช้ในข้อความที่ Admin เห็น เช่น Selected Content, Content Type, Content library |
| Backend content entity | Resource | ใช้ใน entity, DTO, API contract, payload เช่น Resource, ResourceIds, CourseResource, ResourcesCRUD |
| Individual learner in Admin UI | Learner | ใช้ใน label, count, selection, assignment action สำหรับบุคคลที่เรียน |
| Legacy/backend learner identifier | Student / StudentCode | ใช้ใน backend model, controller name, external employee/student source |
| Group membership context | Member | ใช้เมื่อพูดถึงสมาชิกภายใน Student Group เท่านั้น |
| Distribution action | Assignment | งานมอบหมาย course ให้ group/learner พร้อมช่วงวันที่ |
| Learner registration record | Enrollment | record ที่ผู้เรียนคนหนึ่งถูกผูกกับ course/version และเก็บ progress |
| Course publication | Publish Course | ทำให้ course เห็นได้ในระบบผู้เรียน ต้องมี active version ที่พร้อม |
| Content readiness | Ready / Not Ready / Published / Queued Upload | สถานะความพร้อมของ content/version ก่อนเปิดใช้งาน |
| Progress wording | Progress | หมายถึง completion progress ของ course/resource ไม่ใช่ SCORM page progress ภายใน SCO |

## Product And Platform

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| iLearn / iLearn2 | ระบบ iLearn | Internal e-Learning platform ขององค์กร |
| LMS | ระบบจัดการการเรียนรู้ | Learning Management System สำหรับจัด course, assignment, learner progress |
| Admin UI | หน้าผู้ดูแลระบบ | ASP.NET Core MVC สำหรับ HR/training/admin จัดการข้อมูลและรายงาน |
| Learner UI / User UI | หน้าผู้เรียน | ASP.NET Core MVC/Razor สำหรับผู้เรียนดู course และเล่น SCORM |
| API | บริการหลังบ้าน | ASP.NET Core Web API ที่ Admin/User เรียกใช้ |
| Clean Architecture | สถาปัตยกรรมแยกชั้น | Domain -> Application -> Infrastructure -> Presentation |
| DevExtreme | ชุด UI component | ใช้ DataGrid, Form, Popup, Dialog, Chart ใน Admin UI |
| DataGrid | ตารางข้อมูล | พื้นที่หลักของ Admin UI สำหรับ scan/search/filter/export ข้อมูล |
| Dashboard | แดชบอร์ด | หน้าสรุป KPI, chart, trend, recent activity, maintenance status |
| Report | รายงาน | หน้าแสดงข้อมูลรายละเอียด/ประวัติ พร้อม export/print ตามบริบท |

## Users, Roles, And Access

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| User | ผู้ใช้ระบบ | บัญชี Windows Auth ใน Admin หรือระบบที่มี role |
| NID | รหัสผู้ใช้เครือข่าย | ค่า identity ของ Windows user เช่น domain account |
| Role | บทบาท | สิทธิ์ของผู้ใช้ เช่น Admin, SuperAdmin |
| UserRole | ความสัมพันธ์ผู้ใช้กับบทบาท | ตารางเชื่อม User กับ Role |
| Admin | ผู้ดูแลระบบ | ผู้ใช้งาน Admin UI ตามสิทธิ์ที่ได้รับ |
| SuperAdmin | ผู้ดูแลสูงสุด | role ที่เห็นเมนูจัดการระบบ/ทุก division |
| Division-scoped Admin | ผู้ดูแลตามส่วนงาน | admin ที่ถูกจำกัดข้อมูลตาม DivisionId |
| Windows Authentication | การยืนยันตัวตน Windows | ใช้ identity จาก Windows/AD แทน login form ปกติ |
| Claims | ค่าประกอบ identity | เช่น Role, DivisionId ใช้ตัดสินสิทธิ์และเมนู |
| Authorization Policy | นโยบายสิทธิ์ | กติกาที่ controller/action ต้องประกาศเพื่อควบคุมการเข้าถึง |
| Internal Learner Headers | header ยืนยันผู้เรียนภายใน | `X-iLearn-Learner-Code`, timestamp, signature จาก User UI ไป API |

## Organization Structure

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Division | ส่วนงาน / ฝ่าย / หน่วยงาน | หน่วยหลักสำหรับจัดข้อมูลและ data isolation |
| DivisionId | รหัสส่วนงาน | key ที่ใช้กรองข้อมูลตาม division |
| Category | หมวดหมู่ course | หมวดหมู่ของ Course และผูกกับ Division |
| Course Type | ประเภท course | ประเภทของ Course เช่น mandatory/optional ตามข้อมูลระบบ |
| Student Group Category | หมวดหมู่กลุ่มเรียน | folder/tree สำหรับจัด Student Group |
| Student Group | กลุ่มเรียน | กลุ่มของ learner ที่ใช้มอบหมาย course เป็นชุด |
| Student Group Member | สมาชิกกลุ่มเรียน | record ที่เชื่อม StudentGroup กับ StudentCode |
| Parent Category | หมวดแม่ | node แม่ใน tree ของ StudentGroupCategory |
| Children | หมวดย่อย | node ลูกใน tree ของ StudentGroupCategory |
| Path | เส้นทางหมวด | materialized path เช่น `/12/45/` ใช้จัด tree |
| Depth | ระดับชั้น | ความลึกของหมวดใน tree เริ่มจาก root = 0 |
| Data Isolation | การแยกข้อมูลตามสิทธิ์ | การจำกัดข้อมูลตาม Division/role เพื่อไม่ให้ข้าม scope |

## Course And Content

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Course | หลักสูตร / วิชาเรียน | master record ของการเรียน มี code, title, description, category, type |
| Course Code | รหัส course | รหัสอ้างอิง course เช่น CS-101 |
| Course Title / Course Name | ชื่อ course | ชื่อที่แสดงใน Admin/Learner UI |
| Course Description | คำอธิบาย course | รายละเอียด จุดประสงค์ หรือคำอธิบายของ course |
| Course Detail | หน้ารายละเอียด course | หน้า Admin สำหรับดู summary, versions, actions, assignments |
| Course Version | เวอร์ชัน course | ชุด content ของ course ณ ช่วงเวลาหนึ่ง มี VersionNumber และ Note |
| Version Number | เลขเวอร์ชัน | เลขลำดับ เช่น 1, 2, 3 ใช้แยกชุด content |
| Version Note | หมายเหตุเวอร์ชัน | คำอธิบายเวอร์ชัน เช่น Initial release, updated content |
| Active Version | เวอร์ชันที่ใช้งาน | version ที่ถูกเลือกให้เป็น version หลักของ course |
| Draft / Inactive Version | เวอร์ชันร่าง / ยังไม่ใช้งาน | version ที่ยังไม่เปิดเป็น active |
| Set Active | ตั้งเป็นเวอร์ชันใช้งาน | action ใน Admin เพื่อเปิด CourseVersion เป็น active |
| Publish Course | เผยแพร่ course | เปิด course ให้ learner เห็น ต้องผ่าน readiness guard |
| Unpublish / Close Course | ปิด course | ปิดการมองเห็น/ใช้งานของ course สำหรับ learner |
| Content | เนื้อหา | คำใน Admin UI สำหรับสิ่งที่ผู้เรียนเปิดเรียน/สอบ |
| Content Item | รายการเนื้อหา | item เดี่ยวใน Selected Content หรือ CourseVersion |
| Content Library | คลังเนื้อหา | picker/grid ที่เลือก content ที่มีอยู่แล้วเข้ามาใน course/version |
| Selected Content | เนื้อหาที่เลือก | list/grid ของ content ที่จะอยู่ใน course/version |
| Resource | ทรัพยากร | backend entity ของ SCORM package หรือ learning object |
| Resource Type | ประเภท resource | backend field `TypeId`; user-facing เรียก Content Type |
| Content Type | ประเภทเนื้อหา | UI label สำหรับ Learn/Exam |
| Learn | เนื้อหาเรียน | content/resource type สำหรับบทเรียน |
| Exam | เนื้อหาสอบ | content/resource type สำหรับข้อสอบ/การประเมิน |
| CourseResource | ความสัมพันธ์ version กับ resource | junction entity ระหว่าง CourseVersion และ Resource พร้อม Order |
| Order | ลำดับ | ลำดับการแสดง content ใน CourseVersion |
| Existing Content | เนื้อหาที่มีอยู่แล้ว | content จาก library ที่เลือกเข้ามา ไม่ใช่ upload ใหม่ |
| New Upload | ไฟล์อัปโหลดใหม่ | SCORM zip ที่จะถูก process ตอน save |
| Queued Upload | รออัปโหลด | สถานะ UI ของไฟล์ใหม่ก่อน save/process |
| Published Content | เนื้อหาที่เผยแพร่แล้ว | Resource ที่ active และมี launch URL พร้อมใช้งาน |
| Not Ready Content | เนื้อหายังไม่พร้อม | Resource/Content ที่ยังขาดเงื่อนไข readiness |
| Readiness | ความพร้อม | การตรวจว่า version/content พร้อมเปิดใช้งานหรือไม่ |
| Readiness Issue | ปัญหาความพร้อม | เหตุผลที่ version/content ยัง active/publish ไม่ได้ |
| Auto-prepare | เตรียมให้อัตโนมัติ | backend พยายามแตก/เตรียม stored SCORM resource จาก FileStorage ก่อน fail |

## SCORM And Runtime

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| SCORM | มาตรฐาน SCORM | Shareable Content Object Reference Model; ระบบรองรับ SCORM 1.2 และ 2004 |
| SCORM 1.2 | SCORM รุ่น 1.2 | ใช้ field เช่น `cmi.core.lesson_status` |
| SCORM 2004 | SCORM รุ่น 2004 | ใช้ field เช่น `cmi.completion_status`, `cmi.success_status` |
| SCO | หน่วยเรียน SCORM | Shareable Content Object; ระบบปัจจุบัน launch หนึ่ง SCO ต่อ resource |
| imsmanifest.xml | manifest ของ SCORM | ไฟล์อธิบายโครงสร้าง package และ launch href |
| Manifest | ไฟล์กำกับ package | ข้อมูลที่ parser อ่านเพื่อหา launch file และ schema version |
| ResourceHref | path launch ภายใน package | relative href ที่มาจาก manifest |
| Launch URL | URL เปิดเล่น | URL ที่ browser ใช้เปิด SCORM content |
| Launch Href | href เปิดเล่น | path จาก manifest ก่อนแปลงเป็น URL เต็ม |
| FullUrl | URL เต็มของ content | URL ที่ชี้ไปยังไฟล์ launch หลัง import |
| FolderName | ชื่อโฟลเดอร์ content | ชื่อโฟลเดอร์ที่เก็บไฟล์หลังแตก zip |
| SchemaVersion | เวอร์ชัน schema | ค่าเวอร์ชัน SCORM ที่พบจาก manifest |
| Runtime State | สถานะ runtime | ข้อมูล CMI ล่าสุดของ learner ต่อ enrollment/resource |
| SCORM Runtime State | สถานะ runtime ของ SCORM | entity ที่เก็บ LessonLocation, SuspendData, status, score, time |
| Commit | บันทึก runtime | SCORM content ส่งข้อมูลกลับ API เพื่อ persist state/progress |
| CMI | ข้อมูล runtime SCORM | data model ของ SCORM เช่น status, score, time, suspend data |
| CMI Snapshot | snapshot CMI | JSON diagnostic/persisted input ของ runtime commit ไม่ส่งกลับใน player response |
| Lesson Location | ตำแหน่งบทเรียน | bookmark/resume location ใน SCORM |
| Suspend Data | ข้อมูลพักไว้ | state สำหรับ resume เมื่อกลับมาเรียนต่อ |
| Lesson Status | สถานะบทเรียน | SCORM 1.2 status เช่น passed/completed/failed/incomplete |
| Completion Status | สถานะ completion | SCORM 2004 completion field |
| Success Status | สถานะ success | SCORM 2004 success field; failed ต้องไม่ถือว่าผ่านแม้ completed |
| Raw Score | คะแนนดิบ | score ที่ SCORM ส่งมาโดยตรง |
| Session Time | เวลารอบปัจจุบัน | เวลาเรียนใน session commit นั้น |
| Total Time | เวลาสะสมจาก SCORM | total time ตาม field SCORM |
| Entry | entry mode | SCORM entry state เช่น resume/ab-initio ตาม package |
| Exit | exit mode | SCORM exit state เช่น suspend/logout/normal |
| LastCommittedAtUtc | เวลาบันทึกล่าสุด | timestamp ล่าสุดของ runtime commit |
| Player | เครื่องเล่น content | UI/runtime ที่เปิด SCORM และรับส่ง commit |
| Player Info | ข้อมูลสำหรับ player | payload ของ course/version/resources สำหรับ Learner UI |
| Player Resource | resource ใน player | item ที่ learner เห็นใน player พร้อม launch/status/progress |
| View Only / Read Only | ดูอย่างเดียว | player mode ที่ไม่มี enrollment หรือไม่อนุญาต update progress |

## Assignment And Enrollment

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Assignment | การมอบหมาย | งานมอบหมาย course ให้ learner/group พร้อม start/due date |
| AssignmentNo | เลขที่การมอบหมาย | identifier ของ assignment เช่น `ASSIGN-YYYYMMDD-###` |
| Assignment Description | คำอธิบายการมอบหมาย | รายละเอียดของ assignment batch |
| AssignmentCourse | รายการ course ใน assignment | detail line ที่เชื่อม Assignment กับ Course |
| Assignment Rule | rule ของ assignment | ในบาง flow หมายถึง AssignmentCourse line ต่อหนึ่ง course |
| Bulk Assign | มอบหมายเป็นกลุ่ม | wizard สำหรับเลือก courses + learners/groups แล้วสร้าง enrollments |
| Validate Before Assign | ตรวจสอบก่อนมอบหมาย | API/action ตรวจ conflict ก่อนยืนยัน assignment |
| Assignment Conflict | ความขัดแย้งในการมอบหมาย | กรณี learner/course ซ้ำหรือมีสถานะเดิมที่ต้องยืนยัน |
| Conflict Message | ข้อความแจ้ง conflict | รายละเอียดปัญหาก่อนมอบหมาย |
| Confirm Reassign In Progress | ยืนยันมอบหมายซ้ำผู้ที่กำลังเรียน | flag ยอมให้ reassign learner ที่ยัง In Progress |
| Confirm Reassign Completed | ยืนยันมอบหมายซ้ำผู้ที่เรียนจบ | flag ยอมให้ reassign learner ที่ Completed แล้ว |
| Enrollment | การลงทะเบียนเรียน | record ของ learner ต่อ course/version ที่ใช้เก็บ progress/status |
| Enrollment Assignment | ความสัมพันธ์ enrollment กับ assignment | junction ระหว่าง Enrollment และ Assignment พร้อม snapshot |
| EnrolledCourseVersion | เวอร์ชันที่ learner ถูกผูก | version id/number ที่ enrollment กำลังใช้งาน |
| StudentCode | รหัสผู้เรียน/พนักงาน | backend key ของ learner ใน enrollment/log/group |
| Employee Code | รหัสพนักงาน | UI/API term เมื่อเลือก learner จาก directory |
| Start Date | วันที่เริ่ม | วันที่ assignment/enrollment เริ่มมีผล |
| Due Date | วันที่ครบกำหนด | deadline ของ assignment/enrollment |
| Extend Due Date | ขยายวันครบกำหนด | action เปลี่ยน DueDate ของ assignment/enrollment |
| Completed Date | วันที่เรียนจบ | วันที่ enrollment หรือ snapshot ถูก mark completed |
| Reset Enrollment | reset การลงทะเบียน | action รีเซ็ต attempt/progress ของ enrollment |
| ResetAt | เวลา reset | timestamp ที่ทำให้ log เก่ากว่าเวลานี้ไม่นับใน player |
| Reassign | มอบหมายซ้ำ | สร้างหรือ reset enrollment ใหม่จาก assignment เดิม/ใหม่ |
| Snapshot Progress | snapshot progress | progress ที่เก็บไว้ใน EnrollmentAssignment เพื่อรายงานตาม assignment |
| Snapshot Completed | snapshot completed | สถานะ completed ณ assignment link |
| Snapshot Completed Date | วันที่ completed ใน snapshot | วันที่ completed ที่เก็บใน EnrollmentAssignment |
| Valid Employee Codes | รหัสพนักงานที่ใช้ได้ | รายการ learner ที่ผ่าน validation ก่อน assign |
| Estimated Enrollments | จำนวน enrollment ที่คาดว่าจะสร้าง | count ประมาณจาก course x learner ก่อนยืนยัน bulk assign |

## Learner Progress And Status

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Progress | ความก้าวหน้า | completion progress 0-100 ของ course/resource |
| Activity Progress | ความก้าวหน้าภายในกิจกรรม | progress จาก SCORM page/score ภายใน resource ที่ยังไม่ complete |
| IsCompleted | เรียนจบแล้ว | boolean ว่า enrollment/resource สำเร็จแล้ว |
| Total Score | คะแนนรวม | score รวมระดับ enrollment/course |
| Score | คะแนน | score ของ resource/log/runtime |
| Total Time Spent | เวลารวมที่ใช้เรียน | เวลาสะสมระดับ enrollment |
| Total Seconds Played | จำนวนวินาทีที่เล่นรวม | time aggregate ใน LearningLog |
| Attempt Count | จำนวนครั้งที่เรียน/พยายาม | จำนวน attempt ใน LearningLog |
| Pending | รอเริ่ม | สถานะที่ learner ยังไม่เริ่มเรียน |
| Not Started | ยังไม่เริ่ม | dashboard/policy bucket สำหรับ learner ที่ยังไม่เริ่ม |
| In Progress | กำลังเรียน | learner/enrollment เริ่มแล้วแต่ยังไม่ complete |
| Completed | เสร็จสิ้น / เรียนจบ | learner/enrollment/resource complete แล้ว |
| Passed | ผ่าน | SCORM success/lesson status ที่ถือว่าผ่าน |
| Failed | ไม่ผ่าน | SCORM success/lesson status ที่ถือว่าไม่ผ่าน |
| Incomplete | ยังไม่สมบูรณ์ | SCORM/resource ยังไม่ complete |
| Unknown | ไม่ทราบสถานะ | SCORM success status ยังไม่ระบุชัด |

## Learner UI

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| My Courses | course ของฉัน | หน้ารวม course ที่ learner ถูก enroll |
| Course Card | card course | card ที่แสดง course, progress, due date ใน Learner UI |
| Continue Learning | เรียนต่อ | action เปิด player ต่อจาก progress เดิม |
| Start Learning | เริ่มเรียน | action เปิด player ครั้งแรก |
| Course Player | เครื่องเล่น course | หน้ารวม player + content list สำหรับ learner |
| Resource List | รายการ content ใน player | list ของ PlayerResourceDto ที่ learner เลือกเล่น |
| Runtime Commit | บันทึกสถานะการเรียน | call จาก player เพื่อ save progress/runtime state |
| Update Progress | อัปเดตความก้าวหน้า | endpoint/action สำหรับปรับ progress จาก learner runtime |
| Closed Course | course ที่ปิดแล้ว | learner ไม่ควรเห็น/เปิดเล่นหาก course ไม่ active หรือ version ไม่ ready |

## Admin UI And Workflows

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Wizard | ตัวช่วยทำงานทีละขั้น | multi-step Admin flow เช่น BulkAssign, Course Editor, VersionForm |
| Step Card | card ขั้นตอน | indicator ด้านบนของ wizard |
| Selection Step | ขั้นตอนเลือกข้อมูล | step ที่มี filter + grid + selection tray |
| Options Step | ขั้นตอนตั้งค่า | step ที่เลือก policy/status/options ก่อน save |
| Review Step | ขั้นตอนทบทวน | step สรุปก่อน commit |
| Persistent Selection Tray | แถบรายการที่เลือก | tray ที่แสดง selected items ข้ามหน้า DataGrid |
| Filter Sidebar | sidebar ตัวกรอง | ส่วนกรองข้อมูลด้านซ้ายใน selection workflow |
| Quick Filter | ตัวกรองด่วน | chip/filter ที่เปลี่ยนสถานะ grid อย่างรวดเร็ว |
| Monitor Bar | แถบติดตามสถานะ | แถบรวม quick filters และ KPI/status chips |
| Tag Pill / Status Pill | ป้ายสถานะ | badge สีตาม semantic state เช่น success/warning/danger/default |
| KPI | ตัวชี้วัดหลัก | ตัวเลขสรุปเช่น total courses, learners, completed |
| Summary Card | card สรุป | card แสดงค่า/label ใน review/report/dashboard |
| Empty State | สถานะไม่มีข้อมูล | UI เมื่อ grid/table ไม่มี record |
| Load Panel | loading overlay | DevExtreme loading indicator ระหว่าง API/action |
| Toast | ข้อความแจ้งเตือน | notification สั้นๆ เช่น success/error/warning |
| Dialog | กล่องยืนยัน/แจ้งเตือน | DevExpress dialog สำหรับ confirm/remediation |
| Export | ส่งออกข้อมูล | สร้าง Excel/PDF/image ตาม report/grid |
| Clear all cached data | ล้าง cache ทั้งหมด | Admin action ล้าง cache ผ่าน API และ Windows identity |

## Reports And Dashboard Metrics

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Overview | ภาพรวม | dashboard endpoint/section สำหรับภาพรวมระบบ |
| Stats | สถิติ | aggregate KPI ของระบบ |
| Enrollment Trends | แนวโน้มการลงทะเบียน | chart/report จำนวน enrollment ตามเวลา |
| Learning Activity Trends | แนวโน้มกิจกรรมการเรียน | chart/report activity/log ตามเวลา |
| Maintenance Status | สถานะบำรุงรักษา | dashboard section สำหรับสุขภาพระบบ/งานดูแล |
| Recent Admin Activities | กิจกรรมผู้ดูแลล่าสุด | log การทำงานล่าสุดของ admin |
| Priority Assignments | assignment ที่ควรสนใจ | dashboard grouping ของงานที่ due/overdue/active |
| Chart Data | ข้อมูลกราฟ | DTO/ชุดข้อมูลสำหรับ pie/bar/trend chart |
| History | ประวัติ | รายการย้อนหลัง เช่น assignment history, group history |
| Gantt | แผนภาพ Gantt | endpoint/report timeline ของ assignment |
| Due Soon | ใกล้ครบกำหนด | filter/status สำหรับรายการที่กำลังจะถึง due date |
| Overdue | เกินกำหนด | filter/status สำหรับรายการที่เลย due date |

## Version Activation Policy

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| Learner Version Policy | นโยบาย version ต่อผู้เรียน | กติกาเมื่อ active version ใหม่กับ learner ที่มี assignment เปิดอยู่ |
| NewLearnersOnly | เฉพาะผู้เรียนใหม่ | learner เดิมอยู่ version เดิม ผู้เรียนใหม่ใช้ version ใหม่ |
| MoveNotStarted | ย้ายเฉพาะคนยังไม่เริ่ม | move learner ที่ Not Started ไป version ใหม่ |
| ResetInProgress | reset คนที่กำลังเรียน | move และ reset learner ที่กำลัง In Progress |
| NotStartedCount | จำนวนยังไม่เริ่ม | count learner bucket สำหรับ impact/policy |
| InProgressCount | จำนวนกำลังเรียน | count learner bucket สำหรับ impact/policy |
| CompletedCount | จำนวนเรียนจบแล้ว | count learner ที่ completed; ไม่ reset ตาม policy ปัจจุบัน |
| OtherOpenCount | จำนวนเปิดอยู่แต่ไม่กระทบ | learner open assignment ที่ไม่เข้าเงื่อนไขย้าย/reset |
| EligibleOpenCount | จำนวนที่มีสิทธิ์ถูกย้าย | NotStarted + InProgress |
| HasEligibleOpenLearners | มี learner ที่ต้องเลือก policy | boolean สำหรับเปิด dialog/policy options |

## File Storage And Content Import

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| FileStorage | ที่เก็บไฟล์ในฐานข้อมูล | entity เก็บ original uploaded file bytes/metadata |
| ContentType | MIME type | ชนิดไฟล์ที่ upload |
| Data | ข้อมูลไฟล์ | byte array ของไฟล์เดิม ใช้ auto-prepare ได้ |
| Length | ขนาดไฟล์ | byte length ของไฟล์ |
| Upload | อัปโหลด | ส่งไฟล์ SCORM zip เข้า server |
| ZIP Package | ไฟล์ zip package | SCORM package ที่ upload/import |
| Extract / Unzip | แตกไฟล์ | process แตก zip ไปยัง CourseFolder |
| CourseFolder | โฟลเดอร์ course | path ที่เก็บไฟล์ content หลัง import |
| Host UNC | path network share | path server เช่น shared folder สำหรับ content storage |
| Public URL | URL public content | URL ที่ browser ใช้เข้าถึง content ที่แตกไฟล์แล้ว |
| Import | นำเข้า | process parse manifest, extract, create/update resource metadata |

## API And Backend Concepts

| Term | Thai Meaning | Description / Usage |
| --- | --- | --- |
| CRUD Controller | controller จัดการข้อมูลพื้นฐาน | endpoint pattern `Get`, `Post`, `Put`, `Delete`, `GetPaged` |
| Lookup | ข้อมูลสำหรับ dropdown | endpoint/DTO ที่ส่งข้อมูลย่อสำหรับ select box |
| GetActive | ดึงรายการ active | endpoint ที่คืนเฉพาะ active/ready records ตาม business rule |
| GetPaged | ดึงข้อมูลแบ่งหน้า | endpoint สำหรับ DataGrid remote paging/filter/sort |
| DTO | object รับส่งข้อมูล | Data Transfer Object ระหว่าง layer/API/UI |
| Payload | ข้อมูลที่ส่ง request | JSON/FormData ที่ client ส่งไป API |
| FormData | multipart form payload | ใช้ส่ง version fields + files + ResourceIds/ResourceTypes |
| Route | เส้นทาง API | URL pattern ของ controller/action |
| ProblemDetails | รูปแบบ error response | response มาตรฐานสำหรับ validation/conflict/error |
| Conflict / 409 | ขัดแย้งทาง business rule | เช่น active version ไม่ ready หรือ assign conflict |
| Bad Request / 400 | request ไม่ถูกต้อง | validation fail หรือ payload ไม่ครบ |
| Unauthorized / 401 | ยังไม่ผ่าน auth | ไม่มี identity/credential ที่ถูกต้อง |
| Forbidden / 403 | ไม่มีสิทธิ์ | authenticated แล้วแต่ policy/role ไม่อนุญาต |
| Cache | cache ข้อมูล | ข้อมูลชั่วคราวที่ลดการอ่านซ้ำ |
| Clear Cache | ล้าง cache | action ล้าง cached data ฝั่ง API/Admin |

## Entity And Contract Names To Keep

| Name | Type | Keep Because |
| --- | --- | --- |
| Resource | Domain entity | เป็น model หลักของ content package ใน backend |
| CourseResource | Domain entity | เป็น junction table ระหว่าง CourseVersion กับ Resource |
| ResourceIds | API/FormData field | เป็น contract ของ create/update course version |
| ResourceTypes | API/FormData field | เป็น contract ของ content type ต่อ resource |
| resources | JSON property | response shape จาก API หลายจุด เช่น course dashboard/version detail |
| ResourcesCRUD | API route/controller | route ที่ Admin grids ใช้อยู่ |
| StudentsController | API/Admin controller | legacy backend naming; UI ควรใช้ Learners ใน label |
| StudentCode | model/DTO property | key ที่ผูกกับ external employee/student identity |
| StudentGroup | entity/menu term | ชื่อ entity ของกลุ่มเรียน ไม่เปลี่ยนเป็น LearnerGroup |

## Recommended UI Terms

| Context | Use | Avoid / Notes |
| --- | --- | --- |
| Course content selection | Select Existing Content | Avoid Select Existing Resources in visible UI |
| Course/version selected list | Selected Content | Keep Resource only in hidden payload/code contract |
| Content type column | Content Type | Backend field may still be `typeId` / ResourceTypes |
| Existing source badge | Content library | Avoid Resource library in user-facing copy |
| Individual people counts | Learner / Learners | Avoid Student unless referring to backend/controller/entity name |
| Student Group membership | Member / Members | Use only inside group membership context |
| Not ready remediation | Content item is not ready | Error text may still include backend Resource details |
| Active course precondition | Set one version as Active before publishing | Publish requires ready active version |
| Course progress | Progress | Clarify Activity Progress for inside-SCORM progress |

## Common Status Matrix

| Status | Thai Meaning | Applies To | Meaning |
| --- | --- | --- | --- |
| Active | ใช้งาน | Course, Version, Resource | เปิดใช้ในระบบตาม layer นั้น |
| Inactive | ไม่ใช้งาน | Course, Version, Resource | ยังไม่เปิดใช้งานหรือถูกปิด |
| Published | เผยแพร่แล้ว | Content/Resource | พร้อมใช้งาน มี URL และ active |
| Not Ready | ยังไม่พร้อม | Version/Content | ยังขาด resource, URL, active flag, หรือ SCORM metadata |
| Queued Upload | รออัปโหลด | New Upload | รอ save/process SCORM |
| Pending | รอดำเนินการ | Enrollment/Assignment | ยังไม่เริ่มหรือยังไม่ถึงสถานะเรียน |
| In Progress | กำลังดำเนินการ | Enrollment/Assignment | เริ่มแล้วแต่ยังไม่ completed |
| Completed | เสร็จสิ้น | Enrollment/Assignment/Resource | จบแล้วตาม rule ของระบบ |
| Overdue | เกินกำหนด | Assignment/Enrollment | DueDate ผ่านไปแล้วยังไม่ completed |
| Due Soon | ใกล้ครบกำหนด | Assignment/Enrollment | ใกล้ถึง DueDate |
| No Impact | ไม่กระทบ | Version policy | ไม่มี learner ที่ต้องย้าย/reset |
| Action Required | ต้องเลือกการดำเนินการ | Version policy | มี learner ที่ต้องเลือก policy ก่อน save/activate |

## Notes For Future Naming

- คำที่ผู้ใช้เห็นควรสะท้อนงานจริงของผู้ใช้ ไม่จำเป็นต้องเหมือน entity name ทุกคำ
- ถ้าคำหนึ่งมีทั้ง UI term และ backend term ให้ระบุ layer เสมอ เช่น Content (UI) vs Resource (Backend)
- หลีกเลี่ยงการเปลี่ยนชื่อ DTO/API contract โดยไม่ทำ migration/compatibility plan เพราะ Admin/User อาจเรียก field เดิมอยู่
- ถ้าสร้างหน้า Course/Admin ใหม่ ให้ใช้ `Content` ใน label และใช้ `content*` ใน local JS helper/state เมื่อไม่ได้ผูก contract
- ถ้าสร้าง endpoint หรือ entity ใหม่ที่เกี่ยวกับ SCORM package จริง ให้พิจารณาว่ายังเป็น `Resource` domain หรือเป็น UI-level `Content`
- รายงานและ dashboard ควรใช้คำเดียวกับหน้าหลัก เช่น Learners, Content, Enrollments, Assignments