# PLAN-085: Upload Progress UI — แสดงความคืบหน้าอัพโหลด SCORM แบบละเอียด (React admin)

- **Status:** DONE → VERIFIED → help text FIXED (Claude Code 2026-07-14: 200MB→1GB)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **อ้างอิง:** [PLAN-084](PLAN-084-scorm-1gb-streaming-storage.md) (1GB — ทำคู่ขนานได้ **ห้ามแตะไฟล์ backend**), `src/lib/apiClient.ts`, `CourseEditorPage.tsx`, `ContentItemEditorPage.tsx`

> ผู้ใช้สั่ง (2026-07-14): "เพิ่มหน้า UI ในจังหวะที่รอการอัพโหลดให้ละเอียดขึ้น" — ตอนนี้อัพโหลด SCORM (จะใหญ่ถึง 1GB ตาม PLAN-084) เห็นแค่ปุ่ม loading spinner เฉย ๆ ไม่รู้ไปถึงไหน/เหลือเท่าไหร่/ค้างหรือยังวิ่ง

---

## ปัญหาปัจจุบัน

- ทั้ง `CourseEditorPage.tsx` (`saveContentItemsToVersion` → `fetchWithAccessControl` + FormData) และ `ContentItemEditorPage.tsx` ใช้ **fetch** ซึ่ง**ไม่มี upload progress event** — ผู้ใช้เห็นแค่ `AppButton loading` จนกว่าจะจบ
- ไฟล์ 1GB ใช้เวลาอัพโหลด + เวลาเซิร์ฟเวอร์แตก ZIP/validate อีกหลายสิบวินาที — ต้องแยก 2 จังหวะให้ผู้ใช้เห็น: **Uploading (มี %)** กับ **Processing on server (ไม่มี % แต่บอกว่ากำลังทำอะไร)**

## Scope

### 1. `uploadWithProgress` ใน `src/lib/apiClient.ts`

เพิ่ม util ใหม่ (อยู่ไฟล์เดียวกับ `fetchWithAccessControl` เพื่อ reuse `buildApiUrl`/`ApiError`):

```ts
type UploadPhase = 'uploading' | 'processing'
type UploadProgress = { phase: UploadPhase; loadedBytes: number; totalBytes: number; percent: number }

export const uploadWithProgress = <TResponse>(
  path: string,
  formData: FormData,
  options: { method?: 'POST' | 'PUT'; onProgress?: (p: UploadProgress) => void }
): { promise: Promise<TResponse>; abort: () => void }
```

- ใช้ **XMLHttpRequest** (fetch ไม่มี upload progress): `xhr.upload.onprogress` → phase `uploading` พร้อม loaded/total; `xhr.upload.onload` (ส่ง body ครบแล้ว รอเซิร์ฟเวอร์) → เปลี่ยน phase เป็น `processing`
- `xhr.withCredentials = true` + header `Accept: application/json` — **ห้าม set Content-Type เอง** (browser ใส่ multipart boundary ให้)
- Error mapping ให้เข้ากับ `ApiError` เดิม (status + statusText + responseBody) เพื่อให้ `getApiErrorText` เดิมใช้ได้ต่อ; รองรับ `abort()` → reject ด้วย error ที่แยกชนิดได้ (user cancelled)
- **พิเศษ 413:** โยน ApiError message ที่อ่านรู้เรื่อง เช่น `ไฟล์ใหญ่เกินลิมิตของเซิร์ฟเวอร์` (caller เอาไปแสดงต่อ)

### 2. Component `UploadProgressOverlay` (ไฟล์ใหม่ `src/components/shared/UploadProgressOverlay.tsx`)

Modal overlay (โครงเดียวกับ modal เดิมใน CourseEditorPage — `fixed inset-0 bg-slate-900/60 ...`) แสดงระหว่างอัพโหลด:

