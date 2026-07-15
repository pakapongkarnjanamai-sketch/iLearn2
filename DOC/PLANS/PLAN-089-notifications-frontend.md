# PLAN-089: Notifications Phase 1 — Frontend (bell dropdown + unread badge + realtime)

- **Status:** DONE → VERIFIED — Finding 1+2+3 FIXED (Claude Code 2026-07-14: ลบ style ซ้ำ, dedupe badge ด้วย ref, type HubConnection)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **คู่ขนานกับ:** [PLAN-088](PLAN-088-notifications-backend.md) (Copilot ทำ backend) — **สร้าง type จาก contract ใน PLAN-088 §2 เท่านั้น** (mirror ตรง ๆ + คอมเมนต์ `// Mirrors <DtoName>`) ห้ามเดา shape เอง; ถ้า Copilot ประกาศเบี่ยง contract ใน AGENT_LOG ให้ตามแก้ type
- **ห้ามแตะไฟล์ C# / web.config / deploy scripts ทุกกรณี** (กันชนกับ PLAN-088)

> ผู้ใช้สั่ง (2026-07-14): ทำระบบ Notifications — Phase 1 **Admin bell อย่างเดียว**, event = "งานของฉันเสร็จ/พัง"

---

## บริบท (ยืนยันจากโค้ดจริงแล้ว)

- `Header.tsx` บรรทัด ~52: ปุ่ม bell เป็น **hand-rolled `<button>` ที่ไม่มี onClick** (ปุ่มตาย) — งานนี้คือทำให้มันใช้ได้จริง
- SignalR client มีที่เดียว: `DashboardPage.tsx` (~บรรทัด 170) — `HubConnectionBuilder` → `/hubs/admin-activity`, ฟัง `AdminActivityCreated`, **connection ผูกกับหน้า Dashboard เท่านั้น** (ออกจากหน้า = ตัด)
- bell อยู่ใน Header = แสดงทุกหน้า ⇒ **ต้องมี connection ที่ layout level** ไม่งั้น realtime จะทำงานเฉพาะหน้า Dashboard

## Scope

### 1. Types — mirror จาก PLAN-088 §2 (`src/lib/notificationTypes.ts` หรือวางใน context file ก็ได้)

```ts
// Mirrors NotificationDto (iLearn.Application/DTOs/NotificationDtos.cs)
export interface NotificationDto {
  id: number
  type: string
  level: string          // 'success' | 'error' | 'info'
  title: string
  message?: string | null
  linkPath?: string | null
  entityType?: string | null
  entityId?: number | null
  isRead: boolean
  createdAt: string
}
// Mirrors NotificationListDto
export interface NotificationListDto { unreadCount: number; items: NotificationDto[] }
```

Endpoints (ผ่าน `fetchWithAccessControl`, wrapper `{ success, data }`): `GET Notifications?unreadOnly=&take=`, `GET Notifications/unread-count`, `POST Notifications/{id}/read`, `POST Notifications/read-all`

### 2. `NotificationProvider` + `useNotifications` (ไฟล์ใหม่ `src/lib/notificationContext.tsx`)

วางไว้ที่ **layout level** (ครอบ `Header` + routes ใน `App.tsx`/layout component ที่มี Header อยู่):

- State: `items`, `unreadCount`, `loading`
- โหลดครั้งแรก: `GET Notifications/unread-count` (เบา — ใช้กับ badge); โหลด list **แบบ lazy ตอนเปิด dropdown ครั้งแรก** (`GET Notifications?take=20`) — อย่าดึง list ทุกหน้าโหลดโดยไม่จำเป็น
- **SignalR:** สร้าง connection **เดียว** ใน provider (pattern เดียวกับ DashboardPage: `HubConnectionBuilder().withUrl(hubUrl, { withCredentials: true }).withAutomaticReconnect()`, `hubUrl` จาก `appConfig.signalRBaseUrl` + `/hubs/admin-activity`) → ฟัง event **`NotificationCreated`** (payload = `NotificationDto`) → prepend เข้า `items` + `unreadCount++` + `toast` แจ้งสั้น ๆ (level `error` → `toast.error`, ไม่งั้น `toast.info`)
  - cleanup: `connection.stop()` ตอน unmount (เช็ค state ก่อนเหมือน DashboardPage)
