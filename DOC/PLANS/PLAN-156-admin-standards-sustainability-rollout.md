# PLAN-156: Admin standards sustainability rollout

- **Status:** SUPERSEDED
- **Superseded by:** PLAN-157 (execution contract). แผนนี้คงไว้เป็น strategy/history — **ห้าม implement จากไฟล์นี้**
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** React Admin standardization plan + targeted backend consistency follow-up
- **สร้างเมื่อ:** 2026-07-30

## Problem

งานรอบล่าสุดลด drift ไปบางส่วนแล้ว (เช่นลบ grid footer/status bar) แต่โค้ดยังมีรูปแบบที่กระจายหลายแนวในจุดสำคัญ:

1. ปุ่ม interactive ยังเขียนเองหลายหน้าแทนการใช้ primitives กลาง (`AppButton` / `IconButton` / `SegmentedToggle`).
2. บางหน้าเรียก `fetch` ตรงแทน abstraction กลาง (`fetchWithAccessControl`) ทำให้การจัดการ headers/error ไม่สม่ำเสมอ.
3. UI token/tone mapping บางส่วนซ้ำกันหลายไฟล์ (โดยเฉพาะ KPI/report tiles).
4. ฝั่ง backend ยังมี `DateTime.UtcNow` ตรงบางจุดที่ควรใช้ `_dateTime.Now` ตามกติกาเวลาไทย.

หากไม่ทำ rollout แบบเป็นระบบ จะกลับไปเกิด drift ซ้ำทุกครั้งที่เพิ่ม feature.

## Scope (ทำแค่นี้)

1. จัด rollout มาตรฐานแบบเป็น phase ที่ทำทีละส่วนได้ โดยไม่หยุดการพัฒนา feature.
2. กำหนด hard guard (lint/rules/checklist) เพื่อกัน regression อัตโนมัติ.
3. สร้างรายการ refactor ที่มีผลสูงก่อน (high-impact, low-risk) สำหรับ React Admin.
4. ระบุ backend consistency follow-up เฉพาะจุดที่กระทบมาตรฐานร่วม (เวลา, API call pattern).
5. กำหนด acceptance criteria + verification ต่อ phase.

## Out of scope (ห้ามแตะในแผนนี้)

- ไม่รวมการ redesign หน้าทั้งระบบในรอบเดียว.
- ไม่รวมการเปลี่ยน API contract ใหญ่หรือ breaking changes.
- ไม่รวมการเปลี่ยนสถาปัตยกรรมหลักของ Admin shell.
- ไม่บังคับ migration legacy MVC ทั้งหมดในรอบเดียว.

## Baseline findings snapshot (2026-07-30)

- React Admin: พบ `<button>` นอก primitives หลายไฟล์ (hotspots ใน pages และ components บางตัว).
- React Admin: พบ `fetch` ตรงใน report export pages และ health probe.
- React Admin: พบ KPI tone mapping ซ้ำในหลาย report pages.
- Backend: พบ `DateTime.UtcNow` ตรงใน `CourseVersionService` บางจุด.
- Legacy MVC: ยังมี `alert(...)` ใน report export fallback บาง view.

## Rollout phases

### Phase 0 — Guardrails first (1-2 วัน)

เป้าหมาย: กัน drift ใหม่ก่อนแตะ refactor จำนวนมาก

1. เพิ่ม lint policy สำหรับ React Admin:
   - ห้าม `<button>` ใหม่ใน `src/pages/**` ยกเว้น whitelist ที่จำเป็นจริง.
   - บังคับใช้ button primitives สำหรับ action ปกติ.
2. เพิ่ม rule/check สำหรับ network calls:
   - เพจทั่วไปต้องใช้ `fetchWithAccessControl`.
   - กรณีพิเศษ (binary export/health) ต้องผ่าน helper กลางที่กำหนดไว้.
3. อัปเดต checklist ทีมใน README ให้มี “Do/Don’t” แบบสั้นและตรวจได้.

**Exit criteria:** สร้าง PR ใหม่ที่ละเมิดกฎเหล่านี้แล้ว lint ต้อง fail ได้จริง.

### Phase 1 — High-impact UI standardization (2-4 วัน)

เป้าหมาย: ลดความแปรปรวนด้าน interaction และ style

