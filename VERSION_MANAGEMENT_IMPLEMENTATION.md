# Version Management API - Implementation Summary

## ? ??????????????

### 1. ????? Service Layer
- ? `ICourseVersionService` interface
- ? `CourseVersionService` implementation
- ? Method implementations:
  - `GetVersionByIdAsync` - ????????? Version
  - `GetCourseVersionsAsync` - ????????? Versions ??? Course
  - `CreateVersionAsync` - ????? Version ????
  - `UpdateVersionAsync` - ?????? Version
  - `DeleteVersionAsync` - ?? Version
  - `SetActiveVersionAsync` - ???? Version ???? Active

### 2. ????? API Endpoints
????? 6 endpoints ?? `CoursesController`:

```
? GET    /api/courses/{courseId}/versions              - Get all versions
? GET    /api/courses/versions/{versionId}             - Get version details
? POST   /api/courses/{courseId}/versions              - Create version
? PUT    /api/courses/versions/{versionId}             - Update version
? DELETE /api/courses/versions/{versionId}             - Delete version
? PATCH  /api/courses/{courseId}/versions/{versionId}/set-active - Set active
```

### 3. ????? DTOs
- ? `CourseVersionDto` - ?????? Response
- ? `CreateCourseVersionDto` - ?????? Request (??????)

### 4. ???????? VersionForm.cshtml
- ? ?????????? admin endpoint ????? API ????
- ? Update load endpoint: `/admin/CourseVersionsCRUD/Get/{id}` ? `/api/courses/versions/{versionId}`
- ? Update create endpoint: `/courses/CreateVersion` ? `/api/courses/{courseId}/versions`
- ? Update save endpoint: `/courses/UpdateVersion/{id}` ? `/api/courses/versions/{versionId}`
- ? Better error handling

### 5. ????????? Service
- ? `ICourseVersionService` registered in `Program.cs`

---

## ?? API Endpoints Reference

### 1. Get All Versions for Course

**Request:**
```
GET /api/courses/{courseId}/versions
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "courseId": 1,
      "versionNumber": 1,
      "note": "Initial version",
      "isActive": true,
      "createdAt": "2025-01-16T00:00:00Z",
      "resources": [...]
    }
  ]
}
```

### 2. Get Version Details

**Request:**
```
GET /api/courses/versions/{versionId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "courseId": 1,
    "note": "Initial version",
    "isActive": true,
    "resourceIds": [1, 2, 3]
  }
}
```

### 3. Create New Version

**Request:**
```
POST /api/courses/{courseId}/versions
Content-Type: multipart/form-data

Parameters:
- courseId: int (in URL)
- note: string (required)
- isActive: boolean
- resourceIds: int[] (array)
- files: IFormFile[] (optional)
```

**Response:**
```json
{
  "success": true,
  "message": "???????????????????????",
  "data": {
    "id": 2,
    "courseId": 1,
    "versionNumber": 2,
    "note": "Version 2",
    "isActive": false,
    "createdAt": "2025-01-17T00:00:00Z",
    "resources": [...]
  }
}
```

### 4. Update Version

**Request:**
```
PUT /api/courses/versions/{versionId}
Content-Type: multipart/form-data

Parameters:
- courseId: int
- note: string
- isActive: boolean
- resourceIds: int[] (array)
- files: IFormFile[] (optional)
```

**Response:**
```json
{
  "success": true,
  "message": "????????????????????",
  "data": { ... }
}
```

### 5. Delete Version

**Request:**
```
DELETE /api/courses/versions/{versionId}
```

**Response:**
```json
{
  "success": true,
  "message": "????????????????"
}
```

### 6. Set Active Version

**Request:**
```
PATCH /api/courses/{courseId}/versions/{versionId}/set-active
```

**Response:**
```json
{
  "success": true,
  "message": "??????????????????????????????"
}
```

---

## ?? VersionForm.cshtml Changes

### Old Endpoints ? New Endpoints

| Operation | Old | New |
|-----------|-----|-----|
| Load | `/admin/CourseVersionsCRUD/Get/{id}` | `/api/courses/versions/{versionId}` |
| Create | `/courses/CreateVersion` | `/api/courses/{courseId}/versions` |
| Update | `/courses/UpdateVersion/{id}` | `/api/courses/versions/{versionId}` |

### JavaScript Changes
```javascript
// Load version data
const url = `${serviceUrl}/api/courses/versions/${VERSION_ID}`;

// Create version
const url = `${serviceUrl}/api/courses/${COURSE_ID}/versions`;

// Update version
const url = `${serviceUrl}/api/courses/versions/${VERSION_ID}`;

// Method changes
const method = IS_EDIT ? "PUT" : "POST";
```

---

## ?? Features

### Version Management
- ? Create new versions with auto-incrementing version numbers
- ? Update version details and resources
- ? Delete versions
- ? Set version as active (only one version can be active per course)
- ? Automatic deactivation of previous active version

### Resource Management
- ? Add existing resources to versions
- ? Support for multiple resources per version
- ? Resource ordering (via drag-and-drop in UI)

### Auto-Management
- ? Automatic version number calculation
- ? Automatic handling of active versions
- ? Cascade delete of course resources when version is deleted

---

## ?? Files Modified/Created

### Created
- `iLearn.Application/Interfaces/Services/ICourseVersionService.cs`
- `iLearn.Application/Services/CourseVersionService.cs`

### Modified
- `iLearn.API/Controllers/CoursesController.cs` (added 6 endpoints)
- `iLearn.API/Program.cs` (registered service)
- `iLearn.Admin/Views/Courses/VersionForm.cshtml` (updated endpoints)
- `iLearn.Application/DTOs/CourseDetailDto.cs` (added CourseVersionDto)

---

## ? Key Improvements

| Aspect | Before | After |
|--------|--------|-------|
| Architecture | Direct endpoint calls | Service-based API |
| Error Handling | Basic | Comprehensive with messages |
| Version Management | Admin endpoint | Service + API endpoints |
| Code Organization | Mixed concerns | Clear separation of layers |
| Testability | Limited | Service can be easily tested |
| Scalability | Hard to extend | Easy to add new features |

---

## ?? Data Consistency

The service ensures:
1. Only one version per course can be active at a time
2. Version numbers are auto-incremented
3. Deleting a version removes all its resources
4. Inactive versions don't affect course operations
5. Active version is the default version served to users

---

## ?? Testing Checklist

- [ ] Create new version without resources
- [ ] Create new version with resources
- [ ] Update version details
- [ ] Update version resources
- [ ] Set different version as active
- [ ] Verify only one version is active at a time
- [ ] Delete version
- [ ] Load version details correctly
- [ ] Handle error cases (version not found, etc.)

---

## ?? Clean Architecture Compliance

? Follows Clean Architecture principles:
- Service layer handles business logic
- Controller acts as thin wrapper
- DTOs separate API contracts from domain models
- Dependency injection through constructor
- Error handling and validation
- Async/await for all operations
