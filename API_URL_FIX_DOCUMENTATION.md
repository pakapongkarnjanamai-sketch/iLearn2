# API URL Fix - Double /api Path Issue

## ?? Problem Fixed

### Issue
```
Failed to load resource: the server responded with a status of 404 (Not Found)
:7128/api/api/courses/657/versions:1
```

### Root Cause
The `serviceUrl` variable already contains the base API path (with `/api`), but the code was appending `/api/` again, resulting in the double path.

---

## ?? Changes Made

### 1. VersionForm.cshtml - Line 327
**Before:**
```javascript
url: `${serviceUrl}/api/courses/versions/${VERSION_ID}`
```

**After:**
```javascript
url: `${serviceUrl}/courses/versions/${VERSION_ID}`
```

### 2. VersionForm.cshtml - Line 400-403
**Before:**
```javascript
const url = IS_EDIT 
    ? `${serviceUrl}/api/courses/versions/${VERSION_ID}`
    : `${serviceUrl}/api/courses/${COURSE_ID}/versions`;
```

**After:**
```javascript
const url = IS_EDIT 
    ? `${serviceUrl}/courses/versions/${VERSION_ID}`
    : `${serviceUrl}/courses/${COURSE_ID}/versions`;
```

### 3. Dashboard.cshtml - Line 166
**Before:**
```javascript
const courseApiUrl = serviceUrl + '/Courses';
```

**After:**
```javascript
const courseApiUrl = serviceUrl + '/courses';
```

---

## ?? Understanding serviceUrl

### Configuration
From `_DevExtremeLayout.cshtml`:
```javascript
const API_BASE = '@Configuration["ApiSettings:BaseUrl"]';
const serviceUrl = API_BASE;
```

### When to use `serviceUrl`
- ? For admin endpoints: `${serviceUrl}/admin/CourseVersionsCRUD/Get`
- ? For API endpoints: `${serviceUrl}/courses` (already includes `/api`)
- ? NOT: `${serviceUrl}/api/courses` (double `/api`)

### Correct URL Construction
| Endpoint | Correct URL |
|----------|-------------|
| List courses | `${serviceUrl}/courses` |
| Get course | `${serviceUrl}/courses/{id}` |
| List versions | `${serviceUrl}/courses/{courseId}/versions` |
| Get version | `${serviceUrl}/courses/versions/{versionId}` |
| Create version | `${serviceUrl}/courses/{courseId}/versions` |
| Update version | `${serviceUrl}/courses/versions/{versionId}` |
| Delete version | `${serviceUrl}/courses/versions/{versionId}` |
| Set active version | `${serviceUrl}/courses/{courseId}/versions/{versionId}/set-active` |

---

## ? Verified Endpoints

All endpoints now correctly formed:
- ? `/courses` not `/api/courses`
- ? Case-sensitive: `/courses` (lowercase)
- ? No double `/api/` path
- ? Proper parameter placement

---

## ?? How serviceUrl Works

### Example Configuration
If `ApiSettings:BaseUrl` = `http://localhost:7128/api`

Then:
- `serviceUrl` = `http://localhost:7128/api`
- Correct use: `${serviceUrl}/courses/1` = `http://localhost:7128/api/courses/1` ?
- Wrong use: `${serviceUrl}/api/courses/1` = `http://localhost:7128/api/api/courses/1` ?

---

## ?? Testing

After fix, verify these URLs work:

```
GET    http://localhost:7128/api/courses
GET    http://localhost:7128/api/courses/657
PATCH  http://localhost:7128/api/courses/657/versions/1/set-active
GET    http://localhost:7128/api/courses/657/versions
GET    http://localhost:7128/api/courses/versions/1
POST   http://localhost:7128/api/courses/657/versions
PUT    http://localhost:7128/api/courses/versions/1
DELETE http://localhost:7128/api/courses/versions/1
```

All should return 200/201/204 (not 404)

---

## ?? Reference

- `serviceUrl` is defined in `_DevExtremeLayout.cshtml`
- It includes the full base URL with `/api` path
- When using with API endpoints, only append the controller and action names
- Admin endpoints still use `serviceUrl + '/admin/...'` as before

---

**Status**: ? Fixed
**Files Modified**: 2
- iLearn.Admin/Views/Courses/VersionForm.cshtml
- iLearn.Admin/Views/Courses/Dashboard.cshtml
