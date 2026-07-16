# PLAN-091: Notifications Phase 2 — Frontend (หน้า /notifications เต็ม + รวม SignalR connection เดียว)

- **Status:** READY
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

*(เติมหลังทำเสร็จ)*
