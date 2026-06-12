# PLAN-003: ปรับกลุ่ม Learning ใน sidebar ให้เหลือ 3 รายการ

- **Status:** VERIFIED
- **Assigned:** Gemini
- **Priority:** Medium
- **Estimated scope:** 1 ไฟล์ (`navigation.ts`)

## Problem

กลุ่ม **Learning** ใน sidebar (`iLearn.Admin.React/src/config/navigation.ts`, section `label: 'Learning'` บรรทัด ~49-58) ปัจจุบันมี 5 รายการ:

```ts
{ label: 'Courses', path: '/courses', icon: BookOpen },
{ label: 'Content Library', path: '/content-library', icon: Library },
{ label: 'Assignments', path: '/assignments', icon: ClipboardList },
{ label: 'Learner Groups', path: '/learner-groups', icon: Users },
{ label: 'Learners', path: '/learners', icon: UserRound },
```

ผู้ใช้ต้องการให้กลุ่ม Learning เหลือเฉพาะแกนหลักการเรียน 3 รายการ: **Courses, Assignments, Learner Groups** ส่วน **Content Library** และ **Learners** ให้ย้ายไปอยู่กลุ่ม **Operations** (route ทั้งสองยังต้องเข้าถึงได้จาก sidebar เหมือนเดิม — แค่ย้ายกลุ่ม ไม่ใช่ลบ)

## Scope (ทำแค่นี้)

แก้ไฟล์เดียว `iLearn.Admin.React/src/config/navigation.ts`:

1. **กลุ่ม Learning** (`label: 'Learning'`): เหลือ 3 item ตามลำดับนี้
   - Courses (`/courses`, BookOpen)
   - Assignments (`/assignments`, ClipboardList)
   - Learner Groups (`/learner-groups`, Users)
2. **กลุ่ม Operations** (`label: 'Operations'`): เพิ่ม Content Library กับ Learners เข้าไป (วางก่อน Learning Logs หรือหลังก็ได้ ขอให้ icon เดิม)
   - Content Library (`/content-library`, Library)
   - Learners (`/learners`, UserRound)
   - Learning Logs (`/learning-logs`, FileText) — ของเดิม คงไว้
3. ตรวจ import ของ `lucide-react` ด้านบนไฟล์: ทุก icon (`Library`, `UserRound`, ฯลฯ) ยังถูกใช้อยู่ (แค่ย้ายกลุ่ม ไม่มี icon ไหนหลุดการใช้งาน) — ไม่ต้องแก้ import แต่อย่าเผลอลบ

## Out of scope (ห้ามแตะ)

- ห้ามแก้ route ใน `App.tsx` — แค่ย้ายรายการใน sidebar เท่านั้น path เดิมทั้งหมด
- ห้ามแตะกลุ่ม Super Admin, Dashboard
- ห้ามเปลี่ยน label/path/icon ของรายการ (ย้ายอย่างเดียว)
- ห้ามแก้ logic role filter หรือ type ของ `NavigationSection`/`NavigationItem`

## Acceptance criteria

- [x] กลุ่ม Learning ใน sidebar แสดงแค่ Courses, Assignments, Learner Groups (ตามลำดับนี้)
- [x] กลุ่ม Operations แสดง Content Library, Learners, Learning Logs (เข้าถึง `/content-library` และ `/learners` ได้ตามเดิม)
- [x] ไม่มี TypeScript error เรื่อง import icon ที่ไม่ถูกใช้
- [x] กลุ่ม Super Admin และ Dashboard ไม่เปลี่ยน

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด sidebar ดูว่ากลุ่ม Learning เหลือ 3 รายการ, Operations มี Content Library/Learners/Learning Logs, คลิกเข้าทั้ง `/content-library` และ `/learners` ได้

## Implementer Notes

- แก้ไขไฟล์ `iLearn.Admin.React/src/config/navigation.ts` โดยย้ายรายการ `Content Library` และ `Learners` ออกจาก `Learning` section ไปไว้ใน `Operations` section ด้านหน้าของ `Learning Logs`
- ลำดับรายการในแถบเครื่องมือ:
  - **Learning:** Courses $\rightarrow$ Assignments $\rightarrow$ Learner Groups
  - **Operations:** Content Library $\rightarrow$ Learners $\rightarrow$ Learning Logs
- ตรวจสอบแล้วว่าไอคอนเดิมทั้งหมด (`Library` และ `UserRound`) ยังถูกใช้ในเมนูตามปกติ ไม่เกิดปัญหา TypeScript unused import
- รันตรวจสอบ lint/build และ backend test ทั้งหมดผ่านสมบูรณ์ 100%
- **[Claude/planner review 2026-06-12]** ✅ VERIFIED — diff ตรง scope: Learning เหลือ Courses/Assignments/Learner Groups, Operations เพิ่ม Content Library+Learners ก่อน Learning Logs, path/icon เดิมทุกตัว, ไม่แตะ App.tsx/Super Admin — lint (0 error) + build ผ่าน
