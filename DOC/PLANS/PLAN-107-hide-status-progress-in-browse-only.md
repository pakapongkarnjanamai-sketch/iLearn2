# PLAN-107: โหมด "ดูอย่างเดียว" ไม่ต้องแสดงสถานะและ progress bar

- **Status:** READY
- **Assigned:** GitHub Copilot (เจ้าของ `Player.cshtml` ปัจจุบันต่อจาก 105/106)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ที่มา:** ผู้ใช้เปิดคอร์สที่ไม่ได้ถูก assign (`courseId=876`, learner 430339) — header ขึ้น `สถานะ: ดูอย่างเดียว` + progress bar **0%** ซึ่งไม่มีความหมาย เพราะไม่มี enrollment ให้ติดตามความคืบหน้า

---

## บริบท (ยืนยันจากโค้ดแล้ว — จุดที่พลาดง่ายที่สุด)

`isReadOnly` ฝั่ง client เป็น **OR ของสองเคสที่ต่างกันสิ้นเชิง** ([Player.cshtml](../../iLearn.User/Views/MyLearning/Player.cshtml) ~1420):

```js
if (currentData.isReadOnly === true || currentData.isCompleted === true) { isReadOnly = true; setupReadOnlyMode(); }
```

| เคส | เงื่อนไข | สถานะที่แสดง | progress | ควรทำอย่างไร |
| --- | --- | --- | --- | --- |
| **A — เปิดดูเฉย ๆ (ไม่ถูก assign)** | `currentData.isReadOnly === true` | `ดูอย่างเดียว` (เหลือง) | **0% เสมอ** | **ซ่อนทั้งคู่** ← งานนี้ |
| **B — เรียนจบแล้ว กลับมาทบทวน** | `currentData.isCompleted === true` | `เรียนจบแล้ว` (เขียว) | **100%** | **ต้องแสดงต่อ** — เป็นผลการเรียนจริงของผู้เรียน |

**API ยืนยัน:** `EnrollmentsController` ตั้ง `isReadOnly = true` เฉพาะใน `else` ของ `enrollment == null` (บรรทัด ~248-250) ⇒ **`currentData.isReadOnly === true` ⟺ ไม่มี enrollment ⟺ ไม่มีอะไรให้ติดตาม**

> ⚠️ **ห้ามใช้ตัวแปร `isReadOnly` (ตัว OR) เป็นเงื่อนไขซ่อน** — จะไปซ่อนผลการเรียน 100% ของคนที่เรียนจบแล้วด้วย ซึ่งผิดเจตนา

- `#readOnlyBadge` ("View Only Mode") ที่โชว์กลางจอ**สื่อโหมดอยู่แล้ว** ⇒ แถว `สถานะ: ดูอย่างเดียว` เป็นข้อมูลซ้ำซ้อน
- `recalcTotalProgress` ข้ามการอัปเดต per-item bar อยู่แล้วเมื่อ read-only (`if (!isReadOnly)`) ⇒ แถบใน TOC เป็นเส้นเทาว่างเปล่าตลอด

## Scope (แก้เฉพาะ `Player.cshtml`)

### 1. เพิ่ม hook ให้แถวสถานะ

markup ปัจจุบันแถวสถานะไม่มี id (มีแค่ pill ข้างใน) ⇒ เพิ่ม id ที่ **แถว**:

```html
<div class="course-meta-row" id="courseStatusRow">
    <span class="course-meta-label">สถานะ</span>
    <span class="course-status-pill status-muted" id="courseStatusDisplay">-</span>
</div>
```
- **ห้ามแตะ `id="courseStatusDisplay"`** และ class `course-status-pill status-*` — `setCourseStatusDisplay` ผูกอยู่

### 2. ติด class เฉพาะเคส A ใน `setupReadOnlyMode()`

