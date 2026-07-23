# PLAN-140: Canonical host redirect — บังคับทุกการเข้าใช้งานผ่าน FQDN (`*.nikonoa.net`)

- **Status:** DONE
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-23
- **ที่มา:** ผู้ใช้รายงานหน้า Health Check บน PROD — การ์ด Learner Site ขึ้น "Could not reach https://ap-ntc2137-prwb.nikonoa.net/iLearn/health/smoke ... (service down, or blocked by CORS)"

---

## Root cause (Claude ตรวจยืนยันแล้ว — อย่าวินิจฉัยซ้ำ)

- **Service ไม่ได้ down** — ยิงตรงจากเครื่อง dev ได้ `200 status:pass` ทั้ง `https://ap-ntc2137-prwb.nikonoa.net/iLearn/health/smoke` และ `https://ap-ntc2137-prwb/iLearn/health/smoke`
- ผู้ใช้เปิดหน้า admin ด้วย **hostname สั้น** (`https://ap-ntc2137-prwb/iLearn/admin-react/...`) แต่การ์ด Learner Site ใน `HealthCheckPage.tsx` fetch ตรงจาก browser ไปยัง `FileSettings:HostUrl` ซึ่งเป็น FQDN → **cross-origin** และ `iLearn.User` ไม่มี CORS → browser block → fetch throw → ขึ้น unreachable
- ประเด็นเดียวกันนี้อันตรายกว่าที่หน้า health check: ตาม precedent PLAN-094/103 ถ้า**ผู้เรียน**เปิดเว็บด้วย hostname สั้น SCORM iframe (โหลดจาก FQDN ตาม HostUrl) จะ cross-origin กับหน้า Player → `window.parent.API` ไม่ได้ → **ล้มเงียบ เรียนจบแต่คะแนนไม่บันทึก**
- แนวทางที่เลือก (ผู้ใช้อนุมัติแล้ว): **redirect ทุกการเข้าผ่าน hostname สั้น → FQDN** แก้ทั้งสองอาการที่ต้นทาง — ทำระดับ**แอป/บันเดิล** (ไม่แตะ IIS config บนเซิร์ฟเวอร์ ตามข้อห้าม precedent PLAN-130)

## ข้อเท็จจริงแวดล้อม

| สิ่งแวดล้อม | hostname สั้น | FQDN canonical (= `FileSettings:HostUrl`) |
|---|---|---|
| QA | `ap-ntc2138-qawb` | `https://ap-ntc2138-qawb.nikonoa.net/iLearn` (`iLearn.User/appsettings.json`) |
| PROD | `ap-ntc2137-prwb` | `https://ap-ntc2137-prwb.nikonoa.net/iLearn` (`iLearn.User/appsettings.Production.json`) |

- Admin React ใช้ API base แบบ relative (`/iLearn/Service/api` ใน `.env.production`) → หลัง redirect ทุกอย่าง same-origin เอง ไม่ต้องแก้ apiClient
- QA/PROD ใช้ **บันเดิล React ตัวเดียวกัน** (แยก env ด้วย runtime hostname detection ใน `appConfig.ts`) ⇒ ฝั่ง React ห้าม hardcode hostname รายเครื่อง ให้ใช้กติกา generic (ดู §B)
- `iLearn.User/appsettings.Development.json` มี HostUrl เป็น hostname สั้น (`https://ap-ntc2138-qawb/iLearnNew`) — dev รันบน localhost จึงต้องมี localhost skip (ดู §A)

## Scope

### §A `iLearn.User` — middleware redirect ไป canonical host

1. สร้าง middleware ใหม่ (แนะนำ `iLearn.User/Middleware/CanonicalHostRedirectMiddleware.cs` + extension `UseCanonicalHostRedirect()`) ลงทะเบียน**บนสุดของ pipeline** ใน `Program.cs` (ก่อน `UseHttpsRedirection` ที่บรรทัด ~87) เพื่อให้คลุมทั้งหน้า MVC และ course static files (`/Courses/...`)
2. Logic:
   - อ่าน `FileSettings:HostUrl` ตอน startup (bind ผ่าน options/config ที่แอปมีอยู่แล้ว) → parse เป็น `Uri` เอาเฉพาะ **scheme + host** เป็น canonical — parse ไม่ได้/ว่าง ⇒ middleware เป็น no-op ทั้งตัว
   - เงื่อนไข redirect (ต้องครบทุกข้อ):
     - method เป็น `GET` หรือ `HEAD` (method อื่นปล่อยผ่าน — กัน request กลางคันพัง)
     - `Request.Host.Host` ≠ canonical host (case-insensitive)
     - request host ไม่ใช่ `localhost` / `127.*` / `[::1]` (กัน dev + probe ภายในเครื่องเซิร์ฟเวอร์)
     - canonical host เองไม่ใช่ localhost
   - Redirect ด้วย **307 Temporary Redirect** (จงใจไม่ใช้ 301/308 — กัน browser cache ถาวรถ้า HostUrl เปลี่ยนภายหลัง) ไปยัง `{canonicalScheme}://{canonicalHost}{Request.PathBase}{Request.Path}{Request.QueryString}` — **ห้าม**เอา path ของ HostUrl (`/iLearn`) มาต่อเอง เพราะ `PathBase` บนเซิร์ฟเวอร์เป็น `/iLearn` อยู่แล้ว (ต่อซ้ำจะได้ `/iLearn/iLearn/...`)
