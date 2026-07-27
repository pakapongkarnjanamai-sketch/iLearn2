# Implementation Plans

แผนงานที่ Claude Code (planner) เขียนให้ implementer (Antigravity/Gemini, GitHub Copilot/GPT) รับไปทำ

## กติกาการจองเลขแผน (สำคัญ — เคยชนกันมาแล้ว)

1. **ก่อนสร้างแผนใหม่** ใช้ `pwsh tools/plan-status.ps1 -Next` (สแกนไฟล์จริงในโฟลเดอร์นี้ **รวมไฟล์ที่ยังไม่ commit** — agent อื่นอาจเพิ่งสร้างค้างไว้ใน working tree) แล้วใช้เลขที่ได้
2. **หนึ่งเลข = หนึ่งแผน** — ถ้าสร้างแล้วพบว่าชนกับของ agent อื่น: **ใครสร้างไฟล์ก่อนได้เลขไป** ตัวที่สร้างทีหลังย้ายไปเลขถัดไปที่ว่าง + จดหมายเหตุการย้ายไว้หัวไฟล์ + อัปเดต reference ใน `AGENT_LOG.md`
3. **เอกสารประกอบของแผนเดียวกัน** ใช้เลขร่วมกันได้โดยตั้งใจ (เช่น `PLAN-045-production-cutover` + `PLAN-045-data-mapping` + `PLAN-045-etl-catalog.sql` = initiative เดียว) — ตั้งชื่อ slug ให้สื่อว่าเป็นเอกสารประกอบ ไม่ใช่แผนแยก
4. เลขซ้ำที่เป็นประวัติศาสตร์ (ปิดงานไปแล้ว): `PLAN-030` มี 2 ไฟล์คนละเรื่อง (dead-css-cleanup / dead-code-scan) — **คงไว้ตามเดิม** เพราะถูกอ้างใน log/commit เก่า ห้ามสับสนกับกติกาข้อ 3

## Workflow

1. Claude สร้างไฟล์ `PLAN-NNN-<slug>.md` สถานะ `READY` พร้อมระบุ Assigned
   * **หมายเหตุ:** เมื่อเกิดความต้องการหรือคำสั่งให้แก้ไขเพิ่มเติมในรอบใหม่ (Iteration/Feedback Loop) ให้สร้างไฟล์ `PLAN` ตัวใหม่เสมอ หลีกเลี่ยงการแก้ไขหรืออัปเดตข้อมูลบนไฟล์ `PLAN` เดิม
2. Implementer ที่ถูก assign เปิดแผน → ทำตาม Scope → รัน Verification → เปลี่ยนสถานะเป็น `DONE` + เติม Implementer Notes → ลง `DOC/AGENT_LOG.md`
3. Claude รีวิว diff หลังทำเสร็จ ถ้าผ่านเปลี่ยนเป็น `VERIFIED` ถ้าไม่ผ่านเปลี่ยนกลับเป็น `READY` พร้อมหมายเหตุ

สถานะ: `DRAFT` → `READY` → `IN PROGRESS` → `DONE` → `VERIFIED`

**แผนที่ค้างที่ `DONE` = หนี้รีวิว** (ไม่มีใครยืนยันว่างานถูก) — ดูคิวด้วย `pwsh tools/plan-status.ps1 -Debt`

## กติกาเขียนบรรทัด Status (สำคัญ — เคยเพี้ยนจนอ่านด้วยสคริปต์ไม่ได้)

เขียนแบบนี้เป๊ะ ๆ **state คำเดียว ไม่มีคำอธิบายต่อท้าย**:

```markdown
- **Status:** VERIFIED
```

ห้ามเขียนแบบที่เคยเจอ (ทำให้ query สถานะไม่ได้):
`- **Status**: DONE` (ดอกจันผิดที่) · `- **สถานะ:** DONE` (คีย์ภาษาไทย) · `Status: VERIFIED` (ไม่มี bullet/bold) ·
`- **Status:** VERIFIED ✅ (Claude review 2026-06-16: enum 3 ค่า, badge tone ...)` ← **คำอธิบายยาวให้ไปไว้หัวข้อ Reviewer Notes ท้ายไฟล์**

ตรวจว่าไฟล์ตัวเองอ่านได้ไหม: `pwsh tools/plan-status.ps1` (ท้ายผลลัพธ์จะลิสต์ไฟล์ที่ header เพี้ยน)

## Template

```markdown
# PLAN-NNN: <ชื่องาน>

- **Status:** READY
- **Assigned:** Gemini | GPT
- **Reviewer:** Claude Code
- **Priority:** High | Medium | Low
- **Estimated scope:** <จำนวนไฟล์/ขนาดงานโดยประมาณ>

## Problem
<ปัญหาคืออะไร ทำไมต้องทำ — เขียนให้คนไม่มี context อ่านรู้เรื่อง>

## Scope (ทำแค่นี้)
<รายการสิ่งที่ต้องแก้ ระบุไฟล์+บรรทัด/ฟังก์ชันชัดเจน>

## Out of scope (ห้ามแตะ)
<สิ่งที่อาจเผลอไปทำแต่ไม่ต้อง>

## Acceptance criteria
<เช็คลิสต์วัดว่าเสร็จจริง>

## Verification
<คำสั่งที่ต้องรันผ่าน>

## Implementer Notes
<implementer เติมหลังทำเสร็จ>
```
