# PLAN-033: เอา Tab ออกจากหน้า Detail ทั้งหมด → stack section เรียงหน้าเดียว

- **Status:** VERIFIED ✅ (Claude review 2026-06-16: 3 detail pages ไม่มี DetailTabs/activeTab (stacked sections), CourseDetail lazy→load-on-mount, DetailTabs เหลือ CourseEditor, ux_ui_analysis §2.4 = Stacked Sections, build/lint 0/0)
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Medium
- **Estimated scope:** 3 หน้า detail + ลบ/อัปเดต `DetailTabs` + sync `ux_ui_analysis.md`

## Problem / เป้าหมาย

ผู้ใช้ตัดสินใจ: **ไม่ต้องการ Tab ในหน้า Detail อีกต่อไป** — ให้แสดงเนื้อหาทุกส่วนแบบ **stack section เรียงลงมาในหน้าเดียว** (scroll) แทนการสลับแท็บ

> นี่เป็นการเปลี่ยนทิศจาก PLAN-031 (ที่เพิ่งสร้าง `<DetailTabs>`) — ตั้งใจ

## หน้าที่มี tab (จากการสำรวจ)

| หน้า | tab ที่ต้องแปลงเป็น section | บรรทัด |
|---|---|---|
| `CourseDetailPage.tsx` (970) | Overview, Versions, Learners, Assignments | tabs ~475 |
| `AssignmentDetailPage.tsx` (761) | Overview, Courses, Learners | tabs ~364 |
| `LearnerGroupDetailPage.tsx` (1100) | Overview, Members | tabs ~562 |

**กันออก:** `CourseEditorPage.tsx` = **editor (ไม่ใช่ detail)** — tab จัด section ของฟอร์มแก้ไข คนละบริบท → **คงไว้** (เว้นแต่ผู้ใช้สั่งเพิ่ม)

## Scope (ทำแค่นี้)

### 1. แปลง tab → stacked sections (ทั้ง 3 หน้า)
- เอา `<DetailTabs>` + state `activeTab`/`setActiveTab` ออก
- เนื้อหาของแต่ละ tab เดิม → กลายเป็น **section การ์ดเรียงลงมา** ในคอลัมน์ซ้ายของ `DetailLayout` แต่ละ section เปิดหัวด้วย `SectionHeader` (ชื่อเดิมของ tab เช่น "Versions", "Learners", "Members")
- ลำดับ section = ลำดับ tab เดิม (Overview ขึ้นก่อน)
- ใช้ shared detail components เดิม (`DetailCard`/`FactGrid`/`Fact`/`SectionHeader`) — **ห้ามเขียน markup การ์ด/grid เอง**
- `ControlsSidebar` ขวา คงเดิม

### 2. Data loading — เปลี่ยน lazy-per-tab → load-on-mount
- หน้าที่โหลดข้อมูล tab แบบ lazy (เช่น `CourseDetailPage` โหลด learners/assignments ตอนคลิก tab — ดู `if (activeTab === 'learners')` ~306) → เปลี่ยนเป็น **โหลดทุก section ตอน mount** (เพราะไม่มี tab trigger แล้ว)
- แต่ละ section แสดง `LoadingState size="section"` ของตัวเองระหว่างโหลด (ไม่บังทั้งหน้า) — section ที่เสร็จก่อนแสดงก่อนได้
- **ระวัง:** อย่าให้ section หนัก (ตาราง learners/members เยอะ) ทำทั้งหน้าค้าง — โหลดขนานกัน (`Promise.all` หรือแยก effect ต่อ section) + มี loading/empty state ราย section

### 3. จัดการ `DetailTabs`
- หลังเอาออกจาก 3 detail page → `DetailTabs` เหลือผู้ใช้แค่ `CourseEditorPage` (editor) → **คงไฟล์ไว้** (ยังไม่ dead)
- ถ้าผู้ใช้ยืนยันภายหลังให้เอา tab ออกจาก CourseEditor ด้วย ค่อยลบ `DetailTabs` (แผนแยก)

### 4. Sync doc
- อัปเดต `DOC/ux_ui_analysis.md` §2.4 หัวข้อ "แถบแท็บสลับข้อมูล (Tabs Layout)" → เปลี่ยนเป็นมาตรฐานใหม่: **หน้า detail แสดงทุก section แบบ stack เรียงลงมา ไม่ใช้ tab** (tab คงไว้เฉพาะ editor ถ้ามี)

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน **เนื้อหา/ข้อมูล/ฟังก์ชัน** ในแต่ละ section (แค่ย้ายจาก tab → section เรียง) — ตาราง members/learners, version list, course list, modal ต่าง ๆ ต้องทำงานครบเหมือนเดิม
- ห้ามแตะ `CourseEditorPage` (editor — คง tab)
- ห้ามแตะ `DetailLayout`/`DetailCard`/`Fact*`/`ControlsSidebar`
- ห้ามแตะ backend
- ห้ามลบ `DetailTabs` (ยังมี CourseEditor ใช้)

## Acceptance criteria

- [x] 3 หน้า detail ไม่มี `<DetailTabs>` / `activeTab` แล้ว — แสดงทุก section เรียงลงมาในหน้าเดียว
- [x] เนื้อหาทุก section ครบ (Course: overview/versions/learners/assignments; Assignment: overview/courses/learners; LearnerGroup: overview/members) + ฟังก์ชันเดิม (modal/action/table) ทำงาน
- [x] ข้อมูลที่เคย lazy-load โหลดตอน mount ถูกต้อง + มี section loading/empty state (ไม่บังทั้งหน้า, ไม่ค้าง)
- [x] breadcrumb/ControlsSidebar/NotFound/Loading เดิมยังทำงาน
- [x] `ux_ui_analysis.md` §2.4 sync (ไม่ใช้ tab แล้ว)
- [x] `npm run lint` (0/0) + `npm run build` ผ่าน

## Verification

```powershell
npm run lint
npm run build
```
ทดสอบ manual: `/courses/:id` (เห็น overview+versions+learners+assignments เรียงลงมา ครบ + version actions/modal ทำงาน), `/assignments/:id` (overview+courses+learners + modal extend/add learners), `/learner-groups/:id` (overview + members table + เลือก/ลบสมาชิก) — scroll ดูครบ ไม่มีค้าง

## Implementer Notes

- เอา `DetailTabs` และ state (`activeTab` / `detailTab`) ออกจาก 3 หน้า detail ตาม scope:
	- `CourseDetailPage`: แสดง `Course Overview` → `Versions` → `Learners` → `Assignments` แบบ stacked
	- `AssignmentDetailPage`: แสดง `Overview` → `Courses` → `Learners` แบบ stacked
	- `LearnerGroupDetailPage`: แสดง `Overview` → `Members` แบบ stacked
- `CourseDetailPage` เปลี่ยนจาก lazy-per-tab เป็น load-on-mount สำหรับ `loadLearners()` และ `loadAssignments()` โดยคง `LoadingState size="section"` ของแต่ละ section ไว้เหมือนเดิม
- อัปเดตเอกสาร `DOC/ux_ui_analysis.md` §2.4 ให้เป็นมาตรฐานหน้า detail แบบ stacked sections และไม่ใช้ tabs แล้ว
- Verification:
	- `npm run lint` ผ่าน
	- `npm run build` ผ่าน