1. ย้าย raw buttons ใน hotspots ไปใช้ primitives:
   - เริ่มจาก `AssignmentDetailPage`, `BulkAssignPage`, `LearnerGroupDetailPage`, `CourseEditorPage`, `VersionDetailPage`.
2. สร้าง helper สำหรับ table row actions (เช่น icon action preset) เพื่อลดการเขียนคลาสซ้ำ.
3. ปรับ interactive div ที่คลิกได้ให้เป็น semantic button/link ที่เข้ากฎ a11y เดียวกัน.

**Exit criteria:** จำนวน raw `<button>` นอก primitives ลดลงอย่างน้อย 60% จาก baseline.

### Phase 2 — Data/API consistency (1-2 วัน)

เป้าหมาย: network behavior กลางเดียว

1. สร้าง helper กลางสำหรับ export download (headers, credentials, error, filename fallback).
2. ย้าย report exports ที่ยังใช้ `fetch(buildApiUrl(...))` ไปใช้ helper กลาง.
3. คงกรณี health probe เป็น exception ที่มีเหตุผล พร้อมเอกสารชัดเจน.

**Exit criteria:** เส้นทาง export ทั้งหมดใช้ helper เดียวกัน, error UX สอดคล้องกัน.

### Phase 3 — Visual token dedup (1-2 วัน)

เป้าหมาย: ลด tone/color mapping ซ้ำ

1. แยก shared KPI tone map/util ไป `src/lib` หรือ `src/components/ui`.
2. ย้าย report tiles ที่ซ้ำ logic ไปใช้ util เดียวกัน.
3. ตัด class mapping ซ้ำเฉพาะที่ไม่เพิ่มความหมายเชิงธุรกิจ.

**Exit criteria:** ไม่มี tone map ชุดเดิม copy มากกว่า 1 จุดใน report pages หลัก.

### Phase 4 — Backend standards follow-up (0.5-1 วัน)

เป้าหมาย: สอดคล้องกติกาเวลาและ audit

1. เปลี่ยน `DateTime.UtcNow` ตรงใน `iLearn.Application/Services/CourseVersionService.cs` ไปใช้ `_dateTime.Now`.
2. รัน test/build เฉพาะส่วนที่ได้รับผลกระทบ.
3. ตรวจซ้ำว่าไม่มีจุดเวลาใหม่ที่หลุดกฎในชั้น Application.

**Exit criteria:** ไม่มี direct `DateTime.UtcNow/Now` ใหม่ใน Application services ที่แตะ.

## Execution strategy

1. ทำทีละ phase แยก PR เพื่อ review ง่ายและ rollback ง่าย.
2. จำกัดขนาด PR ต่อรอบ:
   - Phase 1: ไม่เกิน 5 ไฟล์หลักต่อ PR.
   - Phase 2+: grouping ตาม capability (export, tokens, backend).
3. ทุก PR ต้องแนบ:
   - before/after grep snapshot
   - lint/build/test result
   - risk note ว่ามี user-facing behavior เปลี่ยนหรือไม่

## Risks and mitigations

1. **Risk:** เปลี่ยนปุ่มเยอะแล้ว behavior หลุด (disabled/loading/submit)
   - **Mitigation:** เริ่มจาก non-submit actions ก่อน, เพิ่ม smoke checklist ต่อหน้า.
2. **Risk:** export helper กลางเปลี่ยน flow แล้วไฟล์ดาวน์โหลดมีชื่อ/format ไม่ตรง
   - **Mitigation:** ทดสอบด้วย endpoint จริงทุก report หลัง migrate.
3. **Risk:** backend time source เปลี่ยนแล้ว assertion เวลาใน test เดิม fail
   - **Mitigation:** รัน test เฉพาะ service + update assertion ให้ยึด `_dateTime` abstraction.

## Acceptance criteria (plan-level)

1. มี roadmap phase-based ชัดเจนพร้อม exit criteria ต่อ phase.
2. มี guardrails ที่ตรวจอัตโนมัติได้ (ไม่พึ่ง manual review อย่างเดียว).
3. มีลำดับงานที่เริ่มจากจุดผลกระทบสูงและเสี่ยงต่ำก่อน.
4. มี verification matrix ที่ใช้ซ้ำได้ทุก PR ของ rollout นี้.

## Verification matrix (ใช้ทุก phase)

React Admin:

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Repo checks:

```powershell
git diff --check
```

