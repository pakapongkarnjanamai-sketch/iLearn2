# Version Service SCORM Support - Summary

## ? ??????????????

### 1. ????? Dependencies ?? CourseVersionService
```csharp
// Dependencies ????
private readonly IGenericRepository<FileStorage> _fileStorageRepository;
private readonly IScormService _scormService;
```

### 2. ????? ProcessNewResourceAsync Method
```csharp
private async Task<Resource> ProcessNewResourceAsync(IFormFile file)
```

?????????:
- ? Save file to FileStorage
- ? Create Resource entity
- ? Detect SCORM files (.zip)
- ? Extract and parse SCORM metadata
- ? Update Resource with metadata
- ? Activate resource when ready

### 3. ???????? CreateVersionAsync
- ? ?????????????? (resourceId == 0)
- ? ????? ProcessNewResourceAsync
- ? Link resources to version

### 4. ???????? UpdateVersionAsync
- ? ????????? CreateVersionAsync
- ? ?????????????? version resources

---

## ?? Process Flow

```
Upload Files (IFormFile[])
    ?
For each file with resourceId == 0:
    ?? ProcessNewResourceAsync(file)
    ?   ?? Save to FileStorage
    ?   ?? Create Resource (inactive)
    ?   ?? Is .zip? ? Extract SCORM
    ?   ?   ?? Success ? Activate + metadata
    ?   ?   ?? Fail ? Keep inactive + rethrow
    ?   ?? Is other? ? Activate immediately
    ?
    ?? Link to CourseVersion
```

---

## ?? API Endpoints

### Create Version with SCORM
```
POST /api/courses/{courseId}/versions
Content-Type: multipart/form-data

Body:
- courseId: 1
- note: "Version 2"
- isActive: true
- resourceIds: [1, 0]        ? 0 = new file
- files: [scorm.zip]
```

### Response
```json
{
  "id": 2,
  "courseId": 1,
  "versionNumber": 2,
  "resources": [
    {
      "id": 1,
      "name": "existing.pdf",
      "isActive": true
    },
    {
      "id": 5,
      "name": "scorm.zip",
      "isActive": true,
      "url": "uploads/scorm/guid"
    }
  ]
}
```

---

## ?? SCORM Processing Details

### Extraction
```csharp
var scormInfo = await _scormService.ExtractAndParseScormAsync(
    fileData,           // Binary file content
    folderName          // Unique folder ID
);
```

### Metadata Stored
```csharp
resource.URL = scormInfo.FolderName;           // "uploads/scorm/guid"
resource.ResourceHref = scormInfo.ResourceHref; // "SCO_1"
resource.SchemaVersion = scormInfo.SchemaVersion; // "1.2"
```

### Content Retrieval
```
GET /api/resources/5/content
? IsActive && IsSCORM ? Return SCORM URL
? Client loads in SCORM player
```

---

## ?? Error Handling

### SCORM Validation Fails
```
InvalidScormPackageException
? Resource saved but inactive
? Exception re-thrown
? API returns 400 BadRequest
```

### File Upload Fails
```
BadRequest: "No file uploaded"
InternalServerError: "Error processing resource"
```

---

## ?? Integration Testing

```javascript
// Test Create Version with SCORM
const formData = new FormData();
formData.append("courseId", 1);
formData.append("note", "Test SCORM");
formData.append("isActive", false);
formData.append("resourceIds", 0);        // New SCORM
formData.append("files", scormZipFile);

const response = await fetch('/api/courses/1/versions', {
  method: 'POST',
  body: formData
});

const version = await response.json();
// Check: version.resources[0].isActive === true
// Check: version.resources[0].url !== null
```

---

## ?? Database Changes

### FileStorage Table
- ? Stores binary file content
- ? Tracks filename and content type

### Resource Table
- ? **URL**: SCORM extraction folder path
- ? **ResourceHref**: SCORM entry point
- ? **SchemaVersion**: SCORM version
- ? **IsActive**: Activation status

### CourseResource Table
- ? Links resources to versions
- ? Maintains order (if needed)

---

## ?? VersionForm Integration

### UI Flow
1. User selects/uploads files
2. Form collects resourceIds and files
3. POST to `/api/courses/{id}/versions`
4. Service processes files
5. Resources created and linked
6. Version returned with resources

### JavaScript Code
```javascript
const formData = new FormData();
formData.append("courseId", COURSE_ID);
formData.append("note", data.note);
formData.append("isActive", data.isActive);

// Existing resources
resourceIds.forEach(id => {
  formData.append("resourceIds", id);
});

// New files
files.forEach(file => {
  formData.append("files", file);
  formData.append("resourceIds", 0); // 0 = new
});

fetch(`/api/courses/${COURSE_ID}/versions`, {
  method: 'POST',
  body: formData
});
```

---

## ? Benefits

? **Seamless Upload**: Upload SCORM during version creation
? **Auto Processing**: SCORM extracted and validated automatically
? **Metadata Capture**: Version/entry point stored for playback
? **Error Safe**: Invalid SCORM doesn't break version creation
? **Flexible**: Mix SCORM and regular files in same version
? **Clean Code**: ProcessNewResourceAsync centralizes logic

---

## ?? Checklist

- ? Dependencies added
- ? ProcessNewResourceAsync created
- ? CreateVersionAsync updated
- ? UpdateVersionAsync updated
- ? Error handling implemented
- ? Build successful
- ? Documentation complete

---

**Status**: Ready for Testing ?
**Build**: Success ?
**Files Modified**: 1 (CourseVersionService.cs)
