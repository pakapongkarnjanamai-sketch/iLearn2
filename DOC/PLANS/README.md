# Implementation Plans

แผนงานที่ Claude Code (planner) เขียนให้ implementer (Antigravity/Gemini, GitHub Copilot/GPT) รับไปทำ

## Workflow

1. Claude สร้างไฟล์ `PLAN-NNN-<slug>.md` สถานะ `READY` พร้อมระบุ Assigned
   * **หมายเหตุ:** เมื่อเกิดความต้องการหรือคำสั่งให้แก้ไขเพิ่มเติมในรอบใหม่ (Iteration/Feedback Loop) ให้สร้างไฟล์ `PLAN` ตัวใหม่เสมอ หลีกเลี่ยงการแก้ไขหรืออัปเดตข้อมูลบนไฟล์ `PLAN` เดิม
2. Implementer ที่ถูก assign เปิดแผน → ทำตาม Scope → รัน Verification → เปลี่ยนสถานะเป็น `DONE` + เติม Implementer Notes → ลง `DOC/AGENT_LOG.md`
3. Claude รีวิว diff หลังทำเสร็จ ถ้าผ่านเปลี่ยนเป็น `VERIFIED` ถ้าไม่ผ่านเปลี่ยนกลับเป็น `READY` พร้อมหมายเหตุ

สถานะ: `DRAFT` → `READY` → `IN PROGRESS` → `DONE` → `VERIFIED`

## Template

```markdown
# PLAN-NNN: <ชื่องาน>

- **Status:** READY
- **Assigned:** Gemini | GPT
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
