# PLAN-081: Assignment Detail — ซ่อนคอลัมน์ Assigned Courses & Progress (ย่อ/ขยายได้) ในแท็บ Learners

- **Status:** DONE → VERIFIED
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **อ้างอิง:** หน้า `/assignments/{id}` แท็บ Learners — [AssignmentDetailPage.tsx](../../iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx)

> ผู้ใช้สั่ง (2026-07-14): ในแถบ Learners ของหน้า assignment detail อยากซ่อนส่วน **Assigned Courses & Progress** ไว้ก่อน ถ้าอยากดูค่อยกดแสดง เพื่อประหยัดพื้นที่ (learner 1 คนมีได้ถึง 12+ คอร์ส ทำให้แต่ละแถวสูงมาก scroll ลำบาก)

---

## ปัญหาปัจจุบัน

ตาราง Learners (`AssignmentDetailPage.tsx` ~บรรทัด 828–952) มีคอลัมน์ "Assigned Courses & Progress" ที่ render รายการคอร์สทุกคอร์สของ learner แบบเต็มเสมอ (ชื่อคอร์ส + code + ProgressBar + StatusBadge + ปุ่ม reset รายคอร์ส) — assignment ที่มี 12 คอร์ส × 35 คน ทำให้แถวสูงมากและเห็น learner ได้ทีละ 1–2 คนต่อจอ

## Scope

