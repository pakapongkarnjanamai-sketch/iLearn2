# PLAN-097: Player iPad UX — lifecycle flush + pseudo-fullscreen + sidebar กระชับ/พับได้ + touch เก็บตก (iPad readiness ชุด B+C)

- **Status:** QA DEPLOYED (code + local runtime ผ่าน, 1 observation ไม่บล็อก — เหลือ iPad smoke ก่อน VERIFIED)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-17
- **ที่มา:** rollout learner บน iPad — ผู้ใช้รายงานเอง 2 อาการ: (1) กด Fullscreen แล้ว "เลื่อนขึ้นลงทำให้หลุดโหมด" (2) อยากได้ info-section กระชับขึ้นเพื่อคืนพื้นที่เนื้อหา + ผลรีวิว readiness (ความเสี่ยง progress หายบน iPad)
- **คู่ขนานกับ:** [PLAN-096](PLAN-096-learner-web-performance.md) (Copilot) — **ไฟล์ไม่ชนกัน:** แผนนี้เป็นเจ้าของ `Player.cshtml`, `MyLearning/Index.cshtml`, `Home/Index.cshtml`, `MyLearningController.cs` / **ห้ามแตะ** `_DevExtremeLayout.cshtml` + `Program.cs` (ของ 096) — ข้อยกเว้นที่ตกลงแล้ว: 096 จะลบ `const baseUrl` 1 บรรทัดใน Player.cshtml (~697) ถ้าเจอว่าหายไปแล้วคือถูกต้อง อย่า revert
- ผู้ใช้เลือกแบบ sidebar (2026-07-17): ยุบ header เหลือ ~110px + ลดกว้าง 320px + ปุ่มพับ — ตาม mockup ที่อนุมัติในแชท

---

## บริบท (ยืนยันจากโค้ดแล้ว)

- **iPadOS Safari ไม่ยิง `beforeunload` อย่างน่าเชื่อถือ** (นโยบาย WebKit) แต่ Player flush runtime ตอนออกจากหน้าไว้ที่ `beforeunload` จุดเดียว (Player.cshtml `$(function(){...})`) ⇒ บน iPad การกด Home/สลับแอป/ปิดแท็บ = session time + suspend data ล่าสุดหาย — `commitRuntimeContentItems` มี option `useBeacon` (navigator.sendBeacon) พร้อมใช้อยู่แล้ว
- **อาการหลุด fullscreen เมื่อเลื่อน:** iPadOS สงวน gesture ปัดลงเป็น "ออกจาก fullscreen" — เมื่อ SCORM ใน iframe เลื่อนชนขอบเกิด rubber-band ระบบตีความเป็นการออกจากโหมด แก้จากฝั่งเราด้วย CSS ไม่ได้เพราะตัว scroll คือเอกสารใน iframe (แพ็กเกจ third-party) ⇒ ต้องเลี่ยงไปใช้ pseudo-fullscreen บนอุปกรณ์ touch; แถม native fullscreen บน iPadOS < 16.4 ต้องใช้ prefix `webkit*` (โค้ดปัจจุบันไม่มี fallback = ปุ่มเงียบ) และคีย์บอร์ด (Exam มีช่องกรอก) ก็ทำหลุดโหมดได้อีกทาง
- **Sidebar (`--sidebar-width: 400px`)** = 39% ของ iPad แนวนอน 1024px; ส่วนหัว `course-header-panel` (eyebrow + title + กล่อง meta 2×2 + กล่อง progress) สูง ~300px ≈ ครึ่งหนึ่งของ sidebar ⇒ เห็นรายการเนื้อหาแค่ ~2 รายการ
- **Cookie learner: sliding 30 นาที** — ต่ออายุเฉพาะเมื่อมี request; SCORM ที่ไม่ commit ระหว่างเรียน (เช่นบทเรียนวิดีโอยาว) ⇒ commit ตอนจบโดน 440 เด้ง login กลางคัน — **ห้ามยืดอายุ cookie** (iPad ใช้ร่วมกันหลายคน ปล่อย session ค้างนานเป็นความเสี่ยง) ⇒ ใช้ keep-alive ping เฉพาะตอน Player เปิดและแท็บ visible
- รูป fallback การ์ดคอร์สชี้ `images.unsplash.com` (MyLearning/Index.cshtml ~1087, ~1425) — iPad ในเน็ตโรงงานออกอินเทอร์เน็ตไม่ได้ ⇒ request แขวนจน timeout
- SCORM adapter log `console.log` ทุก GetValue/SetValue — content บางตัว poll ถี่ = overhead จริง + ข้อมูลผู้เรียนไหลลง console
- JS ของ Player ผูกกับ element IDs: `courseTitleDisplay, learnerCodeDisplay, categoryNameDisplay, courseTypeNameDisplay, courseStatusDisplay, courseProgressFill, courseProgressDisplay` และ `setCourseStatusDisplay` toggle class `status-muted/success/danger/warning` บน `#courseStatusDisplay` — **rework markup ต้องคง IDs + class hook เหล่านี้ทุกตัว**

