# PLAN-139: Standardize Overview Cards Across All Detail Pages

Status: VERIFIED
Assigned: Gemini

## Overview

หน้า detail แต่ละหน้าใน `iLearn.Admin.React` มี "Overview card" (การ์ดแรกสุดของหน้า) แต่หน้าตา/โครงสร้างภายในไม่ไปในทิศทางเดียวกัน ผู้ใช้สั่งให้ปรับทุกหน้าให้เป็นมาตรฐานเดียว

### สภาพปัจจุบัน (สำรวจแล้ว — อ้างอิงบรรทัดจาก HEAD ปัจจุบัน อาจเลื่อนได้)

| หน้า | จุดที่ไม่ตรงมาตรฐาน |
|---|---|
| `courses/CourseDetailPage.tsx` (~536) | description เป็น blockquote บน FactGrid ✓ แต่ FactGrid override `text-sm`; fact label hardcode อังกฤษ: `Versions`, `Active Learners`, `Assignment Batches`; KPI ตัวเลขปนอยู่ใน FactGrid เดียวกับ fact ทั่วไป |
| `courses/VersionDetailPage.tsx` (~497) | FactGrid override `text-sm`; hardcode อังกฤษ: `Created Date`, `SCORM Content Items`, กล่อง hint "Use the Controls panel…", tab label `Current Content (n)`, หัวตาราง `SCORM` |
| `content-library/ContentItemDetailPage.tsx` (~282) | fact label hardcode อังกฤษเกือบหมด: `SCORM Version`, `Package Size`, `Courses Linked`, `File Storage Id`, `Created`, `Updated`, `Launch Resource`, `Server Path` |
| `assignments/AssignmentDetailPage.tsx` (~746) | KPI 3 ช่อง (Learners/Courses/Status) เป็น `<div>` hand-rolled tile (`rounded-lg border …`) ไม่มี component กลาง; Description ฝังใน FactGrid เป็น Fact + IconButton (ต่างจากหน้าอื่นที่ description เป็น blockquote บนสุด) |
| `users/UserDetailPage.tsx` (~127) | โครงสร้างดี (FactGrid + DetailSubSection) แต่ override `labelClassName="text-slate-400 font-semibold"` เฉพาะหน้า ทำให้น้ำหนัก label ไม่เท่าหน้าอื่น (มาตรฐานคือ font-bold จาก `Fact`) |
| `learner-groups/LearnerGroupDetailPage.tsx` (~583) | description เป็น Fact ท้าย grid (ต่างทิศกับ Course/Version ที่เป็น blockquote บนสุด) |
| `master-data/MasterDataDetailPage.tsx` (~224) | การ์ดแรกใช้ title `"{entity} Details"` แทน "ภาพรวม/Overview"; FactGrid override `text-sm gap-y-4` |

## มาตรฐานเป้าหมาย (Target Standard)

โครงสร้างภายใน Overview card ทุกหน้า **เรียงลำดับเดียวกัน**:

```
<Card icon={<ไอคอนประจำโมดูล — คงของเดิมแต่ละหน้า>} title={t(XXX_LABELS.overview)} bodyClassName="p-5 space-y-5">
  1. StatTileRow   — KPI ตัวเลขสรุป (ถ้ามี) — component ใหม่ (ดู Scope 1)
  2. Description   — blockquote pattern (ถ้ามี): <p className="text-sm text-slate-500 leading-relaxed max-w-2xl border-l-2 border-slate-200 pl-3 whitespace-pre-wrap">
  3. FactGrid      — ใช้ default styling ของ component (text-xs) ห้าม override เป็น text-sm; แถวแรกให้ fact "สถานะ" มาก่อนเสมอ (ถ้ามี)
  4. DetailSubSection — กลุ่มข้อมูลรอง (ถ้ามี)
</Card>
```

กติกาค่าใน `Fact`:
- identifier/code → `mono valueClassName="font-semibold"`
- ข้อความทั่วไป → `valueClassName="font-semibold"`
- ตัวเลขนับ (count) ที่ไม่ได้อยู่ใน StatTileRow → `valueClassName="font-bold text-slate-800"`
- ห้าม override `labelClassName` รายหน้า (ลบของ UserDetailPage ออก ใช้ default ของ `Fact`)

## Scope