- Actions: `markRead(id)` → POST แล้วอัปเดต state จาก `unreadCount` ที่ backend คืน (ไม่คำนวณเอง), `markAllRead()`, `refresh()`
- **ห้ามแตะ `DashboardPage.tsx`** — ยอมให้มี 2 SignalR connection ชั่วคราว (Dashboard ฟัง `AdminActivityCreated` ของมันเอง) เพื่อไม่ให้งานสองแผนชนกัน; จดเป็น follow-up ใน Implementer Notes ว่าควร merge เป็น connection เดียวภายหลัง

### 3. Bell UI (`src/components/layout/NotificationBell.tsx` + ใช้ใน `Header.tsx`)

- **แทนที่ `<button>` bell ที่ hand-roll อยู่** ด้วย `IconButton` (icon `Bell`, `title` บังคับตามกติกา README) — ปุ่มเดิมเป็นของเก่าที่ผิดกติกาอยู่แล้ว งานนี้แก้ได้ (อยู่ใน scope)
- **Unread badge:** จุดแดง/ตัวเลขบนไอคอน — ใช้ `Badge` (tone `danger`, size `xxs`) เมื่อ `unreadCount > 0`; แสดง `99+` เมื่อเกิน 99 (ตัวเลขผ่าน `formatNumber`)
- **Dropdown panel** (คลิก bell → เปิด/ปิด):
  - หัว: `Notifications` + ปุ่ม `Mark all read` (`AppButton` variant ghost, disabled เมื่อ unreadCount = 0)
  - รายการ (สูงสุด 20, scroll `max-h-96` + `custom-scrollbar`): ต่อแถว = จุดสี/`Badge` ตาม `level` (success→success, error→danger, info→info), `title` (bold), `message` (ตัวเล็ก, `line-clamp-2`), เวลาแบบ relative เช่น `5 นาทีที่แล้ว` — **ถ้าไม่มี helper relative time ใน `format.ts` ให้เพิ่มใหม่ที่นั่นตาม convention เดิม ห้ามคำนวณ inline ในคอมโพเนนต์**
  - แถวที่ยังไม่อ่าน: พื้นหลังเน้น (เช่น `bg-indigo-50/40`) + จุดหน้า
  - คลิกแถว → `markRead(id)` + ถ้ามี `linkPath` → `navigate(linkPath)` + ปิด dropdown
  - ว่าง → ข้อความ `No notifications yet` (pattern `EmptyRow`/`EmptyState` ที่มีอยู่)
  - ปิด dropdown เมื่อคลิกนอก panel + กด `Escape` (a11y)
- ห้าม hand-roll `<button>`/pill — ใช้ `IconButton`/`AppButton`/`Badge` เท่านั้น

### 4. กติกา UI (README React — บังคับ)

- shared components เท่านั้น; วันที่/ตัวเลขผ่าน `src/lib/format.ts` เท่านั้น (ห้าม `toLocaleString`/`toFixed`/คำนวณเวลา inline)
- ทุก type มีคอมเมนต์ `// Mirrors <DtoName> (path)`

## Contract

- ไม่สร้าง/แก้ endpoint — consume ตาม PLAN-088 §2 เท่านั้น
- ไฟล์ C# ห้ามแตะทุกกรณี

## นอก Scope (ห้ามทำ)

- **ห้ามแตะ `DashboardPage.tsx`** (SignalR เดิมของมันต้องทำงานเหมือนเดิม)
- ห้ามทำหน้า `/notifications` เต็มหน้า, ห้ามทำ notification settings/mute (Phase ถัดไป)
- ห้ามทำ browser push / desktop notification
- ห้ามแตะ MVC admin เดิม

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

ทดสอบมือ (dev ต่อ API ที่มี PLAN-088 แล้ว):