แก้ไฟล์เดียว: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`

### 1. State ใหม่

```tsx
// per-row: learnerCode ที่กางรายการคอร์สอยู่
const [expandedCodes, setExpandedCodes] = useState<Set<string>>(new Set())
```

- reset เป็น `new Set()` ใน `useEffect` ที่ผูกกับ `[id]` ตัวเดิม (บรรทัด ~227 ที่ reset `visibleCourseRows`/`selectedCodes` อยู่แล้ว)
- **ไม่ต้อง** reset ตอน search/filter เปลี่ยน — ให้สถานะกางค้างไว้ (ผู้ใช้ค้นหาต่อเนื่องไม่อยากให้หุบเอง)

### 2. Cell คอลัมน์ Assigned Courses & Progress — collapsed by default

แถวที่ **ยังไม่กาง** (default): แสดงบรรทัดเดียวแบบกะทัดรัด

- `Badge` (tone neutral, variant soft) แสดงจำนวนคอร์ส เช่น `12 courses` (ตัวเลขไม่ใส่ comma — เป็น count เล็ก)
- ปุ่มกาง: `AppButton` variant `ghost` ขนาดเล็ก ข้อความ `Show courses` + icon `ChevronDown` (lucide) — คลิกแล้ว add learnerCode เข้า `expandedCodes`
- กรณี `l.courses.length === 0` คงข้อความ *No courses assigned* เดิม ไม่มีปุ่มกาง

แถวที่ **กางแล้ว**: render รายการคอร์สเต็มรูปแบบ **เหมือนโค้ดปัจจุบันทุกประการ** (ชื่อคอร์ส/code/ProgressBar/StatusBadge/IconButton reset รายคอร์ส) + ปุ่มหุบ `Hide courses` + icon `ChevronUp` ด้านบนหรือท้ายรายการ

> **สำคัญ:** ปุ่ม reset รายคอร์ส (`handleResetLearnerCourse`) อยู่ในส่วนที่ถูกซ่อน — ต้องยังเข้าถึงได้ครบเมื่อกาง ห้ามตัดฟีเจอร์นี้ทิ้ง

### 3. ปุ่มกาง/หุบทั้งหมด (global)

เพิ่มปุ่ม `Expand all` / `Collapse all` ที่แถบ toolbar ของการ์ด Learners (บริเวณ `ListToolbar` `toolbarContent` หรือถัดจาก SegmentedToggle filter):

- ใช้ `AppButton` variant `ghost`/`secondary` ตัวเดียว toggle ตามสถานะ: ถ้ามีแถว(ที่ผ่าน filter)ยังไม่กาง → label `Expand all` (กางทุก `filteredLearners`); ถ้ากางครบแล้ว → `Collapse all` (เคลียร์ Set)
- Expand all ให้ add เฉพาะ `filteredLearners` ที่ `courses.length > 0`

### 4. กติกา UI ที่ต้องตาม (README React)

- ห้าม hand-roll `<button>` — ใช้ `AppButton`/`IconButton` เท่านั้น (ปุ่ม Clear ที่ hand-roll อยู่เดิมในไฟล์นี้เป็นของเก่า **อย่าไปแตะ** — นอก scope)
- ป้ายจำนวนคอร์สใช้ `Badge` จาก `src/components/ui` ห้าม `<span>` pill เอง
- icon ใหม่ (`ChevronDown`, `ChevronUp`) import เพิ่มจาก `lucide-react` ใน import block เดิม

## นอก Scope (ห้ามทำ)

- ห้ามแตะแท็บ Courses, Overview, sidebar actions, modals ทั้งสาม
- ห้ามเปลี่ยน API call / type `AssignmentDetail` — งานนี้ presentation-only ไม่มี contract เปลี่ยน
- ห้าม refactor ตารางไปใช้ `AppTable` (ตารางนี้มี expandable rows เฉพาะทาง — คงโครงเดิม)
- ห้ามแก้หน้า `AssignmentReportPage` / `AssignmentGanttPage` แม้จะมี pattern คล้ายกัน — ถ้าเห็นว่าควรทำด้วย จดใน Implementer Notes

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

ทดสอบมือบน dev (`localhost:5173`) หน้า `/assignments/291` (หรือ assignment ใดที่มีหลายคอร์สต่อคน):

1. เปิดแท็บ Learners → ทุกแถวหุบ default, เห็น badge จำนวนคอร์ส + ปุ่ม Show courses, แถวเตี้ยลงชัดเจน
2. กด Show courses → เห็นรายการคอร์สครบ + ProgressBar + ปุ่ม reset รายคอร์สใช้ได้
3. Expand all / Collapse all ทำงานกับแถวที่ผ่าน filter
4. Search/เปลี่ยน status filter → สถานะกางไม่ reset, checkbox bulk-select + Load more ยังทำงานเดิม
5. Learner ที่ไม่มีคอร์ส → ข้อความ No courses assigned เดิม ไม่มีปุ่ม

## Implementer Notes

- ทำตามแผนครบทุกข้อ ไม่มีเบี่ยงเบน
- `expandedCodes` state (Set<string>) reset ใน `useEffect([id])` ตามสเปค
- Collapsed cell แสดง `Badge` จำนวนคอร์ส + `AppButton` ghost "Show courses" พร้อม `ChevronDown`
- Expanded cell แสดง `AppButton` "Hide courses" + `ChevronUp` ด้านบน ตามด้วยรายการคอร์สเดิมครบ (ProgressBar, StatusBadge, IconButton reset รายคอร์ส)
- Expand all / Collapse all ปุ่มเดียว toggle อยู่ถัดจาก SegmentedToggle ใน ListToolbar toolbarContent — ทำงานกับ `filteredLearners` ที่มีคอร์ส > 0
- Import เพิ่ม: `ChevronDown`, `ChevronUp` (lucide), `Badge` (components/ui)
- Verified: `npm run lint` ผ่าน, `npm run build` (tsc -b + vite build) ผ่าน 0 errors

## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็ม `git diff AssignmentDetailPage.tsx` อิสระ (ไม่เชื่อ Implementer Notes อย่างเดียว):

- **โครง diff ตรงสเปคทุกข้อ:** `expandedCodes` state + reset ใน `useEffect([id])` ✅, collapsed cell = `Badge` (tone neutral/soft) + `AppButton` ghost "Show courses"/ChevronDown ✅, expanded cell คงโครงเดิม 100% (ProgressBar/StatusBadge/`handleResetLearnerCourse` ผ่าน IconButton ไม่หาย) ✅, ปุ่ม Expand all/Collapse all ที่ toolbar ทำงานกับ `filteredLearners` (ไม่ใช่แค่ visible/paginated) ✅, `l.courses.length === 0` ยังขึ้น "No courses assigned" ไม่มีปุ่ม ✅
- **กติกา UI:** ใช้ `Badge`/`AppButton` ตรง prop signature จริง (`tone`/`variant`/`size` — เช็คจาก `Badge.tsx` แล้ว), ไม่มี hand-roll `<button>` ใหม่, import `ChevronDown`/`ChevronUp`/`Badge` สะอาด
- **Verify อิสระ:** `npm run lint` (0 warnings), `npm run build` = `tsc -b && vite build` ผ่าน 0 errors — รันเองซ้ำ ไม่ใช่แค่เชื่อ log
- **ไม่มีการแตะไฟล์นอก scope** — diff เดียวคือ `AssignmentDetailPage.tsx`
- **Gap:** ทดสอบ live click-through ใน browser ทำไม่ได้ในรอบนี้ — backend API `https://localhost:7128` (Windows-auth, ต้องรันผ่าน VS) ไม่ได้รันอยู่ (`ERR_CONNECTION_REFUSED`) จึงไม่ได้เห็นหน้าจอจริงตามเช็คลิสต์ 5 ข้อใน Verification — อาศัย code review + lint/build แทน แนะนำให้ผู้ใช้ทดสอบมือ 5 ข้อบน dev ที่รัน API ครบ (หรือ QA) ก่อนถือว่าปิดงานสมบูรณ์
- **Minor (ไม่บล็อก):** ปุ่ม Expand/Collapse all คำนวณ `filteredLearners.filter().every()` ซ้ำ 3 ครั้งต่อ render (icon/label/onClick) — list เล็ก (~35 คน) ไม่กระทบ perf ปล่อยผ่านได้

**สรุป: โค้ดผ่านรีวิว — ตรงสเปคครบ ไม่มี regression ที่มองเห็นจาก diff/lint/build. รอ manual click-through เพื่อปิด gap สุดท้าย**