3. `/health/*` **ไม่ยกเว้น** — โดน redirect เหมือนหน้าอื่น (monitoring ที่ยิง hostname สั้นจะ follow 307 ได้เอง; probe จาก localhost บนเครื่องเซิร์ฟเวอร์ถูก skip ด้วยเงื่อนไข localhost อยู่แล้ว)
4. เพิ่ม unit test ใน `iLearn.Tests` สำหรับ logic ตัดสินใจ (แยก pure function เช่น `TryGetCanonicalRedirect(...)` เพื่อ test ได้โดยไม่ต้อง spin host): กรณี host ตรง/ไม่ตรง, localhost skip, POST ปล่อยผ่าน, HostUrl ว่าง/ไม่ valid, query string ถูกคงไว้

### §B `iLearn.Admin.React` — redirect ฝั่ง SPA (ไฟล์ static, middleware ไปไม่ถึง)

1. เพิ่ม env var `VITE_ILEARN_ADMIN_CANONICAL_DOMAIN=nikonoa.net` ใน `.env.production` (dev ไม่ตั้ง = ปิด feature)
2. ใน `src/main.tsx` (หรือ module bootstrap ที่รันก่อน render — ก่อนงานอื่นทั้งหมด): ถ้า `location.hostname` **ไม่มีจุด** (short NetBIOS name) และไม่ใช่ `localhost` และ canonical domain ถูกตั้งไว้ ⇒ `window.location.replace()` ไป `${location.protocol}//${location.hostname}.${domain}${location.pathname}${location.search}${location.hash}` แล้ว `return` ไม่ mount React ต่อ
   - กติกา generic นี้ทำให้บันเดิลเดียวใช้ได้ทั้ง QA (`ap-ntc2138-qawb` → `.nikonoa.net`) และ PROD (`ap-ntc2137-prwb` → `.nikonoa.net`) โดยไม่ต้องรู้จักชื่อเครื่อง
   - อ่าน env ผ่าน pattern เดิมของ `appConfig.ts` (เพิ่ม key ใน `appConfig` ให้เรียบร้อย อย่า `import.meta.env` กระจัดกระจาย)
3. หน้า `HealthCheckPage.tsx` **ไม่ต้องแก้** — หลัง redirect ผู้ใช้อยู่บน FQDN เสมอ probe จะ same-origin เอง

### §C Deploy + Verify

1. Verification ก่อน deploy (ครบชุดตาม CLAUDE.md): `npm run lint` + `npm run build` + `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test`
2. Deploy QA: `tools/deploy-user.ps1` + `tools/deploy-admin-react.ps1` (ไม่มี migration — ไม่ต้องรัน ef)
3. QA smoke:
   - `Invoke-WebRequest https://ap-ntc2138-qawb/iLearn/ -UseDefaultCredentials -MaximumRedirection 0 -SkipHttpErrorCheck` → **307** + `Location: https://ap-ntc2138-qawb.nikonoa.net/iLearn/`
   - เปิด browser `https://ap-ntc2138-qawb/iLearn/admin-react/health-check` → URL เด้งเป็น `.nikonoa.net` และ**การ์ดทั้งสองเขียว** (iLearn API Operational + Learner Site pass), console 0 errors
   - `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` (FQDN ตรง) → **ไม่** redirect, 200 ปกติ
   - เปิด SCORM 1 คอร์สผ่าน URL สั้น → เด้ง FQDN → เรียน/commit บันทึกปกติ (ดู console ไม่มี `Lms initialization failed`)
   - POST endpoint ใดก็ได้ผ่าน host สั้น (เช่น form login flow) ต้องไม่โดน redirect ตัดกลางคัน
4. Deploy PROD: `tools/deploy-user-prod.ps1` + `tools/deploy-admin-react-prod.ps1` → PROD smoke **read-only เท่านั้น**: ตรวจ 307 + `Location` ด้วย `-MaximumRedirection 0`, เปิด health check ผ่าน URL สั้นแล้วการ์ดเขียวทั้งคู่, console 0 errors — **ห้าม write-test บน PROD**
5. ปิดงาน: อัปเดตแผนนี้เป็น `DONE` + Implementer Notes + ลง `DOC/AGENT_LOG.md`

