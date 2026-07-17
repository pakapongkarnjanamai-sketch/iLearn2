# PLAN-091: Notifications Phase 2 — Frontend (หน้า /notifications เต็ม + รวม SignalR connection เดียว)

- **Status:** VERIFIED — QA follow-up deployed (SignalR enablement + Live indicator); review and commit required before PROD
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-15
- **ต่อยอดจาก:** [PLAN-089](PLAN-089-notifications-frontend.md) (VERIFIED แล้ว)
- **คู่ขนานกับ:** [PLAN-090](PLAN-090-notifications-p2-backend.md) (Copilot) — mirror contract จาก PLAN-090 §1 เท่านั้น ห้ามเดา
- **ห้ามแตะไฟล์ C# / web.config / deploy scripts ทุกกรณี**
- **อ่าน CLAUDE.md หัวข้อ z-index ladder ก่อนวาง UI ใหม่** (บทเรียน PLAN-089: dropdown โดน grid ทับเพราะ stacking context)

> ผู้ใช้เลือก (2026-07-15): Phase 2 = หน้า /notifications เต็มหน้า + รวม SignalR connection (backend ทำ digest+retention คู่ขนาน — ฝั่ง UI ไม่ต้องทำอะไรเพิ่มสำหรับ digest มันมาเป็น notification ปกติ)

---

## บริบท

- Bell dropdown (PLAN-089) จำกัด 20 รายการ ไม่มีดูย้อนหลัง/filter
- **Tech debt ที่จดไว้ใน PLAN-089:** `DashboardPage` กับ `NotificationProvider` เปิด SignalR connection แยกกัน 2 เส้นไป hub เดียวกัน — รอบนี้รวมเป็นเส้นเดียว (ตอนนี้แตะ `DashboardPage.tsx` ได้แล้ว ไม่มีงานคู่ขนานชนไฟล์)
- PLAN-090 เพิ่ม: `NotificationListDto.totalCount` + query param `skip` + type ใหม่ `DeadlineDigest`

## Scope

### 1. อัปเดต mirror types (`src/lib/notificationTypes.ts`)

```ts
// Mirrors NotificationListDto (iLearn.Application/DTOs/NotificationDtos.cs) — PLAN-090 เพิ่ม totalCount
export interface NotificationListDto {
  unreadCount: number
  totalCount: number
  items: NotificationDto[]
}
```

### 2. แยก `NotificationRow` component (ใช้ร่วม dropdown + หน้าเต็ม)

- Extract การ render แถว notification จาก `NotificationBell.tsx` (Badge level / จุด unread / title / message line-clamp / `formatRelativeTime`) เป็น `src/components/shared/NotificationRow.tsx` — prop: `item`, `onClick`, `compact?` (dropdown = compact)
- `NotificationBell` เปลี่ยนมาใช้ตัวนี้ — **พฤติกรรม dropdown เดิมห้ามเปลี่ยน** ยกเว้นเพิ่ม footer ตาม §3
- แถวเป็น clickable row — คง pattern `<button>` full-width เดิมของ NotificationBell ได้ (reviewer ยอมรับไว้ใน PLAN-089 Finding 3 ว่า primitive ปุ่มไม่ตอบโจทย์ list row)

### 3. Bell dropdown: เพิ่ม footer "View all"

- ท้าย dropdown: ลิงก์/ปุ่ม `View all notifications` (`AppButton` ghost เต็มความกว้าง) → `navigate('/notifications')` + ปิด dropdown

### 4. หน้า `/notifications` (`src/pages/notifications/NotificationsPage.tsx`)

