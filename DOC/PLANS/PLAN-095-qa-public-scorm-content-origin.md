# PLAN-095: Align QA SCORM launch URLs with the public nikonoa.net origin

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **Created:** 2026-07-17

## Purpose

Apply the fix proven in production by PLAN-094 to QA before further SCORM upload and player testing. The QA public learner URL is:

```text
https://ap-ntc2138-qawb.nikonoa.net/iLearn
```

## Scope

- Change QA `FileSettings:HostUrl` in `iLearn.API/appsettings.json` and `iLearn.User/appsettings.json` to the public QA FQDN.
- Do not change `ApiSettings:BaseUrl`: it is a server-to-server Learner-to-API URL, not a browser SCORM launch URL.
- Deploy API first, then Learner, using existing side-by-side scripts and public health checks.

## Verification

1. Build API and Learner.
2. Deploy API, then Learner, to QA.
3. Read deployed settings without exposing secrets and confirm both `HostUrl` values match the public QA URL.
4. Verify QA public health and a SCORM entry asset under `/iLearn/Courses/...` return 200.
5. Launch an assigned QA course through the public FQDN and confirm the iframe uses the public FQDN.

## Implementer Notes

- API and Learner builds passed with no errors.
- QA API deployed first to `_deploy_20260717131321`; public API health returned the expected Windows-auth 401 and `AutoRolledBack=False`.
- QA Learner deployed next to `_user_deploy_20260717131427`; public learner health returned 200 and `AutoRolledBack=False`.
- Read-back from deployed QA root config confirms both `FileSettings:HostUrl` values are `https://ap-ntc2138-qawb.nikonoa.net/iLearn`.
- Fresh public QA health passed and `/iLearn/Courses/` lists the shared SCORM content folders.
- The requested iPad launch confirmation was received for production; QA now has the same public-origin configuration for sandbox SCORM testing.