## Scope

### B1. Flush runtime ให้รอดบน iPad (`Player.cshtml`)

คงของเดิมทั้งหมด แล้ว**เพิ่ม** 2 listener ข้าง `beforeunload` เดิม:

```js
window.addEventListener("pagehide", function () {
    flushSelectedContentItemRuntime({ includeSessionTime: true, useBeacon: true, reason: "pagehide" });
});
document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === "hidden") {
        flushSelectedContentItemRuntime({ includeSessionTime: true, useBeacon: true, reason: "visibility-hidden" });
    }
});
```

- **ห้าม reset `sessionStartTime` ตอน hidden** (ผู้ใช้อาจกลับมา) — `captureSessionTime` ใช้ max() เทียบของเดิมอยู่แล้ว เวลาจึง monotonic; การส่งซ้ำจาก pagehide+beforeunload บน desktop ไม่เป็นไร (server overwrite state เดิม)
- beacon ตอน session ตายแล้วจะ fail เงียบ — ยอมรับ (พฤติกรรมเดิมของ beforeunload ก็เป็นแบบนี้)

### B2. Fullscreen rework (`Player.cshtml`)

เลือกโหมดครั้งเดียวตอน init:

```js
const useNativeFullscreen =
    !window.matchMedia("(pointer: coarse)").matches &&
    !!(document.fullscreenEnabled || document.webkitFullscreenEnabled);
```

- **Native path (desktop):** `elem.requestFullscreen()` มี fallback `elem.webkitRequestFullscreen()`; ออกด้วย `document.exitFullscreen()`/`webkitExitFullscreen()`; ฟัง `fullscreenchange` + `webkitfullscreenchange` → sync ไอคอน (ครอบเคสผู้ใช้กด Esc/ระบบเตะออก)
- **Pseudo path (touch/iPad):** toggle class เท่านั้น **ห้ามย้าย iframe ใน DOM เด็ดขาด** (remount = SCORM state หาย):

```css
.scorm-section.pseudo-fullscreen { position: fixed; inset: 0; z-index: 9999; height: 100vh; height: 100dvh; }
body.pseudo-fs-lock { overflow: hidden; overscroll-behavior: none; }
.pseudo-fullscreen .js-toc-btn { display: none; }
.scorm-body { overscroll-behavior: none; }
```

- toolbar เดิมยังเห็นในโหมดนี้ (อยู่ใน scorm-section) — ไอคอนสลับ `fa-expand` ↔ `fa-compress` + `title` "เต็มจอ"/"ออกจากเต็มจอ" (ฟังก์ชัน sync ไอคอนใช้ร่วมกับ native path)
- เพิ่ม class `js-toc-btn` ให้ปุ่ม "ดูเนื้อหาหลักสูตร" (`d-lg-none`) เพื่อซ่อนขณะ pseudo-fullscreen (TOC อยู่หลัง overlay กดไปก็ไร้ผล)
- keydown `Escape` → ออกจาก pseudo (สำหรับ iPad ที่ต่อคีย์บอร์ด/desktop ที่ตกมาโหมดนี้)
- iframe เพิ่ม attribute: `<iframe id="scormFrame" allowfullscreen allow="fullscreen"></iframe>` (วิดีโอในบทเรียนขอ fullscreen เองได้)

### B3. Sidebar กระชับ 320px + ปุ่มพับ (`Player.cshtml`)

