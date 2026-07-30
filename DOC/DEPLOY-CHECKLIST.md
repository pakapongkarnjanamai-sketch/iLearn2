# Deploy And Rollback Checklist

Use this checklist for every production deploy to `\\10.10.143.39\wwwroot\iLearnNew`.

## 1. Before Deploy

- Open PowerShell 7 (`pwsh`), not Windows PowerShell 5.1.
- Run commands from the repository root.
- Confirm the target project builds locally.
- Confirm the production UNC share is reachable.
- Confirm the target app has a valid `web.config` in the deploy root.
- Decide the target app: `User`, `API`, or `Admin`.
- Record the current live deploy folder from `web.config` before switching.
- For SCORM changes, record the public learner origin and confirm `FileSettings:HostUrl` in both API and Learner configuration use that same public FQDN (for example, `https://<host>.nikonoa.net/iLearn`). Do not use the internal short host name for browser launch URLs.

Recommended local validation:

```powershell
dotnet build .\iLearn.Admin\iLearn.Admin.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.Admin
dotnet build .\iLearn.API\iLearn.API.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.API
dotnet build .\iLearn.User\iLearn.User.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.User
```

## 2. Dry-Run

Run a dry-run first when you want to verify paths, deploy root, and `web.config` switching.

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-admin.ps1 -SkipPublish -WhatIf
```

## 3. Deploy

User:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1
```

API:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1
```

Admin:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\deploy-admin.ps1
```

Optional parameters:

- `-Configuration Debug|Release`
- `-Stamp yyyyMMddHHmmss` to force a known deploy folder name
- `-SkipPublish` to reuse an existing publish output

## 4. Record The Output

Always capture these values from the script output:

- `DeployPath`
- `WebConfigPath`
- `WebConfigArguments`
- `Stamp`

These values are required for verification and rollback.

## 5. Verify Immediately

- Open the real app URL after the switch.
- Test the exact page or workflow changed in this deploy.
- Test at least one export or write action if the release touched those areas.
- Check browser console and server logs if anything looks stale or broken.
- For SCORM, sign in through the public learner URL and launch one assigned item. Confirm the iframe loads from `<public-origin>/Courses/...` and the entry document returns 200. Open Player through canonical public URL and verify iframe origin matches page origin, complete 1 assigned item, and confirm progress is persisted.

Typical production roots:

- User: `\\10.10.143.39\wwwroot\iLearnNew`
- API: `\\10.10.143.39\wwwroot\iLearnNew\Service`
- Admin: `\\10.10.143.39\wwwroot\iLearnNew\admin`

## 6. Rollback

Rollback is a `web.config` switch back to the previous side-by-side folder.

1. Open the target app's `web.config`.
2. Find the `aspNetCore` node.
3. Change the `arguments` attribute back to the previous deploy folder.
4. Save `web.config`.
5. Reload the production app and re-test the failing workflow.

Example rollback targets:

- User: `.\\_user_deploy_<old-stamp>\\iLearn.User.dll`
- API: `.\\_deploy_<old-stamp>\\iLearn.API.dll`
- Admin: `.\\_admin_deploy_<old-stamp>\\iLearn.Admin.dll`

## 7. After Rollback Or Deploy

- Record the final live stamp in the ticket, release note, or team chat.
- Keep the previous deploy folder until the new release is confirmed stable.
- Do not delete old side-by-side folders during the same deploy session.

## 8. Emergency: Split iLearn App Pools (Production)

Use this when `/iLearn/` returns `HTTP Error 500.35 - ASP.NET Core does not support multiple apps in the same app pool`, or when a partial workaround returns `HTTP Error 500.34 - ASP.NET Core does not support mixing hosting models`.

Do not put all `/iLearn*` applications into one shared app pool. ASP.NET Core in-process hosting supports only one app per app pool. The stable production mapping is one process boundary per ASP.NET Core app:

| IIS application | App pool | Hosting model |
|---|---|---|
| `/iLearn` | `iLearn.User` | `inprocess` |
| `/iLearn/Service` | `iLearn.Service` | `inprocess` |
| `/iLearn/admin` | `iLearn.Admin` | `inprocess` |
| `/iLearn/admin-react` | `iLearn.Admin.React` | static app pool |
| `/iLearn/student` | `iLearn.Static` | static/redirect app pool, if present |

Audit first:

```powershell
pwsh -NoLogo -NoProfile -File .\tools\set-ilearn-prod-app-pools.ps1 -AuditOnly
```

If the current shell is not an IIS admin on `ap-ntc2137-prwb`, the audit itself will return WinRM `Access is denied`; rerun it with `-IisCredential`.

Apply with an IIS admin credential when the current shell has no remote IIS rights. If newly-created pools must run as a fixed service account, pass that account as `-AppPoolCredential`; do not print or paste the password into chat/logs.

**Call the script with `&` from the current PowerShell 7 session — never through a nested `pwsh -File`.** `-File` passes every argument as a string, so a `PSCredential` object arrives as the literal text `System.Management.Automation.PSCredential` and authentication fails with a confusing error.

```powershell
$iisCredential = Get-Credential
$appPoolCredential = Get-Credential

& .\tools\set-ilearn-prod-app-pools.ps1 `
    -AuditOnly `
    -IisCredential $iisCredential

& .\tools\set-ilearn-prod-app-pools.ps1 `
    -IisCredential $iisCredential `
    -AppPoolCredential $appPoolCredential
```

The audit prints the current mapping first, then warns if several ASP.NET Core apps share one pool (the 500.35 topology). The apply run re-reads the bindings from IIS afterwards and fails if the split did not take.

After applying, verify:

- `GET https://ap-ntc2137-prwb/iLearn/` returns 200.
- `GET https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me` returns 200 with Windows credentials and 401 without credentials.
- `GET https://ap-ntc2137-prwb/iLearn/admin-react/` returns 200.

If IIS admin access is temporarily unavailable and production is down, a reversible mitigation is to set the active ASP.NET Core `web.config` files for `/iLearn`, `/iLearn/Service`, and `/iLearn/admin` to the same `hostingModel="outofprocess"`. This avoids 500.35/500.34 but is not the preferred steady state; split the app pools and return to `inprocess` afterward.

### Deploy preflight

`deploy-side-by-side.ps1` audits this topology before every PROD deploy of `/iLearn`, `/iLearn/Service`, and `/iLearn/admin`:

- Wrong `-AppPoolName` for the deploy root fails immediately, without touching the server.
- If the remote audit finds ASP.NET Core apps sharing a pool, or a pool bound to the wrong app, the deploy is **blocked** — passing `-IisCredential` is not required for that.
- If the audit cannot run at all (WinRM refused, access denied, host unreachable) it only warns; pass `-IisCredential` to make an unreachable audit fail the deploy too.
- `-Rollback` skips the preflight entirely. Rollback only repoints `web.config` at the previous stamp folder, so it stays available while the topology is broken.