```js
if (currentData.isReadOnly === true) {
    $("#tocSection").addClass("browse-only");
}
```
- ใช้ `currentData.isReadOnly` (ค่าดิบจาก API) **ไม่ใช่**ตัวแปร `isReadOnly` — ตามตารางด้านบน
- ตั้งชื่อ class ใหม่ `browse-only` **อย่าใช้ `view-only-mode`ซ้ำ** (ตัวนั้นถูกใส่ที่ `#courseTocList` ด้วยเงื่อนไข OR อยู่แล้ว คนละความหมาย)

### 3. CSS ซ่อน

```css
#tocSection.browse-only #courseStatusRow,
#tocSection.browse-only .course-progress-row,
#tocSection.browse-only .item-progress-track {
    display: none;
}
```
- ใช้ CSS class **ไม่ใช่ `.hide()`** เพื่อไม่ให้โดน `.show()` ที่อื่นเปิดกลับโดยบังเอิญ
- รวม `.item-progress-track` (แถบใต้แต่ละบทใน TOC) ด้วย เพราะในเคส A มันว่างเปล่าตลอด — ถ้าผู้ใช้อยากให้เหลือไว้ให้ตัดบรรทัดนี้ออกแล้วจดใน Notes
- ไม่ต้องแก้ JS ที่เขียนค่าลง `#courseStatusDisplay`/`#courseProgressFill` (เขียนลง element ที่ซ่อนอยู่ ไม่มีผลเสีย) — ลด surface การแก้

### 4. ตรวจระยะห่างหลังซ่อน

`.course-meta-list` มี `margin-bottom: 10px` และ `.course-header-panel` มี padding เดิม ⇒ เมื่อ progress row หายไป header จะสั้นลง **ตรวจว่าไม่มีช่องว่างค้างแปลก ๆ** ปรับ margin ได้ถ้าจำเป็น (ไม่บังคับ)

## Contract ที่เปลี่ยน

- API / DB / migration: **ไม่มี**
- DOM: เพิ่ม `id="courseStatusRow"` (additive) + class `browse-only` — IDs เดิมทั้ง 7 คงครบ

## นอก Scope (ห้ามทำ)

- **ห้ามซ่อนสถานะ/progress ของเคส B (เรียนจบแล้ว)** — เป็นผลการเรียนจริง ต้องเห็น
- ห้ามซ่อนแถว `ผู้เรียน`/`หมวดหมู่`/`ประเภท` (ยังมีประโยชน์ในโหมดดูอย่างเดียว)
- ห้ามซ่อน/แก้ `#readOnlyBadge` — เป็นตัวสื่อโหมดหลัก
- ห้ามแตะ `setCourseStatusDisplay` / `setCourseProgressDisplay` / `recalcTotalProgress` logic
- ห้ามแตะงานของ 104 §C (session timer), 105 (commit queue), 106 (auto summary)

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (QA):
1. **เคส A** — เปิดคอร์สที่**ไม่ได้ถูก assign** (เช่น `Player?courseId=876` ด้วย learner 430339) → **ไม่มีแถว `สถานะ` และไม่มี progress bar**; ยังเห็น `ผู้เรียน/หมวดหมู่/ประเภท` และป้าย `View Only Mode` กลางจอ
2. **เคส B (สำคัญ — regression ที่ต้องกันให้ได้)** — เปิดคอร์สที่**เรียนจบแล้ว** (เช่นของ learner 430339 ที่ผ่าน course 540) → **ยังต้องเห็น `สถานะ: เรียนจบแล้ว` (เขียว) และ progress 100%**
3. **เคสปกติ** — คอร์สที่กำลังเรียนอยู่ (มี enrollment ยังไม่จบ) → เห็นสถานะ + progress ตามปกติ ไม่กระทบ
4. TOC: เคส A ไม่มีแถบ progress ใต้แต่ละบท; เคส B/ปกติ ยังมีตามเดิม
5. console 0 error

## Deploy note

แตะเฉพาะ **iLearn.User** → deploy learner อย่างเดียว ไม่มี migration

## Implementer Notes

_(เติมโดย implementer)_