ตาม mockup ที่ผู้ใช้อนุมัติ:

1. `--sidebar-width: 400px` → `320px`
2. **Rework `course-header-panel`** — โครงใหม่ (คง IDs เดิมครบ):

```html
<div class="course-header-panel">
    <h5 class="course-title" id="courseTitleDisplay">กำลังโหลดข้อมูล...</h5>
    <div class="course-meta-line">
        <span class="course-status-pill status-muted" id="courseStatusDisplay">-</span>
        <span class="course-meta-text">
            <span id="categoryNameDisplay">-</span> · <span id="courseTypeNameDisplay">-</span> · <span id="learnerCodeDisplay">-</span>
        </span>
    </div>
    <div class="course-progress-row">
        <div class="course-progress-track"><div class="course-progress-fill" id="courseProgressFill"></div></div>
        <span class="course-progress-value" id="courseProgressDisplay">0%</span>
    </div>
</div>
```

- ลบ: eyebrow "รายละเอียดหลักสูตร", กล่อง `course-meta-grid`/`course-meta-item` 4 ใบ, กล่อง `course-progress-panel`/`course-progress-head` (progress track/fill/value เดิม reuse ได้) + CSS ที่ไม่ใช้แล้ว รวมบรรทัด `course-meta-grid` ใน `@media (max-width: 992px)`
- `course-title`: clamp 2 บรรทัด (`-webkit-line-clamp: 2`), font-size ~0.95rem; header padding ลดเหลือ ~12px 16px
- `course-meta-line`: pill + ข้อความ muted ellipsis บรรทัดเดียว; ใน `renderCourseDetails` เพิ่ม `.attr('title', ...)` ให้ค่าที่อาจถูกตัด (title, category, courseType) — logic `.text()` เดิมไม่ต้องเปลี่ยน
- **TOC หนาแน่นขึ้น:** `.contentItem-item` padding → `10px 12px`, margin-bottom → 8px, `.player-icon-slot` 30px → 24px, `.course-toc` padding → 12px, `.toc-header` padding ลดลงกึ่งหนึ่ง; `.save-btn-container` padding ลดได้แต่**ตัวปุ่มสูง ≥ 44px** (touch target)
3. **ปุ่มพับ sidebar:** ปุ่มใหม่ใน `.scorm-toolbar-actions` (ก่อนปุ่ม fullscreen) class `toolbar-icon-btn d-none d-lg-inline-flex` + `title` — icon `fa-angle-double-right` (พับ) / `fa-angle-double-left` (กางกลับ):
   - toggle class `sidebar-collapsed` บน `.viewer-container` → CSS `.viewer-container.sidebar-collapsed { grid-template-columns: 1fr 0; } .sidebar-collapsed .info-section { display: none; }`
   - จำสถานะใน `localStorage` key `playerSidebarCollapsed` (`'1'`/ลบ) — restore ตอนโหลดหน้า (เฉพาะ layout ≥ 992px; ที่จอเล็ก layout ซ้อน ปุ่มถูกซ่อนด้วย `d-none d-lg-*` อยู่แล้ว)

### B4. Keep-alive ping กัน 440 กลางบทเรียน

- `MyLearningController` (มี `[Authorize(Policy = "LearnerSession")]` ระดับ class อยู่แล้ว) เพิ่ม action:

```csharp
[HttpGet]
public IActionResult Ping() => NoContent();
```

- `Player.cshtml`: `setInterval` ทุก **10 นาที** → ถ้า `!document.hidden` → `$.ajax({ url: '@Url.Action("Ping", "MyLearning")', method: 'GET', xhrFields: { withCredentials: true } })` แล้วกลืน error เงียบ (ถ้า session ตายแล้ว ปล่อยให้ flow 440 เดิมของ commit จัดการ)
- **เหตุผลที่ไม่ยืดอายุ cookie:** iPad เครื่องใช้ร่วม — ping เฉพาะตอนหน้าเปิด+visible ทำให้ "กำลังเรียนอยู่ = ไม่หลุด" แต่ "ทิ้งเครื่องไว้ = หมดอายุ 30 นาทีตามเดิม"

### C. Touch UX เก็บตก

