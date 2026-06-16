# PLAN-037: รวมมาตรฐาน Badge / Pill / Tag ทั้งระบบ

- **Status:** VERIFIED ✅ (Claude Code review 2026-06-16)
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** Medium
- **Estimated scope:** ขยาย/เพิ่ม shared component 1–2 ตัว + refactor `<span>` ที่ hardcode สไตล์ badge ในหลายหน้า

## Problem

ผู้ใช้พบว่าใน `iLearn.Admin.React` มี `<span>` ที่ทำหน้าที่เป็น "ป้ายสถานะ/ป้ายชนิด/ป้ายตัวเลข" จำนวนมาก แต่ละจุดเขียน utility class เองทำให้ **drift** ทั้งขนาดตัวอักษร น้ำหนักฟอนต์ ความโค้งมุม และโทนสี ตัวอย่างจากผู้ใช้:

```html
<span class="inline-flex min-h-[24px] items-center rounded-full border px-2.5 text-xs font-bold border-amber-300 bg-amber-50 text-amber-600">Draft</span>
<span class="inline-flex px-2 py-0.5 rounded font-bold text-xs bg-emerald-100 text-emerald-800">Active Version</span>
<span class="inline-flex px-2 py-0.5 rounded font-bold text-xs bg-red-100 text-red-700">Overdue</span>
<span class="inline-flex px-2 py-0.5 rounded font-bold text-xxs bg-emerald-100 text-emerald-800">Completed</span>
<span class="inline-flex rounded border px-2 py-0.5 text-[10px] font-extrabold uppercase border-amber-100 bg-amber-50 text-amber-700">Folder</span>
<span class="inline-flex items-center px-2 py-0.5 rounded text-xxs font-bold bg-emerald-100 text-emerald-800">Yes</span>
```

**ข้อสังเกตสำคัญ:** มี shared component อยู่แล้วบางส่วน แต่หน้า/คอลัมน์จำนวนมากยัง hardcode `<span>` เองแทนที่จะเรียกใช้ ทำให้เกิดความหลากหลาย:
- `font-bold` / `font-semibold` / `font-extrabold` ปนกัน
- `text-xs` / `text-xxs` / `text-[10px]` ปนกัน
- `rounded` / `rounded-sm` / `rounded-full` ปนกัน
- โทนสีเดียวกันใช้เฉดต่างกัน (`emerald-800` vs `emerald-700`, `red-700` vs `rose-800`)

### Component ที่มีอยู่แล้ว (ของจริงในโค้ด)
- [StatusBadge.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/StatusBadge.tsx) — pill พื้นสีอ่อน (`inline-flex px-2 py-0.5 rounded font-bold text-xs/xxs`) + helper `statusTone()` + `export StatusBadge`
- [StatusText.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/StatusText.tsx) — chip เส้นขอบ `rounded-full border min-h-[24px]`
- [CourseStatusBadge.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx) — wrapper เฉพาะ domain course (`CourseStatusBadge` + `CourseStatusText`)

ปัญหาคือ component พวกนี้ **ครอบ use case ส่วนใหญ่ได้อยู่แล้ว** แต่ยังไม่ถูกใช้ และยังขาดมาตรฐานสำหรับ "ป้ายชนิด/ชนิดข้อมูล (type tag)" แบบ uppercase และ "ป้ายตัวเลข (count)"

### จุดที่ hardcode `<span>` (สำรวจแล้ว — ไม่ใช่ exhaustive แต่ครอบคลุมหลัก)
| ประเภท | ตัวอย่างไฟล์ | สไตล์ปัจจุบัน |
|---|---|---|
| Status pill (พื้นอ่อน) | CourseListPage:601, SystemConfigPage:122, AssignmentReportPage, AssignmentDetailPage:546 | `px-2 py-0.5 rounded text-xxs/xs font-bold/semibold` |
| Status chip (เส้นขอบ rounded-full) | จุดที่ลอก StatusText ด้วยมือ | `rounded-full border min-h-[24px] ...` |
| Type tag (uppercase) | EntityListPage:64, LearnerGroupListPage:570, CourseListPage:597 | `rounded border text-[10px]/xxs font-extrabold uppercase` |
| Readiness (Ready/Not Ready) | CourseEditorPage:612/787/925, VersionDetailPage:547/763/873, VersionFormPage:443/654, ContentItemEditorPage:217 | `inline-flex border px-1.5 py-0.5 text-xs font-extrabold rounded-sm ${readiness.className}` |
| Count / นับจำนวน | CourseEditorPage:639, VersionDetailPage:686, VersionFormPage:470, BulkAssignPage:251/294/369 | `border bg-white px-2 py-0.5 rounded text-xs font-bold text-slate-500` ฯลฯ |

---

## เป้าหมายมาตรฐาน (Design)

รวมทุก badge ให้มาจาก **primitive เดียว** = `Badge` แล้วให้ component เดิมเป็น wrapper บางๆ ของมัน เพื่อไม่ทำลาย call site ที่มีอยู่

### Token มาตรฐาน (ตายตัว)
- **ขนาด:** `xs` = `text-xs` (default), `xxs` = `text-xxs` — **ยกเลิกการใช้ `text-[10px]`** ให้ map ไป `xxs`
- **น้ำหนักฟอนต์:** `font-bold` เป็นค่าเริ่มต้นเดียว (ยกเลิก `font-semibold`/`font-extrabold` ใน badge — ยกเว้น variant `tag` ที่ใช้ `font-extrabold uppercase` โดยตั้งใจ)
- **ระยะ padding:** `px-2 py-0.5` (soft/outline), tag ใช้ `px-2 py-0.5` เท่ากัน
- **โทนสี (tone) มาตรฐาน** — รวมเฉดให้เหลือชุดเดียว:
  | tone | soft (พื้น) | outline (ขอบ) |
  |---|---|---|
  | success | `bg-emerald-100 text-emerald-800` | `border-emerald-300 bg-emerald-50 text-emerald-700` |
  | info | `bg-blue-100 text-blue-800` | `border-blue-200 bg-blue-50 text-blue-700` |
  | warning | `bg-amber-100 text-amber-800` | `border-amber-300 bg-amber-50 text-amber-700` |
  | danger | `bg-red-100 text-red-700` | `border-red-300 bg-red-50 text-red-600` |
  | neutral | `bg-slate-100 text-slate-700` | `border-slate-200 bg-white text-slate-500` |

  > ทำให้ `rose-*` ใน CourseStatusBadge map เข้า `danger` (ใช้ `red-*`) เพื่อเลิกเฉดซ้ำ

### Variant
- `soft` (default) — พื้นสีอ่อน `rounded` ไม่มีขอบ → แทน StatusBadge เดิม
- `outline` — `rounded-full border min-h-[24px] px-2.5` → แทน StatusText เดิม
- `tag` — `rounded border text-xxs font-extrabold uppercase` → แทน type tag (Folder/Group/หมวดหมู่)

---

## Scope (ทำแค่นี้)

### 1. สร้าง primitive `Badge` ใน `src/components/ui/Badge.tsx`
- Props: `tone?: BadgeTone` (`success|info|warning|danger|neutral`), `variant?: 'soft'|'outline'|'tag'` (default `soft`), `size?: 'xs'|'xxs'` (default `xs`), `children`
- รวมตาราง tone × variant ข้างบนเป็น lookup เดียว
- export `type BadgeTone` ด้วย

### 2. ทำ component เดิมให้เป็น wrapper ของ `Badge` (ไม่เปลี่ยน public API)
- `StatusBadge` → render `<Badge variant="soft" .../>` (คง prop `tone`/`size` + helper `statusTone()` เดิมไว้ทุกตัว ห้ามลบ export)
- `StatusText` → render `<Badge variant="outline" .../>` (คง prop `tone` เดิม; map `tone` 4 ค่าของมันเข้า BadgeTone)
- `CourseStatusBadge`/`CourseStatusText` → ใช้ `Badge` ภายใน, map `rose` → `danger`; **คง signature `{ status, statusCode }` และ `getCourseStatusTone()` เดิมไว้**