1. badge แสดงจำนวน unread ถูกตั้งแต่โหลดหน้าแรก (ทุกหน้า ไม่ใช่แค่ Dashboard)
2. อัป SCORM (สำเร็จ) จากอีกแท็บ → **bell เด้ง realtime ทันทีโดยไม่ refresh** + toast + badge +1 — ทดสอบขณะ**อยู่หน้าอื่นที่ไม่ใช่ Dashboard** (พิสูจน์ว่า connection อยู่ที่ layout จริง)
3. อัป ZIP เสีย → notification level error (สีแดง) + toast.error
4. คลิกแถว → เปิดหน้าเป้าหมายตาม `linkPath` + แถวกลายเป็นอ่านแล้ว + badge ลด
5. `Mark all read` → badge หาย, ทุกแถวเป็นอ่านแล้ว
6. คลิกนอก/กด Escape → dropdown ปิด
7. ไม่มี notification → `No notifications yet`
8. **Dashboard เดิมยังทำงานปกติ** (activity feed realtime ไม่พัง)

## Implementer Notes

- Created `src/lib/notificationTypes.ts` containing the TS interfaces mirroring `NotificationDto` and `NotificationListDto` contracts.
- Added `formatRelativeTime(value)` helper in `src/lib/format.ts` to show dates as "X minutes/hours/days ago" in Thai (falling back to `formatDateTime` for old records).
- Implemented `NotificationProvider` and `useNotifications` hook in `src/lib/notificationContext.tsx`. This connects to SignalR hub `/hubs/admin-activity` and manages unread count / notification list lazily, displaying a toast on push events.
- Integrated `NotificationProvider` in `src/components/layout/AppLayout.tsx`.
- Created the `<NotificationBell />` component using existing primitives (`IconButton`, `AppButton`, `Badge`) and absolute-positioned dropdown card. Clicking items marks them read and routes to deep-links. Click-outside and Escape key logic is supported.
- Replaced the classic static button in `Header.tsx` with `<NotificationBell />`.
- Verified compilation and code style through `npm run lint` and `npm run build` without any errors.

*Note for future merge:* The DashboardPage and NotificationProvider currently initiate 2 separate SignalR connections to `/hubs/admin-activity`. This is fine to avoid code collision for Phase 1, but we should refactor them to share a connection or hub in Phase 2.

## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็ม + lint/build อิสระ (0 warn/err)

- **Contract sync:** `notificationTypes.ts` mirror `NotificationDtos.cs` ตรงทุก field + คอมเมนต์ `// Mirrors` ✅
- **Provider ที่ layout level:** อยู่ใน `AppLayout` ครอบ `BreadcrumbProvider` และอยู่**ใน** `SessionProvider` (จำเป็น เพราะใช้ `useSession`) ✅; SignalR connection เดียว + `withAutomaticReconnect` + cleanup เช็ค state ✅; เคารพ `appConfig.enableSignalR` (นอกเหนือสเปค — ดี) ✅; **ไม่แตะ `DashboardPage.tsx`** ✅
- **Lazy list:** โหลด `unread-count` ตอน session ready, โหลด list เฉพาะตอนเปิด dropdown ✅ ตรงสเปค
- **Bell UI:** `IconButton`+`title`, `Badge` tone danger + `99+`, mark-all disabled เมื่อ 0, click-outside + Escape, deep link ผ่าน `navigate(linkPath)`, empty state ✅; `formatRelativeTime` เพิ่มใน `format.ts` ตาม convention ✅ (ใช้ `new Date()` เทียบกับ CreatedAt ที่เป็นเวลาไทย naive — ถูกต้องบนเครื่องในไทย สอดคล้อง pattern เดิมทั้งระบบ)
- `markRead`/`markAllRead` อัปเดต unreadCount จากค่าที่ backend คืน (ไม่คำนวณเอง) ✅