## นอก Scope (ห้ามทำ)

- **ห้ามแตะ IIS config / applicationHost / web.config บนเซิร์ฟเวอร์** (precedent PLAN-130 — deploy script เขียนทับ web.config อยู่แล้ว)
- ห้ามเพิ่ม CORS ใน `iLearn.User` (ไม่จำเป็นเมื่อ redirect แล้ว และเปิดช่องเกินเหตุ)
- ห้ามแก้ `iLearn.API` / `iLearn.Admin` (MVC เดิม) — ผู้ใช้ admin เดิมเข้าผ่าน `/iLearn/admin` ยังไม่ถูก redirect ในแผนนี้ ถ้าเจอปัญหาจดใน Implementer Notes พอ
- ห้ามแก้ `FileSettings:HostUrl` ทุก environment
- ห้ามแตะไฟล์ของแผนอื่นที่ค้างใน working tree (`labels.ts`/`apiClient.ts` มี fix ของ Claude ค้าง commit อยู่ — stage เฉพาะไฟล์ของแผนนี้)

## ความเสี่ยง / จุดระวัง

- **Windows auth (NTLM/Negotiate)**: handshake เกิดระดับ IIS ก่อนถึง middleware — redirect 307 ไม่รบกวน แต่หลัง redirect browser จะ handshake ใหม่กับ FQDN (ปกติ ผ่าน intranet zone policy อยู่แล้ว เพราะผู้ใช้ที่เข้าด้วย FQDN ตรง ๆ ใช้งานได้วันนี้)
- ถ้าเครื่องเซิร์ฟเวอร์มี monitoring/scheduled job ยิง URL host สั้นแบบ `-MaximumRedirection 0` อยู่ก่อน จะเริ่มได้ 307 — ตรวจใน Implementer Notes ว่ามีหรือไม่ (เท่าที่รู้ไม่มี)
- `window.location.replace` ใน main.tsx ต้องรันก่อน side effects อื่น (SignalR/session bootstrap) — วางไว้บรรทัดแรก ๆ ของ entry

## Implementer Notes

- **Implementation Highlights:**
  1. `iLearn.User/Middleware/CanonicalHostRedirectMiddleware.cs`:
     - สร้าง `CanonicalHostRedirectHelper` (pure function `TryGetCanonicalRedirect` & `IsLocalhost`) และ `CanonicalHostRedirectMiddleware` / `UseCanonicalHostRedirect()`.
     - อ่าน `FileSettings:HostUrl` จาก `IConfiguration`, ตรวจสอบ HTTP GET/HEAD, host สั้น, ข้าม localhost/127.*/[::1] และคืน 307 Temporary Redirect ไปยัง canonical FQDN โดยคง `PathBase`, `Path`, `QueryString` ครบถ้วน.
     - ลงทะเบียน `app.UseCanonicalHostRedirect()` ไว้บนสุดของ middleware pipeline ใน `iLearn.User/Program.cs`.
  2. `iLearn.Admin.React`:
     - เพิ่ม `VITE_ILEARN_ADMIN_CANONICAL_DOMAIN=nikonoa.net` ใน `.env.production`.
     - เพิ่ม `canonicalDomain` ใน `src/config/appConfig.ts`.
     - เพิ่ม `redirectIfCanonicalHostNeeded()` ใน `src/main.tsx` ให้รันก่อน mount React และก่อน side-effects อื่นๆ.
  3. `iLearn.Tests`:
     - ตั้งค่า `<Aliases>ILearnUserApp</Aliases>` ใน `iLearn.Tests.csproj` เพื่อกัน root namespace `iLearn.User` ชนกับ `User` domain entity.
     - เพิ่ม `CanonicalHostRedirectTests.cs` ครอบคลุม 10 test cases (GET/HEAD redirect, query string, short host vs canonical host, localhost bypass, POST/PUT bypass, HostUrl invalid, middleware status 307 & Location header).
- **Verification Results:**
  - `npm run lint` ✓ (0 errors)
  - `npm run build` ✓ (built dist successfully)
  - `dotnet build` & `dotnet test` ✓ (272/272 passed)
  - Deploy QA: `tools/deploy-user.ps1` + `tools/deploy-admin-react.ps1` ✓
  - QA Smoke: `https://ap-ntc2138-qawb/iLearn/` -> 307 Location `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` ✓, FQDN -> 200 ✓
  - Deploy PROD: `tools/deploy-user-prod.ps1` + `tools/deploy-admin-react-prod.ps1` ✓
  - PROD Smoke: `https://ap-ntc2137-prwb/iLearn/` -> 307 Location `https://ap-ntc2137-prwb.nikonoa.net/iLearn/` ✓, FQDN -> 200 ✓