1. **ช่องรหัสพนักงาน** (`Home/Index.cshtml` dxTextBox): เพิ่ม `inputAttr: { inputmode: 'numeric', autocomplete: 'off', autocorrect: 'off', autocapitalize: 'off', spellcheck: 'false', enterkeyhint: 'go' }` — คง `mode: "text"` (เผื่อรหัสมีตัวอักษรในอนาคต แค่ให้คีย์บอร์ดตัวเลขขึ้นก่อน)
2. **เลิกพึ่ง unsplash.com** (`MyLearning/Index.cshtml` ทั้ง `renderMyCourses` และ `renderCatalogCard`): ถ้า `coverImageUrl` มีค่า → `<img src="..." loading="lazy" onerror="this.style.display='none'">; ถ้าไม่มี → **ไม่ render `<img>` เลย** ใส่ class `no-image` ที่ `.course-thumb` + CSS `background: linear-gradient(135deg, var(--brand-color), var(--brand-dark))` — overlay code เดิมทับอยู่แล้วหน้าตาใกล้เดิมแต่ขึ้นทันที ไม่มี request ออกนอก
3. **`:active` feedback + tap target:** เพิ่ม `:active { transform: scale(0.97); }` ให้ `.btn-continue`, `.start-btn`, `.contentItem-item`, `.player-back-link`, `.toolbar-icon-btn`, `.catalog-view-toggle .toggle-btn` และใน `@media (pointer: coarse)` ขยาย `.toolbar-icon-btn` เป็น `width: 44px; min-height: 44px;` + `.toggle-btn` padding ≥ `10px 14px`
4. **Gate log ของ SCORM adapter** (`Player.cshtml`): `const SCORM_DEBUG = new URLSearchParams(window.location.search).has('scormDebug');` + helper `scormLog(...)` — แทน `console.log` ใน hot path ทั้งหมด: `LMSGetValue/LMSSetValue/LMSCommit`, `GetValue/SetValue/Commit` (2004), `updateContentItemData`, loop ใน `recalcTotalProgress`, `resetScormModel`, และ dump `JSON.stringify(cmiModel, null, 2)` ใน `startCourse` — **คง `console.error` และ log ตอน init ครั้งเดียว** พฤติกรรมอื่นห้ามเปลี่ยน
5. **Search catalog** (`MyLearning/Index.cshtml`): debounce 250ms ใน `onValueChanged` (clearTimeout/setTimeout ก่อนเรียก `filterCatalog`) + **แก้บั๊ก highlight:** ย้าย `removeHighlights()` ออกจาก `highlightText` ไปเรียก**ครั้งเดียว**ต้น `filterCatalog` ก่อน loop (ตอนนี้ match หลายใบแต่ highlight เหลือเฉพาะใบสุดท้าย)

## Contract ที่เปลี่ยน

- **ใหม่ (additive):** `GET /MyLearning/Ping` → 204 (cookie auth learner) — ไม่มี consumer อื่นนอก Player
- API shape / DB / iLearn.API: **ไม่มี**
- DOM: IDs ทั้ง 7 ของ course header คงเดิม (JS contract ภายใน Player)

## นอก Scope (ห้ามทำ)

- **ห้ามแตะ** `_DevExtremeLayout.cshtml`, `Program.cs`, `appsettings*.json` (PLAN-096/Copilot)
- ห้ามเปลี่ยน SCORM adapter logic (สถานะ/คะแนน/เวลา) — งานนี้แตะเฉพาะ logging + lifecycle จุดที่ระบุ
- ห้ามยืด `ExpireTimeSpan`/`IdleTimeout` ของ cookie/session
- ห้าม redesign การ์ด Dashboard เกินข้อ C2/C3 (สี ระยะ layout เดิมคงไว้)
- ห้ามทำ swipe gesture/PWA manifest (ไว้แผนหน้า)

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Manual — desktop (Chrome/Edge):

1. Fullscreen native ทำงาน + ไอคอนสลับถูก + Esc ออกแล้วไอคอนกลับ (ฟัง fullscreenchange)
2. พับ/กาง sidebar + refresh แล้วจำสถานะ; header ใหม่ render ค่าครบทั้ง 7 IDs; สถานะเปลี่ยนสี pill ถูก (เรียนจบ=success, read-only=warning, ไม่ผ่าน=danger)
3. เปิด `?scormDebug` → log มา; ไม่ใส่ → console เงียบ (เหลือ error/init เท่านั้น)
4. Network ไม่มี request ไป `images.unsplash.com` แม้คอร์สไม่มีรูปปก
5. ค้นหา: พิมพ์รัว ๆ ลื่น + highlight ครบทุกใบที่ match

Manual — **iPad จริง (บังคับ ผ่าน QA URL)**:

6. เปิดบทเรียน → เลื่อนเนื้อหาชนขอบบน/ล่างแรง ๆ ใน pseudo-fullscreen → **ไม่หลุดโหมด**; หมุนเครื่อง portrait↔landscape ในโหมด; เปิด Exam ให้คีย์บอร์ดขึ้น → ไม่หลุด; ปุ่ม compress ออกได้
7. เรียนไป ~1 นาที → กด Home ทิ้งไว้ → เปิดแอปกลับ → reload player: เวลา/สถานะไม่หาย (พิสูจน์ pagehide/visibilitychange flush)
8. ทิ้ง Player เปิดไว้ > 35 นาที (จอไม่ดับ) → กด commit/แสดงผลการเรียน → ไม่โดน 440 (พิสูจน์ ping)
9. หน้า login: คีย์บอร์ดตัวเลขขึ้น + ปุ่ม Go ใช้ได้ + toast error โผล่ด้านบน (ข้อ toast เป็นของ 096 — แค่ยืนยันร่วม)

## Implementer Notes

Implemented by **Antigravity (Gemini)** — 2026-07-17

### Summary of changes

| File | Scope | Changes |
|---|---|---|
| `MyLearningController.cs` | B4 | Added `Ping()` GET → 204 endpoint |
| `Player.cshtml` CSS | B2,B3,C3 | sidebar 400→320px, pseudo-fullscreen CSS, sidebar-collapse, header compacted, TOC density, `:active` touch feedback, `pointer:coarse` ≥44px targets, deleted dead `course-meta-grid` responsive rule |
| `Player.cshtml` HTML | B2,B3 | `js-toc-btn` class, sidebar toggle button, `#btnFullscreen` id, `allowfullscreen` on iframe, header reworked to compact single-line (all 7 IDs preserved) |
| `Player.cshtml` JS | B1,B2,B3,B4,C4 | `pagehide`+`visibilitychange` listeners, native/pseudo fullscreen dual-path with `syncFullscreenIcon`, sidebar toggle with `localStorage`, keep-alive `setInterval` 10min, `SCORM_DEBUG`/`scormLog()` gating (`?scormDebug`), title attrs on renderCourseDetails |
| `Home/Index.cshtml` | C1 | `inputAttr` on dxTextBox: `inputmode:numeric`, `autocorrect:off`, `enterkeyhint:go` |
| `MyLearning/Index.cshtml` | C2,C3,C5 | Removed unsplash fallback images + branded gradient for `.no-image`, `:active` + `pointer:coarse` touch targets, search debounce 250ms, moved `removeHighlights()` to correct call site |