Optional drift snapshots:

```powershell
rg -n "<button\\b" iLearn.Admin.React/src/pages iLearn.Admin.React/src/components
rg -n "fetch\\(" iLearn.Admin.React/src/pages
rg -n "DateTime\\.UtcNow|DateTime\\.Now" iLearn.Application/**/*.cs
```

## Implementation backlog candidates (ordered)

1. PR-A: Guardrails/lint for button + fetch patterns.
2. PR-B: Convert action buttons in `AssignmentDetailPage` + `BulkAssignPage`.
3. PR-C: Convert action buttons in `LearnerGroupDetailPage` + `CourseEditorPage`.
4. PR-D: Export download helper + migrate 2 report pages.
5. PR-E: KPI tone utility dedup across report pages.
6. PR-F: Backend `_dateTime` consistency in `CourseVersionService`.

## Notes

- แผนนี้ออกแบบให้ “หยุด regression ก่อน แล้วค่อยลด debt เดิม” เพื่อความยั่งยืน.
- หากต้องเริ่ม implement ให้เปิดแผนลูกของแต่ละ PR (PLAN-157+) ตามกติกา repo.

## Reviewer Notes (Claude Code, 2026-07-30)

ทิศทางถูก และ PLAN-157 แก้ปัญหาหลักของแผนนี้ไปแล้ว. ให้ถือ **PLAN-157 เป็น contract ที่ implement** และแผนนี้เป็น strategy/history เท่านั้น. บันทึกความคลาดเคลื่อนของ baseline ไว้กันอ้างซ้ำ:

1. **ลำดับ Phase 0 ผิด (PLAN-157 แก้แล้ว)** — Phase 0 exit criteria สั่งให้ lint fail เมื่อละเมิดกฎ `<button>` *ก่อน* Phase 1 migration แต่วันนี้มี native `<button>` อยู่ **19 จุด / 10 ไฟล์** ใน `src/pages/**` ⇒ เปิด enforcement ตอนนั้น = `npm run lint` แดงทั้ง repo ทันที. PLAN-157 §7 (report-only ก่อน แล้ว enforce หลัง Batch C) คือลำดับที่ถูก.
2. **Baseline #5 ผิดข้อเท็จจริง** — `alert(...)` ที่เขียนมือใน `iLearn.Admin/Views` มีแค่ 2 จุด คือ `Categories/Report.cshtml:396` และ `Learners/Profile.cshtml:710` ทั้งคู่ข้อความ `"Could not save the image."` = **image upload fail ไม่ใช่ report export fallback**. PLAN-157 ตัด legacy MVC ออกทั้งหมดแล้วจึงไม่ propagate.
3. **Baseline #3 เกินจริงเล็กน้อย** — KPI tone map ซ้ำ **2 ไฟล์เป๊ะ ๆ** (`AssignmentSummaryReportPage` / `LearnerGroupSummaryReportPage`) ไม่ใช่ “หลาย report pages”. ทั้งสองตัว byte-identical (tone `slate|indigo|emerald|rose`).
4. **Baseline #4 ถูก และแคบกว่าที่คิด** — `DateTime.UtcNow` ดิบใน `iLearn.Application/` ทั้งชั้นมีแค่ 2 จุด ทั้งคู่อยู่ `CourseVersionService.cs:200,581`. แก้ 2 จุดนี้ = Application layer สะอาดทั้งชั้น (แผนไม่ได้เคลมแรงขนาดนี้ แต่ควรรู้เพื่อวาง exit criteria).
5. **Phase 1 exit criteria “ลด 60%” วัดได้จริงแต่หลวม** — Batch A+B (5 ไฟล์ตามที่แผนระบุ) = 13/19 = 68% ⇒ เป้า 60% ทำได้. แต่เกณฑ์ absolute ของ PLAN-157 (ไม่เหลือ native button ที่ไม่ annotate) ดีกว่าเพราะไม่ต้องเถียงเรื่อง baseline snapshot.
6. **Verification matrix**: `rg -n "DateTime\.UtcNow|DateTime\.Now" iLearn.Application/**/*.cs` — glob `**/*.cs` ใน PowerShell ส่งให้ rg แล้วไม่ recurse ตามที่คาด ให้ใช้ `rg -n "DateTime\.(UtcNow|Now)" iLearn.Application` แทน.
