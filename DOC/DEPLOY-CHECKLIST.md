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