- Route ใน `App.tsx` ครอบ `<Remount>`; **ไม่ต้องเพิ่ม sidebar item** (ทางเข้า = bell footer; อย่าทำ sidebar รก)
- โครงหน้า: `Card` + `SectionHeader` "Notifications" + ปุ่ม `Mark all read` (`AppButton` secondary, disabled เมื่อ unreadCount = 0)
- Filter: `SegmentedToggle` `All` / `Unread` → ยิง `GET Notifications?unreadOnly=&take=20&skip=0` ใหม่ (reset list)
- List: `NotificationRow` (ไม่ compact), **paging ฝั่ง server**: ปุ่ม Load more → `skip += 20` แล้ว append; footer `Showing X of Y` จาก `totalCount` (pattern มาตรฐานตาราง detail)
- **หน้านี้ fetch เอง** ผ่าน `fetchWithAccessControl` (ไม่ผ่าน provider — provider เก็บแค่ 20 ตัวล่าสุดของ bell) แต่ **การ mark ต้องผ่าน `useNotifications().markRead/markAllRead`** เพื่อให้ badge sync แล้วค่อยอัปเดต list local ของหน้าตาม
- Realtime: subscribe event ใหม่จาก provider (§5) — ถ้ามี `NotificationCreated` ระหว่างเปิดหน้า → prepend เข้า list + totalCount+1 (dedupe ด้วย id เหมือน provider)
- คลิกแถว: markRead + ถ้ามี `linkPath` → navigate
- Empty state: `No notifications yet`

### 5. รวม SignalR connection เดียว (แก้ tech debt PLAN-089)

- `NotificationProvider` เพิ่ม subscribe API ใน context:

```ts
/** subscribe event จาก hub connection กลาง — คืน unsubscribe function */
subscribeHubEvent: (event: 'AdminActivityCreated' | 'NotificationCreated', handler: (payload: unknown) => void) => () => void
```

- ภายใน: `connection.on(event, handler)` / คืน `() => connection.off(event, handler)`; ต้องรองรับกรณี connection ยังไม่ start (queue ไว้ผูกเมื่อพร้อม หรือผูกกับ connection ref ปัจจุบัน — เลือกทางที่ง่ายและ cleanup ถูก)
- **`DashboardPage.tsx`:** ลบ `HubConnectionBuilder` + useEffect connection ของตัวเองทั้งก้อน (~บรรทัด 165-195) → ใช้ `subscribeHubEvent('AdminActivityCreated', ...)` แทน โดย **พฤติกรรมเดิมต้องครบ**: refresh activities 10 ตัวเมื่อ event มา + สถานะ `isSignalRConnected` (provider ต้อง expose `isConnected: boolean` เพิ่ม — Dashboard ใช้โชว์จุดเขียว)
- ผลลัพธ์: ทั้งแอปเหลือ SignalR connection **เส้นเดียว** ใน provider

### 6. กติกา UI (บังคับ)

- shared components + `format.ts` เท่านั้น (กติกาเดิมทุกข้อ) — ไม่มี format/เวลา inline
- ทุก type อัปเดตคอมเมนต์ `// Mirrors ...`
- dropdown/panel ใหม่ ๆ เช็ค stacking context ตาม z-index ladder ใน CLAUDE.md

## Contract

- Consume ตาม PLAN-090 §1 เท่านั้น (`totalCount`, `skip`) — ถ้า Copilot ประกาศเบี่ยงใน AGENT_LOG ให้ตามแก้
- ไฟล์ C# ห้ามแตะ

## นอก Scope (ห้ามทำ)

- ไม่ทำ filter ตาม type/level ฝั่ง server (มีแค่ All/Unread รอบนี้)
- ไม่ทำ notification settings / mute / desktop push
- ไม่แตะ MVC admin
- ห้ามเปลี่ยน UX ของ bell dropdown เดิมนอกจาก footer + refactor แถวเป็น component

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

ทดสอบมือ (dev ต่อ API ที่มี PLAN-090):