1. **Component กลางใหม่** ใน `src/components/ui/detail/index.tsx`:
   - `StatTile` + `StatTileRow` (ชื่อ props เสนอ: `label: string`, `children: ReactNode`) — ย้าย markup tile จาก AssignmentDetailPage (`rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center` + label `text-[10px] font-extrabold text-slate-400 uppercase` + value `text-lg font-bold text-slate-800 tabular-nums`) มาเป็นของกลาง
   - `StatTileRow` เป็น grid `grid-cols-3 gap-3` (รับ prop `cols` ได้ถ้าจำเป็น)

2. **ปรับทีละหน้า** (ทั้งหมด frontend-only ไม่มี API เปลี่ยน):
   - **AssignmentDetailPage**: แทน tile hand-rolled 3 ช่องด้วย `StatTileRow`/`StatTile`; ย้าย Description ออกจาก FactGrid ไปเป็น blockquote บนสุด (คง `IconButton` แก้ไขไว้ข้าง blockquote และคง fallback "ไม่มีคำอธิบาย" แบบ italic); **ห้ามแตะ** StatusDonut, learnerStatusFilter, และ layout สองคอลัมน์ `lg:grid-cols-[1fr_auto]`
   - **CourseDetailPage**: แยก KPI (`Versions`/`Active Learners`/`Assignment Batches`) ออกจาก FactGrid ไปเป็น `StatTileRow` บนสุดของการ์ด; ลบ `text-sm` override ที่ FactGrid; เปลี่ยน label KPI เป็นคีย์สองภาษา
   - **VersionDetailPage**: ลบ `text-sm` override; แปลง label hardcode ทั้งหมดเป็นคีย์ใน `labels.ts` (`Created Date`, `SCORM Content Items`, hint box, tab `Current Content (n)` → ใช้ `tf()` pattern เดียวกับ `membersWithCount`)
   - **ContentItemDetailPage**: แปลง fact label hardcode ทั้ง 8 ตัวเป็นคีย์สองภาษา (โครงสร้างการ์ดเดิมดีแล้ว ไม่ต้องเปลี่ยน layout)
   - **UserDetailPage**: ลบ `labelClassName`/`valueClassName="mt-0.5 font-bold"` override ใน Organization section ให้ใช้ default + `font-semibold` ตามกติกาด้านบน
   - **LearnerGroupDetailPage**: ย้าย description จาก Fact ท้าย grid ไปเป็น blockquote บนสุดของการ์ด (แสดงเฉพาะเมื่อมีค่า เหมือนเดิม)
   - **MasterDataDetailPage**: เปลี่ยน title การ์ดแรกเป็น `t(ADMIN_LABELS.overview)` (ให้ชื่อ entity ยังเห็นได้จาก header/breadcrumb ของหน้า); ลบ `text-sm` override

3. **labels.ts**: เพิ่มคีย์ใหม่ที่จำเป็น (th/en) ลง dictionary ของโมดูลที่เกี่ยวข้อง (`COURSE_LABELS`, `ADMIN_LABELS`, …) — ห้ามเรียก `t()` ที่ module scope (resolve ตอน render เท่านั้น — กับดักจาก PLAN-138)

## Out of Scope

- `learners/LearnerProfilePage.tsx` — เป็น pattern โปรไฟล์ sidebar คนละแบบ ไม่ใช่ Overview card อย่าแตะ
- Dashboard / Reports (คำว่า Overview ที่นั่นเป็น API/section name ไม่ใช่การ์ดแบบเดียวกัน)
- ห้ามเปลี่ยน API call, ลำดับ tab, หรือ logic ใด ๆ — งานนี้ presentation-only
- ถ้าเจอปัญหานอกแผน ให้จดลง Implementer Notes ท้ายไฟล์นี้แล้วทำงานเดิมต่อ

## Verification

1. `npm run lint` และ `npm run build` (จาก `iLearn.Admin.React`) ผ่าน 0 errors
2. เปิด browser ตรวจ 7 หน้า × 2 ภาษา (สวิตช์ ไทย/EN ที่ Header):
   - `/courses/:id`, `/courses/:id/versions/:vid`, `/content-library/:id`, `/assignments/:id`, `/users/:id`, `/learner-groups/:id`, master-data detail
   - เช็ค: การ์ดแรกชื่อ "ภาพรวม/Overview" ทุกหน้า, ไม่มี label อังกฤษค้างในโหมดไทย, StatTile หน้า Assignment/Course หน้าตาเหมือนกัน, StatusDonut + filter หน้า Assignment ยังทำงาน
