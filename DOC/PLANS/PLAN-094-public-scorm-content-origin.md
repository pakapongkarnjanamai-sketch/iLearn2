# PLAN-094: Use the public nikonoa.net origin for learner SCORM content

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **Created:** 2026-07-17

## Scope

Update the production `FileSettings:HostUrl` used to construct SCORM launch URLs from the internal host name to the public learner origin:

```text
https://ap-ntc2137-prwb.nikonoa.net/iLearn
```

This change applies to both the API (which creates `PlayerContentItemDto.LaunchUrl`) and the learner application (which exposes the same mounted content origin through configuration and health diagnostics).

## Incident Summary

| Item | Detail |
| --- | --- |
| Symptom | The learner page opened through `nikonoa.net` on iPad, but the selected SCORM content did not display. |
| Root cause | The API generated `launchUrl` values from `FileSettings:HostUrl`, which still used the internal short host name. The Player iframe therefore navigated away from the public `nikonoa.net` origin. |
| Evidence | The public `/iLearn/Courses/.../res/index.html` URL returned SCORM content, while the production configuration still contained the internal host name. |
| Resolution | Set `FileSettings:HostUrl` in both API and Learner production configuration to the public learner origin, then deploy API before Learner. |
| Prevention | Keep API and Learner public SCORM origins identical in each environment and verify a real launch URL through the public hostname after deployment. |

## Evidence

- `GET https://ap-ntc2137-prwb.nikonoa.net/iLearn/health` passes and reports that `D:\\iLearnContent\\Courses` is mounted.
- `GET https://ap-ntc2137-prwb.nikonoa.net/iLearn/Courses/<content-id>/res/index.html` returns a SCORM entry document.
- `GET .../Enrollments/player-info/{courseId}` creates each launch URL with `IScormService.GetScormUrl`, which uses `FileSettings.FileUrl`.
- Before this change, production `FileSettings:HostUrl` was `https://ap-ntc2137-prwb/iLearn`; an iPad using the public FQDN can load the learner page but receives the internal-host launch URL in its iframe.

## Deployment and Verification

1. Build `iLearn.API` and `iLearn.User`.
2. Deploy API first with `tools/deploy-api-prod.ps1`, then deploy learner with `tools/deploy-user-prod.ps1`.
3. Verify `GET https://ap-ntc2137-prwb.nikonoa.net/iLearn/health` returns 200.
4. Sign in through the public URL on an iPad, open an assigned SCORM item, and verify the iframe request starts with `https://ap-ntc2137-prwb.nikonoa.net/iLearn/Courses/` and its entry document returns 200.

## Implementer Notes

- Build verification passed: `iLearn.API` and `iLearn.User` both built successfully (existing nullable warnings only; no errors).
- API deployed first to production stamp `_deploy_20260717130658`; public API health returned 200 on the first attempt and `AutoRolledBack=False`.
- Learner deployed next to production stamp `_user_deploy_20260717130840`; public learner health returned 200 on the first attempt and `AutoRolledBack=False`.
- Read-back from the deployed production root configs confirms both API and Learner now use `https://ap-ntc2137-prwb.nikonoa.net/iLearn` as `FileSettings:HostUrl`.
- Public SCORM entry asset `https://ap-ntc2137-prwb.nikonoa.net/iLearn/Courses/001ea64d-8146-494b-892c-95bb04f83309/res/index.html` returns content successfully.
- User confirmed that launching SCORM content through the public URL now works on iPad.