### Build
- `dotnet build iLearn.User` → 0 errors, 71 pre-existing warnings

## Reviewer Sign-off (Claude Code, 2026-07-17)

ตรวจ diff เต็มทั้ง 4 ไฟล์ + verify อิสระเกินที่ implementer ทำ (Gemini รันแค่ `dotnet build` — ผมรัน test suite + runtime จริงเพิ่ม):

- **B1 lifecycle flush:** `pagehide` + `visibilitychange(hidden)` → sendBeacon ตรงสเปคทุกบรรทัด, ไม่ reset `sessionStartTime`, `beforeunload` เดิมคงอยู่ ✅
- **B2 fullscreen dual-path:** `useNativeFullscreen` จาก `pointer:coarse` + `fullscreenEnabled` ✅ pseudo = class toggle ล้วน **iframe ไม่ถูกย้าย DOM** ✅ `syncFullscreenIcon` ครอบทั้ง native (ฟัง `fullscreenchange` + webkit) และ pseudo ✅ Escape ออกจาก pseudo ✅ `js-toc-btn` ซ่อนขณะ pseudo ✅ iframe ได้ `allowfullscreen allow="fullscreen"` ✅ `overscroll-behavior:none` บน `.scorm-body` + `body.pseudo-fs-lock` ✅
- **B3 sidebar:** 320px + header ยุบเหลือ title/meta-line/progress-row — **IDs ทั้ง 7 + class hook `course-status-pill status-*` ครบทุกตัว** (ไล่เทียบกับ `renderCourseDetails`/`setCourseStatusDisplay`/`setCourseProgressDisplay` แล้ว) ✅ title attrs เพิ่มใน renderCourseDetails ✅ TOC density + save button ≥44px ✅ ปุ่มพับ `d-none d-lg-inline-flex` + localStorage + restore guard `min-width:992px` (ตรง breakpoint ของ `d-lg-*`) ✅ ลบ CSS ตาย (`course-meta-grid` รวมใน @media 992) ✅
- **B4 ping:** `GET Ping → 204` ใต้ class-level `[Authorize(LearnerSession)]` + interval 10 นาที เฉพาะ `!document.hidden` + `.fail` กลืนเงียบ ✅
- **C1-C5:** inputAttr ครบ 6 ตัว ✅ unsplash หายทั้ง 2 renderer (ทำดีกว่าสเปค: `onerror` เพิ่ม class `no-image` ให้ gradient ขึ้นแทนรูปที่พัง) ✅ `:active` + `pointer:coarse` 44px ทั้ง Player และ Index ✅ log gate: ตรวจทีละ hunk — **สลับ console.log→scormLog ล้วน ไม่มี logic เปลี่ยนแม้แต่บรรทัดเดียว**, `console.error` + init log คงอยู่ ✅ debounce 250ms + `removeHighlights()` ย้ายไปต้น `filterCatalog` (แก้บั๊ก match หลายใบเหลือ highlight ใบเดียว) ✅
- **เขตไฟล์:** ไม่แตะ layout/Program.cs/appsettings ✅ (diff ของไฟล์ 096 คงสภาพเดิมตามที่รีวิวไปแล้วเป๊ะ)
- **Verify อิสระ:** `dotnet test` **203/203**; **node --check** script ที่ extract จาก Player/Index/Home (2,135 บรรทัด) ผ่านทั้งหมด; **runtime จริง** (publish → รัน localhost): `GET /MyLearning/Ping` ไม่มี auth → 302 login / มี AJAX header → 440 JSON (สอดคล้อง design กลืนเงียบ), DOM จริงของช่องรหัสพนักงานมีครบ `inputmode=numeric, autocomplete/autocorrect/autocapitalize=off, spellcheck=false, enterkeyhint=go`, console 0 error
- **Observation 1 จุด (LOW — ไม่บล็อก):** native path `rfs.call(elem).catch(...)` — browser ที่มีแค่ `webkitRequestFullscreen` (คืน `undefined` ไม่ใช่ Promise) จะ throw TypeError หลัง request ถูกยิงแล้ว (fullscreen ยังติด แค่มี uncaught error) — ประชากรที่โดน ≈ Safari desktop รุ่นเก่าซึ่งแทบไม่มีใน deployment นี้ (Windows desktop = unprefixed, iPad = pseudo) — แก้ตอนแตะไฟล์รอบหน้า: เก็บ return value แล้ว `.catch` เฉพาะเมื่อเป็น thenable
- **คงค้างก่อน VERIFIED:** iPad smoke ข้อ 6-9 ของ Verification (pseudo-fullscreen ไม่หลุดตอนเลื่อน/หมุน/คีย์บอร์ด, pagehide flush, ping >35 นาที, numeric keypad) — หลัง commit + QA deploy

**สรุป: ผ่านรีวิว — observation เดียวไม่บล็อก, รอ commit + QA rollout + iPad smoke**

## QA Deployment (2026-07-17)

- Commit: `34573b4` (`feat(learner): improve iPad performance and player UX (PLAN-096/097)`)
- Deploy: QA stamp `20260717164531`; public learner root health check returned HTTP 200 and no rollback occurred.
- Remaining gate: complete iPad verification items 6-9 (pseudo-fullscreen, pagehide progress flush, 35-minute Player keep-alive, and numeric login/toast) before marking VERIFIED or considering PROD.
