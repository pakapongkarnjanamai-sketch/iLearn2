# Content And Course Version Lifecycle Rules

Last updated: 2026-04-30

## Purpose

Content and Course Version lifecycle controls whether a course can be opened, assigned, and launched by learners. Course status says whether the course accepts assignments or learner access; content/version readiness says whether there is something valid to launch.

## Content Item Lifecycle

| State | Stored/Derived From | Meaning | Learner Launch | Can Be Used In Active Version |
| --- | --- | --- | --- | --- |
| Queued Upload | UI-only before save | File selected in a course/version form but not yet persisted or processed | No | No |
| Draft / Unpublished | `ContentItem.IsActive = false` | Content item exists but is not published to launchable storage | No | No |
| Published | `IsActive = true` plus publish metadata | Content item has been processed or made public | Yes, if linked through a ready version | Yes |
| Ready | Derived by `CourseContentReadiness` | Published and has a launch URL; SCORM package has launch file unless URL is direct | Yes | Yes |
| Not Ready | Derived readiness issue | Missing record, not published, missing URL, or missing SCORM launch file | No | No |
| Unused Published | Published and not linked to active versions | Launch files exist but are not used by active learning | No normal learner path | Not currently |
| Should Publish | Unpublished but linked to active versions | Active version depends on content that is not launchable | No | This is a remediation state |
| Deleted | Base soft delete | Content item is removed from normal lists; history may still reference it | No | No |

## Content Item Transitions

### Upload To Draft

- Upload validates that the file is a SCORM zip package.
- `FileStorage` stores the original package bytes and metadata.
- `ContentItem` is created with `IsActive = false` and references `FileStorageId`.
- Launch fields are not reliable until publish succeeds.

### Draft To Published

- Publish extracts and parses the SCORM package.
- The package must contain exactly one valid `imsmanifest.xml`.
- Supported schema versions are SCORM 1.2 and SCORM 2004.
- Publish sets `LaunchHref`, `SchemaVersion`, `URL`, and `IsActive = true`.

### Published To Draft / Unpublished

- Unpublish deletes extracted server files when a URL exists.
- Unpublish clears `URL`, `LaunchHref`, and `SchemaVersion`.
- Single unpublish blocks content that is linked to course versions.
- Bulk unpublish now follows the same shared guard and shows an impact preview so linked content is skipped instead of being silently reverted to draft.

### Published Or Draft To Deleted

- Delete soft-deletes `ContentItem` so historical references can remain meaningful.
- If active server files exist, they are removed.
- The related `FileStorage` record can be hard-deleted when no other record needs the binary data.

## Course Version Lifecycle

| State | Stored/Derived From | Meaning | Course Impact |
| --- | --- | --- | --- |
| Inactive Version | `CourseVersion.IsActive = false` | Version is saved but not used for new launches | No direct learner launch unless enrollment explicitly points to it from past data |
| Active Version | `IsActive = true` | Version is the current version for course assignment/launch | Required before opening a course |
| Ready Version | Active or inactive version whose linked content is ready | Version can be activated or used by learners | Enables course opening and assignment eligibility |
| Not Ready Version | No content or at least one not-ready content item | Version cannot be activated safely | Blocks Open course and learner launch |
| Deleted Version | Version removed through version delete flow | Version should no longer be chosen | Historical enrollments should retain meaningful version references where possible |

## Course Version Activation Rules

1. A version must contain at least one linked content item.
2. Every linked content item must be ready.
3. Activating a version deactivates other active versions for the same course.
4. Activating a version on a Draft course can open the course automatically.
5. Deactivating the only active version can move an Open course back to Draft.
6. Closed and Retired courses should not become Open only because a version is activated.
7. Backend readiness validation is the final guard even if Admin UI has pre-checks.

## Learner Version Policy

When a new active version is introduced, existing learners are handled by `CourseVersionLearnerPolicy`.

| Policy | Meaning | Affects Not Started | Affects In Progress | Affects Completed |
| --- | --- | --- | --- | --- |
| NewLearnersOnly | Existing learners stay on their enrolled version | No | No | No |
| MoveNotStarted | Move only learners who have not started | Yes | No | No |
| ResetInProgress | Move and reset open learners in current in-progress assignments | Yes | Yes | No |

Eligibility is limited to open learners in assignments that are currently in progress by schedule. Upcoming, expired, deleted, completed, or snapshot-completed links are not moved.

## Readiness Issues

| Issue | Meaning | Typical Admin Action |
| --- | --- | --- |
| content item record is missing | Linked content item cannot be found | Remove/replace the linked item |
| content item is not published | `IsActive = false` | Publish the content item or keep version inactive |
| launch URL is missing | Content has no launch storage path/URL | Re-publish or repair storage |
| SCORM launch file is missing | `LaunchHref` is absent and URL is not a direct launch URL | Re-import or repair package metadata |
| original SCORM package is missing | Auto-prepare cannot find stored package bytes | Re-upload package |
| automatic content preparation failed | Server attempted preparation but failed | Inspect error, package, and storage |

## Admin UI Rules

- User-facing labels should say `Content`, `Content item`, and `Content library`.
- Backend entity/API names can remain `ContentItem`.
- Course/version pickers should show `Published`, `Not Ready`, and `Queued Upload` consistently.
- Active-version save should offer a clear fallback: keep the version inactive until content is ready.
- Readiness warnings should list the affected content items and exact issue reasons.

## Current Contract Notes

- Content item DTO responses should expose `PublishState` and `IsPublished` in addition to raw `IsActive`.
- Content library lookup/list responses should also expose `PublishState` and `IsPublished` when they project content items locally in controllers.
- `ContentItemsCRUDController.Get/{id}` should return `ContentItemDto`, not a raw `ContentItem` entity.
- Course version DTO responses should expose `VersionState` in addition to raw `IsActive`.
- `CourseVersionsCRUDController.Get/{id}` should return `CourseVersionDto` with nested `CourseContentItemDto`, not raw `CourseVersion` entities.
- `IsActive` remains for compatibility, but clients should prefer `PublishState`, `IsPublished`, and `VersionState` when rendering domain status.
- Single publish, bulk publish, single unpublish, and bulk unpublish now run through the same content publication policy so linked-content guards cannot drift.
- Bulk unpublish preview responses should distinguish eligible content from blocked content and show linked course references before destructive maintenance actions run.

## Recommendations

1. If operations ever need to override blocked unpublish guards, add an explicit force flow separate from the default safe preview path.
2. Extend more content/version responses with explicit readiness-oriented fields such as `ReadinessStatus` so clients do not need to infer everything from `IsActive`, `URL`, and `LaunchHref`.
3. Keep the original uploaded package in `FileStorage` as long as auto-prepare is a supported recovery path.
4. Add tests that active course versions cannot reference unpublished content after any bulk maintenance action.
