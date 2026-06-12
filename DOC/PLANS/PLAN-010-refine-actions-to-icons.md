# PLAN-010: Refine Row Actions to Use Icons - Convert Reset Button to Icon Button

- **Status:** DONE
- **Assigned:** GPT
- **Priority:** Medium
- **Estimated scope:** แก้ไข 1 ไฟล์ (`AssignmentDetailPage.tsx`)

## Problem

ในหน้า `AssignmentDetailPage.tsx` ส่วนตารางรายชื่อผู้เรียน (Learners table) คอลัมน์ปุ่มดำเนินการ (Actions) ปุ่มสำหรับรีเซ็ตสิทธิ์การเรียน (`Reset`) ถูกแสดงผลเป็นปุ่มข้อความแบบมีกรอบ (Text Button) ในขณะที่ปุ่มลบ (`Trash2`) และปุ่มดำเนินการในหน้าจออื่น ๆ ของระบบเป็นปุ่มรูปไอคอน (Icon Button) ทั้งหมด 

เพื่อสร้างมาตรฐานด้าน UI/UX และความเป็นระเบียบเรียบร้อยของหน้าจอแบบ Premium slate/indigo จะต้องปรับปรุงปุ่ม Reset ดังกล่าวให้เปลี่ยนมาใช้งานในรูปแบบปุ่มไอคอนเหมือนกับจุดอื่น ๆ ในระบบ

## Scope (ทำแค่นี้)

### 1. ปรับปรุง AssignmentDetailPage.tsx

#### [MODIFY] [AssignmentDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx)
- นำเข้าไอคอน `RotateCcw` จาก `lucide-react`
- เปลี่ยนแปลงโค้ดส่วนเรนเดอร์ปุ่ม Reset ในแถวตารางรายชื่อผู้เรียน (บรรทัด ~417-423) จาก:
  ```tsx
  <button
    onClick={() => handleResetLearner(l.learnerCode)}
    className="px-2 py-1 bg-slate-50 text-slate-600 border border-slate-200 rounded text-xxs font-semibold hover:bg-slate-100 transition"
    title="Reset attempts"
  >
    Reset
  </button>
  ```
  ให้เปลี่ยนเป็นรูปแบบ Icon button:
  ```tsx
  <button
    onClick={() => handleResetLearner(l.learnerCode)}
    className="p-1 text-slate-400 hover:text-indigo-600 rounded transition cursor-pointer"
    title="Reset attempts"
  >
    <RotateCcw className="h-4 w-4" />
  </button>
  ```
- ทดสอบความสวยงาม การวางตำแหน่ง และระยะห่าง (gap) ร่วมกับปุ่มถังขยะด้านข้างให้สมดุลและเท่ากัน

## Out of scope (ห้ามแตะ)
- ห้ามดัดแปลง Logic การ Reset หรือยิง API
- ห้ามแก้ตารางหรือโมดูลอื่น ๆ

## Acceptance criteria
- [x] ปุ่ม Reset ในตารางผู้เรียนของหน้าแสดงรายละเอียด Assignment เปลี่ยนเป็นรูปไอคอนลูกศรหมุนกลับ (`RotateCcw`)
- [x] สไตล์ ขนาด ความสูง และ hover effects ของปุ่ม Reset สอดคล้องและกลมกลืนกับปุ่มถังขยะ (`Trash2`) ข้างเคียง
- [x] การสร้างความสัมพันธ์และ API call ของปุ่ม Reset ยังคงทำงานปกติเมื่อถูกคลิก

## Verification
```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes
- ปรับเฉพาะจุดตามขอบเขตแผน: เปลี่ยนปุ่ม Reset ในตาราง Learners ของ `AssignmentDetailPage.tsx` จาก text button เป็น icon button ด้วย `RotateCcw`
- คง logic เดิมทั้งหมดของ `handleResetLearner` และ API call (`Assignments/{id}/reset-enrollments`) โดยไม่แตะ flow ยืนยัน/รีโหลดข้อมูล
- สไตล์ปุ่ม Reset ปรับเป็น `p-1 text-slate-400 hover:text-indigo-600 rounded transition cursor-pointer` ให้กลมกลืนกับปุ่มถังขยะข้างเคียงที่ใช้ icon button pattern เดียวกัน
- Verification ผ่าน: `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน
