# Course Version SCORM Processing - Implementation Update

## ?? Overview

???????? `CourseVersionService` ?????????????????????????????? SCORM files ????????????????/?????? Version

---

## ?? Changes Made

### 1. **CourseVersionService Dependencies**

**Added:**
```csharp
private readonly IGenericRepository<FileStorage> _fileStorageRepository;
private readonly IScormService _scormService;
```

**Updated Constructor:**
```csharp
public CourseVersionService(
    IGenericRepository<CourseVersion> versionRepository,
    IGenericRepository<CourseResource> courseResourceRepository,
    IGenericRepository<Resource> resourceRepository,
    IGenericRepository<FileStorage> fileStorageRepository,  // ? New
    ICourseRepository courseRepository,
    IScormService scormService)                              // ? New
```

---

### 2. **ProcessNewResourceAsync Method**

????? helper method ???????????????? uploaded files:

```csharp
private async Task<Resource> ProcessNewResourceAsync(IFormFile file)
```

**Step-by-step Process:**
1. ? Save file to FileStorage database
2. ? Create Resource entity (inactive initially)
3. ? Check if file is SCORM (.zip)
   - If YES: Extract and parse SCORM using `IScormService`
   - If NO: Activate file immediately
4. ? Update Resource with SCORM metadata (if applicable)
5. ? Return activated/processed Resource

**Handling Errors:**
- SCORM parsing failures: Leave resource inactive, re-throw exception
- This allows partial processing and clear error messaging

---

### 3. **CreateVersionAsync Update**

**Before:**
```csharp
if (resourceId == 0 && fileIndex < (files?.Count ?? 0))
{
    // ???????? - ???????????? support
    fileIndex++;
}
```

**After:**
```csharp
if (resourceId == 0 && fileIndex < (files?.Count ?? 0))
{
    // ???????? - ??????????????????
    var file = files[fileIndex];
    var newResource = await ProcessNewResourceAsync(file);
    
    if (newResource != null)
    {
        var courseResource = new CourseResource
        {
            CourseVersionId = newVersion.Id,
            ResourceId = newResource.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _courseResourceRepository.AddAsync(courseResource);
    }
    fileIndex++;
}
```

---

### 4. **UpdateVersionAsync Update**

Same process as CreateVersionAsync - now supports new file uploads

---

## ?? File Processing Flow

```
Upload File (IFormFile)
    ?
Save to FileStorage
    ?
Create Resource (Inactive)
    ?
Is SCORM (.zip)?
    ?? YES ? Extract & Parse SCORM
    ?         ?? Success? ? Update Resource metadata ? Activate
    ?         ?? Fail? ? Keep inactive ? Re-throw error
    ?
    ?? NO ? Activate immediately
         (Regular file)
    ?
Link to CourseVersion
    ?
Done ?
```

---

## ?? Database Schema

**FileStorage** (??? ??)
- Name: ???
- ContentType: MIME type
- Length: ?? ??
- Data: ?? ?? (binary)

**Resource**
- Name: ????
- TypeId: 1=Learn, 2=Exam
- IsActive: ??? ??
- FileStorageId: FileStorage FK
- **URL**: SCORM folder path (e.g., "uploads/scorm/guid")
- **ResourceHref**: SCORM manifest entry (e.g., "SCO_1")
- **SchemaVersion**: SCORM version (1.2, 2004)

**CourseResource**
- CourseVersionId: ?? FK
- ResourceId: ??? FK

---

## ?? How SCORM Processing Works

### Extract SCORM Package
```csharp
await _scormService.ExtractAndParseScormAsync(fileData, folderName)
// Returns: ScormManifestDto
// - FolderName: Where SCORM was extracted
// - ResourceHref: Entry point in manifest
// - SchemaVersion: SCORM version detected
```

### SCORM Metadata Storage
```
Resource
?? URL: "uploads/scorm/12345678-1234-1234..."  ? Folder path
?? ResourceHref: "SCO_1"                        ? Entry point
?? SchemaVersion: "1.2" or "2004"              ? Version info
```