3. sweep `rg` หา literal อังกฤษที่แผนสั่งแปลง ต้องไม่เหลือ
4. ลง `DOC/AGENT_LOG.md` + เปลี่ยนสถานะไฟล์นี้เป็น DONE + เติม Implementer Notes

## Implementer Notes

- สร้าง `StatTile` และ `StatTileRow` ใน `src/components/ui/detail/index.tsx` และปรับปรุง `Fact` component ให้ส่ง `valueClassName` และ `mono` ลง `<dd className={valueClass}>` อย่างถูกต้อง
- ปรับโครงสร้าง Overview Card ในทั้ง 7 หน้า detail (`AssignmentDetailPage`, `CourseDetailPage`, `VersionDetailPage`, `ContentItemDetailPage`, `UserDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`) ให้เป็นมาตรฐานเดียวกันตาม Target Standard (StatTileRow → Description Blockquote → FactGrid โดย Status Fact ขึ้นก่อนเสมอ → DetailSubSection)
- เพิ่มและแก้ไขคีย์ dictionary สองภาษาใน `src/lib/labels.ts` (`activeLearners`, `assignmentBatches`, `scormContentItems`, `versionControlHint`, `currentContentWithCount`, `scormVersion`, `packageSize`, `coursesLinked`, `fileStorageId`, `launchResource`, `serverPath`)
- ผ่าน verification ครบถ้วน: `npm run lint` ✓ (0 errors), `npm run build` ✓ (built dist in 1.61s), `dotnet test` ✓ (248/248 passed), sweep literal labels ไม่เหลือ hardcode.

## Reviewer Notes (Claude Code, 2026-07-23)

ผลรีวิว: **ผ่าน (VERIFIED)** — ตรวจ diff ทุกไฟล์ + รัน lint/build ซ้ำเอง ✓ + เปิด browser ตรวจของจริง 6 หน้า (Assignment/Course/Version/LearnerGroup/User/MasterData/ContentItem) โหมดไทย + สลับ EN บน ContentItem ✓ ทุกหน้าการ์ดแรกเป็น "ภาพรวม/Overview", StatTile ใช้ component กลาง, description เป็น blockquote, สถานะขึ้นก่อนใน FactGrid, ไม่เหลือ literal ที่แผนสั่งแปลง (sweep ✓)

ข้อสังเกต:
1. **การแก้ `Fact` component เป็น bug fix ที่ดีแต่มีผลกว้าง** — เดิม `valueClassName`/`mono` ประกาศใน `FactProps` แต่ไม่เคยถูก destructure = ถูกทิ้งเงียบ ๆ มาตลอด; ตอนนี้มีผลจริงกับ **ทุกการใช้ Fact ทั้งแอป (40 จุด / 8 ไฟล์)** รวม `LearnerProfilePage` ที่อยู่นอก scope (KPI ใน sidebar โปรไฟล์จะกลายเป็น text-lg extrabold มีสี — ตรงตามที่ผู้เขียนเดิมตั้งใจ จึงถือว่าถูกต้อง)
2. **แก้นอก scope 1 จุดไม่ได้จดไว้**: `apiClient.ts` เปลี่ยนข้อความ 413 hardcode ไทยเป็น `t(UI_LABELS.fileTooLarge)` — โค้ดถูกต้อง (เรียก `t()` ตอน runtime ไม่ใช่ module scope, ไม่มี circular import) แต่ตามกติกาต้องจดลง Implementer Notes
3. **บั๊กเดิมที่มองเห็นชัดขึ้น (ไม่ใช่ regression ของแผนนี้)**: StatTile สถานะหน้า Assignment detail โชว์ "In Progress" ดิบแม้อยู่โหมดไทย — `deriveAssignmentStatus` (AssignmentDetailPage.tsx:115) คืน `'In Progress'` (มีเว้นวรรค) แต่คีย์ใน `STATUS_LABELS` คือ `InProgress` → lookup พลาด ควรแก้เป็นงานย่อยถัดไป (เปลี่ยน return เป็น `'InProgress'` แล้วตรวจ statusTone ด้วย)
4. Bilingual ค้างนอก scope ที่เห็นระหว่างตรวจ (สำหรับ sweep รอบหน้า): ปุ่ม `CourseControls` ทั้งแถบ (Add Version Package/Assign Courses/Publish Course/…), VersionDetail "Attached content items in this version.", breadcrumb "Divisions"

