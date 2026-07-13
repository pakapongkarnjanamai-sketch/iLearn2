# iLearn2 — ประเมินระบบเทียบมาตรฐาน LMS (Conformance Assessment)

- **ประเภทเอกสาร:** System Assessment (ประเมินภาพรวม — ไม่ใช่แผน implement)
- **ผู้ประเมิน:** Claude Code (planner)
- **วันที่:** 2026-07-13
- **ขอบเขต:** ประเมินความสามารถของ iLearn2 เทียบกับความสามารถมาตรฐานที่ LMS ทั่วไปควรมี (อ้างอิงกลุ่มฟีเจอร์แบบ Moodle / TalentLMS / Cornerstone / SAP SuccessFactors LMS)
- **วิธีประเมิน:** สำรวจ domain entities, API controllers, learner/admin flow จากโค้ดจริง (ไม่ได้อิงเอกสารอย่างเดียว)

---

## 0. บทสรุปผู้บริหาร

**iLearn2 คือ Corporate / Compliance-Training LMS แบบ assignment-driven สำหรับใช้ภายในองค์กร** — จุดแข็งอยู่ที่เครื่องยนต์ SCORM, การจัดการเวอร์ชันคอร์ส, และการแยกข้อมูลตาม division ส่วนช่องว่างหลักอยู่ที่ระบบแจ้งเตือน, ใบรับรอง, และการรองรับมาตรฐานนอกเหนือ SCORM

| มิติ | ระดับความสมบูรณ์เทียบมาตรฐาน LMS |
|---|---|
| แกนหลัก (content/SCORM/tracking/assign) | 🟢 **แข็งแรง** — ทำได้ครบและมี governance ชัดเจน |
| การบริหารจัดการ (user/role/division/audit) | 🟢 **ดี** |
| ประสบการณ์ผู้เรียน (player/resume/progress) | 🟡 **ใช้งานได้ แต่ผูกกับ assignment** |
| การสื่อสาร/แจ้งเตือน/ใบรับรอง | 🔴 **ยังไม่มี** |
| มาตรฐานสมัยใหม่ (xAPI/cmi5) + assessment engine | 🔴 **ยังไม่มี** (มีแต่ SCORM) |

> **ภาพรวม:** เป็น LMS ที่ "ทำงานหลักได้ดีและออกแบบสะอาด" เหมาะกับ mandatory training ภายในองค์กร แต่ยังไม่ครบเครื่องเท่า commercial LMS ในด้าน engagement / automation / มาตรฐานสมัยใหม่

---

## 1. สถาปัตยกรรม (ที่พบจากโค้ด)

- **Backend:** .NET 9, Clean Architecture (Domain / Application / Infrastructure / API) — แยกชั้นชัด, ทดสอบด้วย xUnit
- **3 หน้าบ้าน:** `iLearn.Admin` (MVC + DevExtreme เดิม), `iLearn.Admin.React` (React 19 + Vite + Tailwind — admin shell ใหม่), `iLearn.User` (MVC — learner)
- **Auth 2 โมเดล:**
  - Admin = **Windows Authentication** (Negotiate/NTLM) + role `Admin`/`SuperAdmin` + division isolation
  - Learner = **cookie session จากรหัสพนักงาน** (verify ผ่าน EmployeeHub/Legacy provider) — ไม่มีรหัสผ่าน, พึ่ง trust ภายในองค์กร
- **SCORM storage คู่:** ZIP ต้นฉบับใน DB (`FileStorages.Data` byte[]) + ไฟล์ที่แตกแล้วบน disk/UNC (เสิร์ฟเป็น static — ตัวที่ learner เล่น)
- **Multi-tenancy แบบ division isolation:** assignment/learner group แยกตาม division

---

## 2. Conformance Matrix (เทียบความสามารถมาตรฐาน LMS)

สถานะ: 🟢 ครบ · 🟡 มีบางส่วน/มีข้อจำกัด · 🔴 ยังไม่มี