### Access SCORM Content
```
GET /api/resources/{id}/content
? Checks if SCORM
? Returns: { url: GetScormUrl(folder, entry) }
? Client loads SCORM player with URL
```

---

## ?? Integration with VersionForm

VersionForm.cshtml sends files via FormData:

```javascript
// Create Version with Files
POST /api/courses/{courseId}/versions
Content-Type: multipart/form-data

FormData:
- courseId: 1
- note: "Version 2"
- isActive: true
- resourceIds: [1, 2, 0, 0]     ? 0 = new file
- Files: [file1.zip, file2.pdf]
```

---

## ? Error Handling

### SCORM Parse Error
```csharp
try
{
    await _scormService.ExtractAndParseScormAsync(...);
}
catch (InvalidScormPackageException ex)
{
    // Keep resource inactive
    savedResource.IsActive = false;
    await _resourceRepository.UpdateAsync(savedResource);
    
    // Notify caller
    throw; // Re-throw for API error response
}
```

### File Upload Error
```
BadRequest: "No file uploaded"
or
InternalServerError: "Error processing resource"
```

---

## ?? Response Examples

### Success Response
```json
{
  "id": 2,
  "courseId": 1,
  "versionNumber": 2,
  "note": "Version with SCORM",
  "isActive": false,
  "createdAt": "2025-01-20T10:30:00Z",
  "resources": [
    {
      "id": 5,
      "name": "course.zip",
      "typeId": 1,
      "typeName": "Learn",
      "isActive": true,
      "url": "uploads/scorm/12345678-..."
    },
    {
      "id": 1,
      "name": "resource1.pdf",
      "typeId": 1,
      "typeName": "Learn",
      "isActive": true,
      "url": null
    }
  ]
}
```

### Error Response
```json
{
  "error": "Invalid SCORM Package",
  "message": "Missing imsmanifest.xml"
}
```

---

## ?? Testing Steps

### 1. Upload SCORM and Regular Files
```
POST /api/courses/1/versions
- Add SCORM file (.zip)
- Add PDF file
- Add existing resource (ID)
```

### 2. Verify Processing
```
? SCORM extracted to disk
? Resource metadata saved
? Both files linked to version
```

### 3. Check Resource Status
```
GET /api/courses/1/versions/2
? SCORM: isActive: true, URL populated
? PDF: isActive: true, URL: null
```

### 4. Load Course Content
```
GET /api/resources/5/content
? SCORM: Returns { url: "..." }
? Client loads SCORM player
```

---

## ?? Security Considerations

? File size limit: 100MB (configured in ResourcesController)
? File type validation: .zip, .pdf, etc.
? SCORM validation: Checks manifest validity
? Temporary extraction: Automatic cleanup via ScormService
? Access control: Via existing auth middleware

---

## ?? Dependencies

```csharp
// Required interfaces already registered in Program.cs
- IGenericRepository<FileStorage>
- IScormService
- IGenericRepository<Resource>
- ICourseRepository
- IGenericRepository<CourseResource>
- IGenericRepository<CourseVersion>
```

---

## ?? Usage Example

### Create Version with New SCORM Files
```javascript
const formData = new FormData();
formData.append("courseId", 1);
formData.append("note", "Added SCORM v2");
formData.append("isActive", true);
formData.append("resourceIds", 1);    // Existing resource
formData.append("resourceIds", 0);    // New SCORM file
formData.append("files", scormFile);  // Actual file

fetch('/api/courses/1/versions', {
  method: 'POST',
  body: formData
})
.then(r => r.json())
.then(data => console.log('Version created:', data));
```

---

## ?? Key Features

? **Automatic SCORM Detection**: Extracts .zip files automatically
? **Metadata Capture**: Stores SCORM version and entry points
? **Mixed Resources**: Support both SCORM and regular files in one version
? **Error Recovery**: Failed SCORM stays inactive, doesn't break process
? **Streaming Response**: Large file uploads supported (100MB max)
? **Database Integration**: All metadata saved for content delivery

---

**Status**: ? Complete and Ready for Use
**Build**: Successful
**Tests**: Ready for QA