### 3. เพิ่ม wrapper `ReadinessBadge` (รวม readiness ที่ซ้ำ 8+ จุด)
- รับ prop เช่น `ready: boolean` + `label?: string` แล้ว render `<Badge variant="outline" tone={ready ? 'info' : 'neutral'}>` ตาม `readiness.className` เดิม (ตรวจสีจริงจาก [ContentItemEditorPage.tsx:217](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx#L217) ที่ใช้ `border-blue-200 bg-blue-50 text-blue-700` = ready)
- หาแหล่งที่ map `readiness.label/className` (น่าจะมี helper อยู่แล้ว) — ถ้ามี helper กลาง ให้ ReadinessBadge เรียกใช้ ไม่ต้องสร้างตรรกะใหม่

### 4. Refactor call site ที่ hardcode `<span>` ให้ใช้ component มาตรฐาน
ไล่แก้ตามตารางใน Problem — **เฉพาะ `<span>` ที่เป็น badge สถานะ/ชนิด/readiness/count เท่านั้น** ห้ามแตะ `<span>` ที่เป็น layout/ไอคอน/inline text (เช่น `DashboardPage:342`, `BulkAssignPage:280` ที่เป็น checkbox box)
- Count badge → `<Badge tone="neutral">{n} items</Badge>` (variant soft) หรือเพิ่ม wrapper `CountBadge` ถ้าซ้ำมากพอ (ขึ้นกับวิจารณญาณ implementer — ถ้าทำ ให้จดใน Implementer Notes)

### ขอบเขตที่ห้ามทำ (กันงานบาน)
- ห้ามเปลี่ยน "ข้อความ/ตรรกะ" ของสถานะ — แก้แค่ presentation
- ห้ามเปลี่ยนสีจนกระทบความหมาย (เขียว=ดี, แดง=เตือน) — แค่รวมเฉดให้เป็นชุดเดียว
- ห้ามแตะ `iLearn.Admin` (MVC เดิม)

---

## Verification (รันก่อนปิดงาน)
```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
- เปิดหน้าเหล่านี้ด้วยตา/diff: Course list, Version detail, Course editor, Learner-group list, System config, Bulk assign — ยืนยัน badge หน้าตายังถูกต้องและสม่ำเสมอ
- ยืนยันว่าไม่มี `text-[10px]` / `font-extrabold` (นอก variant `tag`) / `rose-` หลงเหลือใน badge: grep ตรวจ
```powershell
# ควรเหลือเฉพาะใน Badge.tsx / ที่ไม่ใช่ badge
rg "text-\[10px\]|font-semibold|rose-(100|700|800)" src/pages src/components
```

## Implementer Notes
- ทำ `CountBadge` เพิ่มเติมหรือไม่: **ไม่ได้เพิ่ม** — ใช้ `Badge tone="neutral"` ตรงจุดนับจำนวนโดยตรงเพื่อคง scope pure refactor และลด abstraction ที่ไม่จำเป็น
- Readiness helper อยู่ที่ไหน: เดิมมีฟังก์ชัน local `getContentReadiness` อยู่ใน `CourseEditorPage`, `VersionFormPage`, `VersionDetailPage` (คืน `{ label, className }`) จึงรวมเป็น helper กลาง `getContentReadinessBadgeModel()` ใน `ReadinessBadge.tsx` และให้ทั้ง 3 หน้าเรียกใช้ร่วม
- Span ที่ตั้งใจไม่แตะ: span ที่เป็น text/layout/meta ทั่วไป (เช่น micro-label, code mono text, tooltip text, icon wrapper, inline deleted marker) ไม่ได้ migrate ตามขอบเขตที่กำหนดว่าแก้เฉพาะ badge/status/type/readiness/count
- Wrapper compatibility: คง public API เดิมของ `StatusBadge`, `StatusText`, `CourseStatusBadge`/`CourseStatusText` และ `getCourseStatusTone()` ครบ โดยเปลี่ยน internals ให้เรียก `Badge`
- Color normalization: ลบการใช้โทน `rose-*` ใน course-status badge และ normalize ไป `danger` (`red-*`) ตามแผน
- Verification:
  - `npm run lint` ผ่าน
  - `npm run build` ผ่าน