- **Phase `uploading`:** `ProgressBar` (shared component เดิม) + `{formatBytes(loaded)} / {formatBytes(total)}` + `{formatPercent(percent)}` — format ผ่าน `src/lib/format.ts` เท่านั้น ห้ามคำนวณ/round เอง; ชื่อไฟล์ที่กำลังส่ง (ถ้าหลายไฟล์ให้แสดงรวมเป็น total เดียว — FormData ส่งครั้งเดียวทั้งก้อนอยู่แล้ว)
- **Phase `processing`:** ProgressBar เต็ม/indeterminate + ข้อความ `Processing on server — extracting & validating SCORM package...` (จุดนี้ยกเลิกไม่ได้แล้ว — ซ่อนปุ่ม Cancel)
- ปุ่ม **Cancel** (AppButton variant danger, ใช้ `useConfirm` ก่อนยกเลิก) เฉพาะ phase uploading → เรียก `abort()`
- ข้อความเตือนตัวเล็ก: `อย่าปิดหน้านี้ระหว่างอัพโหลด` + ใส่ `beforeunload` guard ระหว่าง phase uploading/processing (ถอด listener เมื่อจบ)
- ห้าม hand-roll `<button>`/pill — ใช้ `AppButton`/`Badge`/`ProgressBar` ตามกติกา README

### 3. ผูกเข้า 2 หน้า

- **`CourseEditorPage.tsx`:** `saveContentItemsToVersion` — ถ้า FormData มีไฟล์ upload (`contentItems.some(i => i.source === 'upload' && i.file)`) ให้ใช้ `uploadWithProgress` + แสดง overlay; ถ้าไม่มีไฟล์ (แค่ reorder/เลือกของเดิม) ใช้ `fetchWithAccessControl` เดิม (ไม่ต้องมี overlay)
- **`ContentItemEditorPage.tsx`:** จุด submit ที่ส่ง FormData มีไฟล์ (ทั้ง create + update) — pattern เดียวกัน
- ระหว่าง overlay แสดง ปุ่ม submit เดิมคง disabled (state `saving` เดิม)

### 4. อัปเดต help text ขนาดไฟล์ (ประสานกับ PLAN-084)

- ตอนนี้ text บอก `200 MB / 500 MB expanded` (React `ContentItemEditorPage.tsx` ~177 + หน้า MVC — MVC **นอก scope แผนนี้**)
- **ทำเป็นขั้นสุดท้าย เฉพาะเมื่อ PLAN-084 สถานะ DONE แล้ว:** เปลี่ยนเป็น `1 GB (extracted up to 2.5 GB)` — ถ้า PLAN-084 ยังไม่ DONE ตอนปิดงานนี้ ให้**คงข้อความเดิม** แล้วจดใน Implementer Notes ว่า text ค้างรอ 084 (กันหน้าเว็บโฆษณา 1GB ทั้งที่ server ยังรับ 200MB)

## Contract

- ไม่มี API เปลี่ยน — endpoint/FormData shape เดิมทุกอย่าง (XHR แค่เปลี่ยนวิธีส่ง)
- ไฟล์ backend ห้ามแตะ (PLAN-084 ทำคู่ขนานโดย Copilot — กันชนกัน)

## นอก Scope (ห้ามทำ)

- ห้ามแตะ C# / web.config / deploy scripts
- ห้ามแตะหน้า MVC admin เดิม
- ห้ามทำ chunked/resumable upload (งานใหญ่ ไม่ได้สั่ง)
- ห้ามเปลี่ยน `fetchWithAccessControl` เดิม (เพิ่ม util ใหม่ข้าง ๆ เท่านั้น — โค้ดอื่นทั้งแอปใช้ตัวเดิมอยู่)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

ทดสอบมือ (dev proxy ไป QA หรือรัน API local):

1. อัพโหลดไฟล์ ~50MB (KSN.zip) → เห็น % วิ่ง + ตัวเลข MB/MB + เปลี่ยนเป็น Processing ก่อนจบ → success
2. Cancel ระหว่างอัพโหลด → confirm dialog → ยกเลิกจริง (network tab เห็น request aborted) + form กลับมาใช้ได้
3. ไฟล์เกินลิมิต → ข้อความ error อ่านรู้เรื่อง (ไม่ใช่ statusText ดิบ)
4. Save แบบไม่มีไฟล์ใหม่ (reorder อย่างเดียว) → ไม่มี overlay, flow เดิม
5. beforeunload guard ทำงานระหว่างอัพโหลด และถูกถอดหลังจบ (ปิดแท็บหลังจบได้เงียบ ๆ)