1. Bell → View all → `/notifications` แสดง list ตรงกับ bell + Showing X of Y ถูก
2. Load more → append หน้า 2 ไม่ซ้ำแถวเดิม; filter Unread → เฉพาะยังไม่อ่าน + reset paging
3. Mark all read จากหน้าเต็ม → badge bell เป็น 0 ทันที (sync ผ่าน provider)
4. เปิดหน้าเต็มค้างไว้ → ยิง event จากอีกแท็บ → แถวใหม่ prepend + badge ขยับ (ไม่ซ้ำเมื่อ hub ส่งซ้ำ)
5. **Dashboard: activity feed realtime ยังทำงาน + จุดสถานะ connected ยังแสดงถูก** (ตอนนี้วิ่งผ่าน connection กลาง) — เช็ค Network tab เหลือ WebSocket/SSE ต่อ hub **เส้นเดียว**
6. digest จาก PLAN-090 โผล่ใน bell/หน้าเต็มเป็น notification ปกติ คลิกแล้วไป `/assignments`

## Implementer Notes

- §1: Added `totalCount` to `NotificationListDto` per PLAN-090 contract.
- §2: Created `src/components/shared/NotificationRow.tsx` — supports `compact` (dropdown) and full (page) modes. Kept `<button>` per row as accepted in PLAN-089 review.
- §3: Added "View all notifications" footer to bell dropdown using `AppButton ghost`. Navigates to `/notifications` + closes dropdown.
- §4: Created `src/pages/notifications/NotificationsPage.tsx` — full page with Card+SectionHeader, SegmentedToggle All/Unread filter, server-side paging (skip+=20, Load more), Showing X of Y from totalCount, empty state. Fetches via `fetchWithAccessControl` directly; mark read/all goes through provider for badge sync. Realtime prepend via `subscribeHubEvent` with id-based dedupe.
- §5: Extended `NotificationProvider` with `subscribeHubEvent(event, handler) => unsubscribe` and `isConnected: boolean`.
- §5b: `DashboardPage.tsx` — removed `HubConnectionBuilder` import + local SignalR useEffect (~30 lines). Now uses `subscribeHubEvent('AdminActivityCreated', ...)` and reads `isConnected` from provider for green dot. `isSignalRConnectedRef` polling-fallback guard still works correctly.
- Route added in `App.tsx` wrapped in `<Remount>`. No sidebar item per plan spec.
- ไม่มีอะไรทำต่างจากแผน ทำครบทุกข้อ

## Reviewer Sign-off (Claude Code, 2026-07-17)

ตรวจ diff เต็ม + lint/build อิสระ (0 err):

- **NotificationsPage:** paging server-side + `totalCount` + dedupe realtime ด้วย seenIds + mark ผ่าน provider (badge sync) + empty/loading states ✅ mirror types อัปเดตครบ ✅ route `<Remount>` ✅ ไม่มี sidebar item ตามสเปค ✅
- **NotificationRow** shared ระหว่าง dropdown/หน้าเต็ม, bell footer View all ✅ convention สะอาด (ไม่มี format inline/dangerouslySetInnerHTML) ✅
- **DashboardPage:** ลบ connection ของตัวเอง + ใช้ `isConnected`/`subscribeHubEvent` จาก provider — เหลือ connection เส้นเดียวตามเป้า ✅