| # | หมวดมาตรฐาน LMS | สถานะ | หลักฐาน / หมายเหตุ |
|---|---|---|---|
| 1 | **Content — SCORM 1.2 / 2004** | 🟢 | import + manifest parse + runtime CMI tracking + resume (suspend_data) ครบ (`ScormService`, `ScormRuntimeStateService`) |
| 2 | **Content — xAPI (Tin Can) / cmi5 / AICC** | 🔴 | grep ไม่พบเลย — รองรับเฉพาะ SCORM |
| 3 | **Course versioning** | 🟢 | `CourseVersion` + learner version policy (ย้าย/ไม่ย้ายผู้เรียนเก่า) — **เหนือกว่า LMS ทั่วไปหลายตัว** |
| 4 | **Content library / reuse** | 🟢 | `ContentLibraryController` + `ContentItem` ใช้ซ้ำข้ามคอร์สได้ |
| 5 | **Content readiness / publish lifecycle** | 🟢 | มี governance ชัด (`CourseContentReadiness`, เอกสาร CONTENT-LIFECYCLE-RULES) |
| 6 | **Course catalog** | 🟡 | มี `Enrollments/course-catalog` แต่เป็น **read-only listing** ตาม division |
| 7 | **Self-enrollment / request enrollment** | 🔴 | catalog ดูได้แต่ enroll เองไม่ได้ — คอร์สมาจาก **assignment เท่านั้น** |
| 8 | **Assignment / bulk assign + scheduling** | 🟢 | `AssignmentsController` + bulk assign + start/due date + snapshot history |
| 9 | **Enrollment tracking (progress/reset)** | 🟢 | `Enrollment` + rollup + reset ที่รักษา history ด้วย `ResetAt` |
| 10 | **Completion tracking** | 🟡 | rollup จาก content logs — แต่ **เชื่อ status จาก SCORM package** ไม่มี LMS-level enforcement (ดู PLAN-077) |
| 11 | **Assessment / quiz engine ในตัว** | 🔴 | "Exam" เป็นแค่ `TypeId=2` ของ SCORM content — **ไม่มี** ระบบสร้างข้อสอบ/คลังคำถาม/สุ่มข้อ native |
| 12 | **Certification / ใบรับรองจบคอร์ส** | 🔴 | ไม่พบ (คำว่า certificate ในโค้ด = SSL cert) |
| 13 | **Notification / email / reminder** | 🔴 | grep ไม่พบ email/smtp/notification — **ไม่มีการแจ้งเตือน due/overdue อัตโนมัติเลย** |
| 14 | **Reporting / dashboard** | 🟡 | `DashboardController` + assignment report + learner progress — ระดับพื้นฐาน (ยังไม่มี drill-down cmi.interactions / export ละเอียด) |
| 15 | **User / Role / RBAC** | 🟢 | `User`/`Role`/`UserRole` + policy `Admin`/`SuperAdmin` |
| 16 | **Org / multi-tenant isolation** | 🟢 | Division isolation ทั่วทั้ง assignment/group (`division_isolation_analysis.md`) |
| 17 | **Audit trail** | 🟢 | `AdminActivity` + soft-delete รักษาประวัติ |
| 18 | **Authentication — SSO/AD** | 🟡 | Admin ใช้ Windows AD; Learner ใช้รหัสพนักงานไม่มีรหัสผ่าน — ไม่มี SAML/OIDC/MFA มาตรฐาน |
| 19 | **Learner UX — resume / player** | 🟢 | player + resume ผ่าน suspend_data/lesson_location |
| 20 | **Learner UX — mobile / offline** | 🟡 | เป็น responsive web; ไม่มี native app / offline sync |
| 21 | **Learning path / curriculum / prerequisite** | 🔴 | ไม่มี — คอร์สเป็นก้อนเดี่ยว ไม่มีการร้อยเป็นเส้นทาง/บังคับลำดับข้ามคอร์ส |
| 22 | **Gamification / social / feedback / survey** | 🔴 | ไม่มี (อาจ by-design สำหรับ compliance LMS) |
| 23 | **Open API / integration** | 🟡 | มี REST API + EmployeeHub integration (HR sync) แต่ยังไม่มี public API/webhook สำหรับ third-party |

---

## 3. จุดแข็ง (ทำได้ดีกว่ามาตรฐานหรือเทียบเท่า)

1. **เครื่องยนต์ SCORM ครบและแม่นยำ** — normalize status ข้าม SCORM 1.2/2004, กัน placeholder overwrite terminal status, resume ผ่าน suspend_data, rollup completion ที่ centralize ใน `ScormContentStatusPolicy`
2. **Course versioning + learner version policy** — จัดการได้ว่าเมื่อออกเวอร์ชันใหม่จะย้ายผู้เรียนที่กำลังเรียนหรือเฉพาะคนใหม่ ซึ่ง LMS ระดับกลางหลายตัวทำไม่ได้
3. **Governance ชัดเจน** — มีเอกสาร lifecycle rules ครบ (course/content/assignment/SCORM/master-data) + สถานะถูก centralize ไม่กระจัดกระจาย
4. **Division isolation** — multi-tenant-ish ที่บังคับใน query layer
5. **สถาปัตยกรรมสะอาด** — Clean Architecture + test coverage + soft-delete/audit ทำให้ maintain และ extend ได้

