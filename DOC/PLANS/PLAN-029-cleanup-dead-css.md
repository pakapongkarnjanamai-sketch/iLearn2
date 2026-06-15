# PLAN-029: ลบ dead CSS ใน index.css (class/keyframe ที่ไม่ถูกใช้)

- **Status:** DONE
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Low
- **Estimated scope:** 1 ไฟล์หลัก (`src/index.css`) + sync `DOC/ux_ui_analysis.md` ถ้าจำเป็น

## Problem

`iLearn.Admin.React/src/index.css` (324 บรรทัด, CSS เดียวของโปรเจกต์) มี custom class/keyframe ที่ **define แต่ไม่ถูกใช้เลย** (grep className ใน `src/**/*.tsx,ts` = 0) — เป็น dead code

**ยืนยันแล้วว่าไม่ถูกใช้ (0 references):**
| รายการ | ชนิด |
|---|---|
| `.neon-glow-dot` | class |
| `@keyframes neon-glow` | keyframe (ใช้โดย neon-glow-dot เท่านั้น) |
| `.selected-floating-badge` | class |
| `@keyframes badge-pulse` | keyframe (ใช้โดย selected-floating-badge) |
| `@keyframes badge-fade-slide-in` | keyframe (ใช้โดย selected-floating-badge) |
| `.overflow-wrap-anywhere` | class |
| `.wiz-section` | class |
| `.wiz-section-title` | class |

**เก็บไว้ (ใช้จริง):** `.custom-scrollbar`(15), `.wiz-input`(9), `.wiz-label`(10), `.modal-overlay`/`.modal-window`(6), `.modal-window-lg`(2), `.premium-hover-row`(1), `.font-mono`, keyframes `fade-in`/`scale-in`/`modal-fade-in`/`modal-scale-in`

## Scope (ทำแค่นี้)

1. ลบ class + keyframe ที่อยู่ในตาราง "ไม่ถูกใช้" ออกจาก `src/index.css`
2. **ก่อนลบแต่ละ keyframe** — grep ในตัว `index.css` เองว่า keyframe นั้นไม่ได้ถูก `animation:` ของ class ที่**เก็บไว้**อ้างถึง (เช่น `neon-glow`/`badge-pulse`/`badge-fade-slide-in` ควรถูกอ้างเฉพาะโดย class ที่จะลบ) — ถ้าพบว่ามี class ที่เก็บไว้ใช้ ให้คงไว้ + จดใน Notes
3. **ยืนยันซ้ำก่อนลบทุก class** — grep ทั้ง `src` (`*.tsx`,`*.ts`) ว่า class name นั้น = 0 จริง (กันเคสที่ผมพลาด เช่นถูกใส่แบบ dynamic string)
4. ถ้าลบ `.wiz-section` / `.wiz-section-title` → **อัปเดต `DOC/ux_ui_analysis.md` §2.3** ที่ระบุคลาสเหล่านี้เป็นมาตรฐาน wizard (ตอนนี้ doc พูดถึงแต่โค้ดไม่ใช้) ให้สะท้อนความจริง (ลบรายการ หรือหมายเหตุว่า deprecated)

## Out of scope (ห้ามแตะ)

- ห้ามลบ/แก้ class ที่ใช้จริง (ตามรายการ "เก็บไว้")
- ห้ามแก้ Tailwind config / @theme / CSS variables (token สี/ฟอนต์)
- ห้ามแตะ `.font-mono` override (ตั้งใจ override ตาม ux_ui_analysis §1)
- ห้าม "จัดระเบียบ/format" ส่วนอื่นของ index.css ที่ไม่เกี่ยวกับ dead code (กัน diff บวม/พลาด)

## Acceptance criteria

- [x] class/keyframe ที่ไม่ถูกใช้ (8 รายการ) ถูกลบจาก `index.css`
- [x] grep ยืนยัน: class ที่ลบ = 0 references ใน `src`; keyframe ที่ลบไม่ถูก `animation:` ใด ๆ ที่เหลืออ้างถึง
- [x] class ที่ใช้จริงยังอยู่ครบ — UI ไม่เพี้ยน (custom-scrollbar, wizard inputs, modals, hover row, neon dot ที่... ไม่มีแล้ว)
- [x] ux_ui_analysis §2.3 sync กับความจริง (ถ้าลบ wiz-section)
- [x] `npm run build` ผ่าน (Tailwind/Vite ไม่ error)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run build
npm run lint
```
ทดสอบ manual: เปิดหน้าที่ใช้ของที่เก็บไว้ — ตาราง (custom-scrollbar), wizard create (wiz-input/label), modal (เช่น confirm dialog/learner group folder), dashboard live dot — ดูว่ายังปกติ (ของที่ลบไม่มีผลเพราะไม่ถูกใช้อยู่แล้ว)

## Implementer Notes

- ลบ dead CSS ครบ 8 รายการจาก `iLearn.Admin.React/src/index.css`:
	- classes: `.neon-glow-dot`, `.selected-floating-badge`, `.overflow-wrap-anywhere`, `.wiz-section`, `.wiz-section-title`
	- keyframes: `neon-glow`, `badge-pulse`, `badge-fade-slide-in`
- ตรวจซ้ำใน `index.css` ก่อนลบ keyframe แล้วพบว่า `neon-glow`, `badge-pulse`, `badge-fade-slide-in` ถูกอ้างโดย class ที่ลบออกเท่านั้น (ไม่มี class ที่เก็บไว้เรียกใช้งาน)
- grep ยืนยัน class ที่ลบแล้วไม่ถูกเรียกใน `src/**/*.ts,tsx` (0 references)
- sync เอกสาร `DOC/ux_ui_analysis.md` ที่ §2.3 โดยลบรายการ `.wiz-section`/`.wiz-section-title` ออกจากมาตรฐาน wizard
- Verification:
	- `npm run lint` ผ่าน
	- `npm run build` ผ่าน