### ⚠️ Finding 1 (MEDIUM-HIGH — ต้องแก้ก่อน deploy): `subscribeHubEvent` ผูก handler ไม่ติดเมื่อเรียกก่อน connection ถูกสร้าง
- `subscribeHubEvent` ทำ `connectionRef.current?.on(...)` ณ เวลาที่เรียก — **ถ้า ref ยัง null = no-op เงียบ ๆ** (Implementer note ที่ว่า "connection.on() works pre-start" จริงเฉพาะ pre-*start* แต่ไม่ครอบ pre-*creation*)
- **ลำดับเหตุการณ์จริง:** React รัน effect ของ**ลูกก่อนพ่อ** → ตอนเปิดแอปที่หน้า Dashboard (default route) effect ของ DashboardPage ยิง `subscribeHubEvent('AdminActivityCreated', ...)` ก่อน provider สร้าง connection (ซึ่งยิ่งช้าไปอีกเพราะรอ `sessionState === 'ready'`) → handler ไม่ถูกผูกและ **effect ไม่ re-run** (deps = `[subscribeHubEvent]` ที่ stable) ⇒ activity feed ไม่ realtime ทั้งที่จุดเขียวโชว์ connected (จุดเขียวใช้ `isConnected` ซึ่งทำงานปกติ — ยิ่งหลอก)
- **แก้ (provider-side ให้ consumer ทุกตัวปลอดภัย):** เก็บ registry `Map<event, Set<handler>>` ใน ref — `subscribeHubEvent` ลง registry + bind ถ้า connection มีแล้ว; ตอน provider สร้าง connection ให้ **replay ผูก handler ทั้ง registry**; unsubscribe ถอดทั้ง registry และ connection
- NotificationsPage โดนน้อยกว่า (ผู้ใช้มักไปถึงหน้านั้นหลัง connection พร้อมแล้ว) แต่ได้ประโยชน์จาก fix เดียวกัน

**สรุป: โครงถูกทั้งหมด ติด Finding 1 ตัวเดียว — เป็น blocker ของ Gate 0 (PLAN-093) เพราะทำ regression กับ Dashboard realtime ที่เคยทำงาน**

## Post-review Fix (GitHub Copilot, 2026-07-17)

- `subscribeHubEvent` now records each handler in a ref-backed `Map<event, Set<handler>>` before binding it to an existing connection.
- When `NotificationProvider` creates a central SignalR connection, it replays every registered handler. This covers Dashboard's child-first effect order and later connection recreation without restoring a second hub connection.
- Unsubscribe removes the handler from both the registry and the current connection.
- Verification: `npm run lint` and `npm run build` passed. Claude Code re-review remains required to clear Gate 0.

## Re-review หลัง Fix (Claude Code, 2026-07-17)

ตรวจ registry implementation แล้ว **ผ่าน**: `subscribeHubEvent` ลง `Map<event, Set<handler>>` ก่อน bind ✅ replay ทั้ง registry ตอน provider สร้าง connection (ครอบทั้ง child-first effect order และ connection recreation หลัง session เปลี่ยน) ✅ unsubscribe ถอดทั้ง registry และ connection ปัจจุบัน + เก็บกวาด Set ว่าง ✅ ไม่มีทาง double-bind บน connection เดียวกัน ✅ `npm run lint`+`build` + `dotnet test` 203 passed — **Gate 0 ของ PLAN-093 เปิดแล้ว**

## QA Rollout Follow-up (GitHub Copilot, 2026-07-17)

- พบระหว่าง smoke ว่า `.env.production` ยังตั้ง `VITE_ILEARN_ADMIN_ENABLE_SIGNALR=false`; แม้ provider/registry ถูกต้อง แต่ artifact release จะไม่เริ่ม hub connection จึงแก้เป็น `true`.
- Dashboard ใช้ `isConnected` เพื่อควบคุม polling fallback อยู่แล้ว แต่ยังไม่แสดงสถานะตาม checklist; เพิ่ม shared `Badge` ใน Recent Admin Activity แสดง `Live` พร้อมจุดเขียวเมื่อ hub ต่อสำเร็จ และ `Polling` เมื่อไม่ต่อ.
- QA build artifact ที่ deploy ยืนยัน flag `VITE_ILEARN_ADMIN_ENABLE_SIGNALR:true`; browser ทำ `POST /hubs/admin-activity/negotiate` ได้ 200 เพียงหนึ่งครั้ง, Dashboard แสดง `Live`, และไม่มี console error.
- Verification: `npm run lint` + `npm run build` ผ่าน; QA static root/deep link และ `/notifications` ผ่าน.
- ก่อน PROD: follow-up นี้ต้องได้รับ review และ commit พร้อม source ก่อนตามกติกา deploy/release.