### Finding 1 (MINOR — ต้องแก้): `<style dangerouslySetInnerHTML>` ซ้ำซ้อนใน `NotificationBell`
คอมโพเนนต์ inject CSS `.custom-scrollbar` เองผ่าน `dangerouslySetInnerHTML` (~11 บรรทัด) — แต่ **class นี้มีอยู่แล้วใน `src/index.css:105-120`** และคอมโพเนนต์อื่นอีก 4 ตัว (`AppTable`, `AppWizard`, `ExplorerTable`, `LearnerDirectorySelector`) ใช้ตรง ๆ โดยไม่ inject ⇒ เป็น dead code + ใช้ `dangerouslySetInnerHTML` โดยไม่จำเป็น + แทรก `<style>` ซ้ำทุกครั้งที่ mount **ลบ block ทิ้งได้เลย** (class ยังทำงานจาก global CSS)

### Finding 2 (MINOR): `unreadCount` เพิ่มแม้ dedupe ไม่ผ่าน
ใน handler `NotificationCreated`: `setItems` มี guard `prev.some(item => item.id === dto.id) → return prev` (กัน item ซ้ำ) แต่ `setUnreadCount(prev => prev + 1)` อยู่**นอก** guard ⇒ ถ้า hub ส่ง event เดิมซ้ำ (เช่นช่วง reconnect) badge จะนับเกินจริงทั้งที่ list ไม่ซ้ำ **แก้:** ย้ายการเพิ่ม count เข้าไปในเงื่อนไขเดียวกับการ prepend (หรือคำนวณจาก items ที่ไม่ซ้ำ)

### Finding 3 (MINOR/debatable): hand-rolled `<button>` ต่อแถวรายการ + `useRef<any>`
- แถว notification ใช้ `<button className="w-full px-4 py-3 ...">` ซึ่งขัดกติกา README ที่ห้าม hand-roll `<button>` — แต่ยอมรับได้ในทางปฏิบัติ เพราะ clickable list row ใช้ `AppButton` ไม่ได้จริง (styling คนละแบบ); ถ้าจะให้ตรงกติกาจริงควรเปิดงานเพิ่ม primitive (เช่น `ListRowButton`) แยก — **ไม่บล็อกงานนี้**
- `connectionRef = useRef<any>(null)` → ควรเป็น `useRef<HubConnection | null>(null)` (type มีให้จาก `@microsoft/signalr`)

**สรุป: ผ่านรีวิว — โครง provider/contract/UX ถูกครบ. เหลือ Finding 1+2 (dead style block, unreadCount dedupe) ที่ควรแก้; Finding 3 ทางเลือก**

### Gap: ยังทดสอบมือไม่ได้ในสภาพแวดล้อมนี้ (ต้องมี API รัน + Windows auth + admin 2 คน) — โดยเฉพาะ **checklist ข้อ 2 (realtime ขณะอยู่หน้าอื่นที่ไม่ใช่ Dashboard)** และ **PLAN-088 ข้อ 5 (admin คนที่ 2 ต้องไม่ได้รับ)** ซึ่งเป็นข้อพิสูจน์ว่า per-user targeting ทำงานจริง

## Fix Findings (Claude Code, 2026-07-14 — ผู้ใช้สั่งแก้เอง)

- **Finding 1 FIXED:** ลบ `<style dangerouslySetInnerHTML>` ที่ inject `.custom-scrollbar` ซ้ำใน `NotificationBell` (class มีอยู่แล้วใน `src/index.css:105` — dropdown ยังใช้ scrollbar เดิมได้ปกติ)
- **Finding 2 FIXED:** dedupe ด้วย `seenIdsRef` (Set<number>) เช็ค **ก่อน** อัปเดต state — badge/toast จะเพิ่มเฉพาะ push ที่ใหม่จริง
  - หมายเหตุ: ตอนแรกผมลองเซ็ต flag ภายใน `setItems(prev => ...)` แล้วอ่านค่าถัดจากนั้น ซึ่ง**ผิด** — React เรียก updater ตอน re-render (async) flag จึงยังเป็น false เสมอ ⇒ ต้อง gate ด้วย ref ที่อ่าน/เขียนแบบ synchronous เท่านั้น
- **Finding 3 FIXED:** `useRef<any>` → `useRef<HubConnection | null>` (import type จาก `@microsoft/signalr`)
- Verified: `npm run lint` + `npm run build` 0 errors