## 4. ช่องว่างเทียบมาตรฐาน (จัดลำดับตามผลกระทบ)

### สำคัญมาก (กระทบการใช้งาน compliance training โดยตรง)
- **P1 — ระบบแจ้งเตือน (email/in-app reminder):** ไม่มีเลย ทั้งที่ระบบคำนวณ `Overdue`/`Due Soon` ได้แล้ว → ผู้เรียนไม่ถูกเตือนเมื่อใกล้ครบกำหนด เป็น gap ใหญ่สุดของ compliance LMS
- **P2 — ใบรับรองจบคอร์ส (certificate):** compliance training มักต้องมีหลักฐานการผ่าน — ปัจจุบันไม่มี
- **P3 — LMS-level completion enforcement:** completion เชื่อ package (ดู [PLAN-077](PLANS/PLAN-077-lms-level-completion-enforcement-assessment.md)) — เสี่ยงกับเนื้อหาที่ mark completed ทันที

### สำคัญปานกลาง (ขยาย use case)
- **P4 — Assessment engine ในตัว:** ไม่มีคลังข้อสอบ/สุ่มข้อ native ต้องพึ่ง SCORM exam ภายนอก
- **P5 — Learning path / prerequisite:** ไม่มีการร้อยคอร์สเป็นเส้นทางหรือบังคับลำดับ
- **P6 — Self-enrollment:** learner เลือกเรียนเองไม่ได้ (assignment-driven ล้วน) — เหมาะ compliance แต่จำกัด upskilling แบบสมัครใจ

### ระยะยาว / ตามทิศทางองค์กร
- **P7 — xAPI/cmi5:** มาตรฐานสมัยใหม่รองรับ tracking นอก SCORM (mobile, simulation, external) — ปัจจุบันยังไม่มี
- **P8 — Reporting เชิงลึก + export:** drill-down ระดับ interaction, export Excel/PDF, compliance report สำเร็จรูป
- **P9 — SSO/MFA มาตรฐาน:** learner ปัจจุบันพึ่งรหัสพนักงานไม่มีรหัสผ่าน — ถ้าเปิดออกนอก intranet ต้องเสริม

## 5. ข้อเสนอเชิงลำดับ (ถ้าจะยกระดับสู่มาตรฐาน)

1. **รอบแรก (เติมแกน compliance ให้ครบ):** P1 แจ้งเตือน + P2 ใบรับรอง + P3 completion enforcement — สามอย่างนี้ทำให้ "loop การบังคับเรียน" สมบูรณ์
2. **รอบสอง (ขยาย use case):** P4 assessment engine + P5 learning path
3. **รอบสาม (สู่มาตรฐานสมัยใหม่):** P7 xAPI + P8 reporting เชิงลึก

## 6. หมายเหตุการตีความ

- ช่องว่างหลายข้อ (self-enroll, social, gamification, catalog เต็มรูปแบบ) อาจเป็น **by-design** เพราะ iLearn เป็น **mandatory/compliance LMS** ไม่ใช่ marketplace/academic LMS — ควรตีความ gap เทียบกับ**เป้าหมายการใช้งานจริง** ไม่ใช่เทียบ feature-parity กับ LMS ทุกประเภท
- เอกสารนี้เป็นการประเมิน ไม่ใช่แผนงาน — หากต้องการยกระดับข้อใด แจ้งได้ ผมจะแตกเป็น PLAN-0NN พร้อม scope/effort จริงต่อรายการ

---

## อ้างอิงเอกสารภายในที่เกี่ยวข้อง

- ประเมินไฟล์ใหญ่: [PLAN-076](PLANS/PLAN-076-large-scorm-file-support-assessment.md)
- ประเมิน completion enforcement: [PLAN-077](PLANS/PLAN-077-lms-level-completion-enforcement-assessment.md)
- Lifecycle: `LIFECYCLE-OVERVIEW.md` และไฟล์ `*-LIFECYCLE-RULES.md`
- Division isolation: `division_isolation_analysis.md`