## Implementer Notes

- Created a progress-aware HTTP POST/PUT utility `uploadWithProgress` in `src/lib/apiClient.ts` using XMLHttpRequest.
- Built a beautiful overlay modal `UploadProgressOverlay` that registers a `beforeunload` event handler during upload/processing, shows upload statistics (MB/MB, %, filename), and handles cancellation with confirmation (using `useConfirm`).
- Integrated the overlay and new client in `ContentItemEditorPage.tsx` and `CourseEditorPage.tsx`.
- Verified that `npm run lint` and `npm run build` pass with 0 warnings/errors.
- Note: Backend unit test `ExtractAndParseScormAsync_RejectsArchiveThatExpandsBeyondAllowedSize` failed because of parallel work being done for PLAN-084 in the backend codebase (untracked files and local modifications exist). No backend code was touched in this plan.
- Kept the upload size labels as "200MB" in the UI because PLAN-084 is still in progress (not DONE yet).


## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็ม + lint/build เองซ้ำ:

- **`uploadWithProgress` (apiClient.ts):** XHR + `withCredentials` + `Accept: application/json` ไม่ set Content-Type เอง ✅; `upload.onprogress`→uploading, `upload.onload`→processing ✅; 413→ข้อความไทย, JSON error→message/error, abort→`isAborted` flag ✅; **`fetchWithAccessControl` เดิมไม่ถูกแตะ** (diff ทั้งไฟล์เป็น line-ending CRLF + เพิ่ม util ท้ายไฟล์เท่านั้น) ✅
- **`UploadProgressOverlay`:** ใช้ `AppButton`/`ProgressBar`/`useConfirm`/`formatBytes`/`formatPercent` ตามกติกา (ไม่ hand-roll button/pill, ไม่ format มือ) ✅; beforeunload guard add/remove ใน useEffect ✅; phase uploading = %+MB/MB+Cancel, processing = spinner+ซ่อน Cancel ✅; **`z-9999` ยืนยัน generate จริงเป็น `z-index:9999` ใน CSS build** (Tailwind 4 bare z-value) — ไม่ใช่ bug ✅
- **Integration 2 หน้า:** เงื่อนไข `hasFileUpload` → uploadWithProgress+overlay, else `fetchWithAccessControl` เดิม ✅; abort ref + isAborted handling + finally cleanup ครบ ✅; **ไม่มี client-side size cap** → ไม่บล็อกไฟล์ 1GB ที่ฝั่ง client ✅
- **Verify อิสระ:** `npm run lint` 0 warn, `npm run build` (tsc+vite) 0 err ✅

### Finding (MINOR — ต้องตามแก้ตอนนี้): help text ยัง "Max 200MB"
`ContentItemEditorPage.tsx:191` ยังเขียน `Max 200MB`. Gemini คงไว้ถูกต้องตาม §4 (ตอนทำ PLAN-084 ยังไม่ DONE) — **แต่ตอนนี้ PLAN-084 = DONE แล้ว** จึงถึงเวลา update เป็น `1 GB (extracted up to 2.5 GB)` ตาม §4. (หน้า MVC admin นอก scope — ถ้ามี help text 200MB ที่นั่นด้วย เปิดงานแยก)

### Gap เดิม (เหมือนทุกแผนรอบนี้): manual click-through บนเบราว์เซอร์ยังทำไม่ได้ (backend/API ไม่รันในสภาพแวดล้อมนี้) — checklist 5 ข้อ (progress วิ่ง/cancel/error/no-overlay/beforeunload) ต้องทดสอบมือบน dev ที่ต่อ QA ก่อนปิดสมบูรณ์

**สรุป: โค้ดผ่านรีวิว — ครบสเปค ไม่มี regression. เหลือ update help text → 1GB (084 DONE แล้ว) + manual click-through**
