# ?? Course Dashboard - Quick Start Guide

## ?? ????????????

???? Course Dashboard ???????????????????????????? API ?????????? Clean Architecture ??? `ICourseService`

---

## ?? API Endpoints ??????

### Core Operations (??? ICourseService)
```
? GET    /api/courses/{id}                    - Load course details
? PUT    /api/courses/{id}                    - Update course info
? DELETE /api/courses/{id}                    - Delete course
? PATCH  /api/courses/{id}/status             - Publish/Close course
? POST   /api/courses/{id}/assign-now         - Trigger assignment
? PATCH  /api/courses/{courseId}/versions/{versionId}/set-active  - Set active version
```

### Admin Operations (??? Admin CRUD - ????????????)
```
? GET    /admin/CourseVersionsCRUD/Get        - Load versions
? GET    /admin/CourseResourcesCRUD/Get       - Load resources
? DELETE /admin/CourseVersionsCRUD/Delete     - Delete version
```

---

## ?? Feature List

### 1?? **Course Information** (Card ????????)
- ? Display course code, name, description, category, type
- ? Edit and save course information
- ? Real-time form validation
- ? Error notifications

### 2?? **Quick Actions** (Card ???????)
| Action | Condition | API |
|--------|-----------|-----|
| **New Version** | Always enabled | Redirect to VersionForm |
| **Assignments** | Only if published | POST assign-now |
| **Publish/Close** | Always available | PATCH status |
| **Delete Course** | Only if not published | DELETE |

### 3?? **Versions History** (Bottom Card)
- ? Display all course versions
- ? Show active version with badge
- ? Set another version as active
- ? Delete version with confirmation
- ? Preview resources in each version

---

## ?? Usage Flow

### A. Load & Edit Course
```
1. Open Dashboard for Course ID = X
   ?
2. API loads: GET /api/courses/X
   ?
3. Form populate with course data
   ?
4. Edit fields (courseCode, courseName, etc.)
   ?
5. Click "Save Changes"
   ?
6. API call: PUT /api/courses/X
   ?
7. Success notification shown
```

### B. Publish Course
```
1. Click "Publish Course" button
   ?
2. Status changes to "Active" (with lock icon)
   ?
3. API call: PATCH /api/courses/X/status
   ?
4. Button text changes to "Close Course"
   ?
5. "Delete" button becomes disabled
   ?
6. "Assignments" button becomes enabled
```

### C. Trigger Assignment
```
1. Ensure course is published (Active status)
   ?
2. Click "Assignments" button
   ?
3. Confirmation dialog appears
   ?
4. User confirms
   ?
5. API call: POST /api/courses/X/assign-now
   ?
6. Success notification shown
   ?
7. Assignment process starts in background
```

### D. Manage Versions
```
1. Scroll to "Versions History" section
   ?
2. Each version card shows:
   - Version number
   - Description/Note
   - List of resources
   - Status (Active/Inactive)
   ?
3. For inactive version:
   - Click "?" menu (ellipsis)
   - Select "Set Active"
   - Confirm
   - API: PATCH /api/courses/X/versions/Y/set-active
   ?
4. Version becomes active (shows blue badge)
```

---

## ?? JavaScript Functions Reference

### Core Functions

#### `loadVersions()`
- Loads all versions for the course
- Calls `/admin/CourseVersionsCRUD/Get`
- Renders version cards
- Attaches event handlers

#### `loadVersionResources(versionId)`
- Loads resources for specific version
- Calls `/admin/CourseResourcesCRUD/Get`
- Shows resource list with preview button

#### `updateStatusUI(isActive)`
- Updates button states based on course status
- Enables/disables action buttons
- Changes icon and text

#### `setActiveVersion(versionId)` ? NEW
- Sets a version as active
- Calls `PATCH /api/courses/{courseId}/versions/{versionId}/set-active`
- Reloads versions after success

#### `deleteVersion(versionId)`
- Deletes a version
- Calls `/admin/CourseVersionsCRUD/Delete`
- Shows confirmation dialog

#### `viewContent(resourceId, resourceName)`
- Preview content (SCORM or file)
- Handles ZIP files specially
- Opens in new window

---

## ?? UI/UX Features

### Status Badges
```
? Active:    Primary (Blue) with check icon
? Inactive:  Secondary (Gray) with lock icon
```

### Action Cards
```
? Hover effect: Lift up + Blue border + shadow
? Disabled state: Opacity 50% + Not clickable
? Delete button: Red icon when enabled
```

### Form Validation
```
? Required fields marked with red asterisk
? Validation before submit
? Error messages on field
```

### Notifications
```
? Success: Green notification (2-3 seconds)
? Error: Red notification with error message
? Warning: Yellow notification for warnings
```

---

## ?? Configuration

### API Base URL
```javascript
const courseApiUrl = serviceUrl + '/Courses';
// Example: https://localhost:7270/api/Courses
```

### Course Types
```javascript
const courseTypes = [
    { id: 0, name: 'Special' },
    { id: 1, name: 'General' }
];
```

### Categories
```javascript
const categoriesStore = createDataStore(
    serviceUrl,
    'admin/CategoriesCRUD',
    { key: 'id' }
);
```

---

## ?? Error Handling

### Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| "?????????????" | Course ID not found | Check course exists in database |
| "Error updating course" | API validation fails | Check required fields are filled |
| "????????? Publish ???" | No active resources | Add resources to active version |
| "Cannot delete active course" | Trying to delete published | Close course first |

---

## ?? Testing Checklist

### Basic CRUD
- [ ] Load course data into form
- [ ] Edit course information
- [ ] Save changes successfully
- [ ] Refresh and verify data persists
- [ ] Delete course and redirect works

### Status Management
- [ ] Publish course (status becomes Active)
- [ ] Close course (status becomes Inactive)
- [ ] Verify buttons enable/disable correctly
- [ ] Verify warning message appears when needed

### Versions & Resources
- [ ] Load versions displays correctly
- [ ] Resources load for each version
- [ ] Set active version works
- [ ] Delete version works with confirmation

### Assignment
- [ ] Assignment button disabled when course closed
- [ ] Assignment button enabled when published
- [ ] Clicking assignment shows confirmation
- [ ] Assignment triggers and shows success

---

## ?? Related Documentation

- See `DASHBOARD_UPDATE_DOCUMENTATION.md` for detailed API changes
- See `DASHBOARD_IMPLEMENTATION_NOTES.md` for technical notes
- See `iLearn.API/Controllers/CoursesController.cs` for API implementation

---

## ? Key Improvements from Previous Version

| Aspect | Before | After |
|--------|--------|-------|
| API endpoint for save | `/info` | Direct PUT endpoint |
| Response handling | Basic | Structured with success/data wrapper |
| Error messages | Generic | Specific API error messages |
| Assignment flow | Redirect to form | Direct API call with confirmation |
| Error handling | Minimal | Comprehensive error handling |
| Code organization | Mixed concerns | Clean separation of API calls |

---

## ?? Future Enhancements

- [ ] Add resource drag-and-drop reordering
- [ ] Inline resource editor
- [ ] Version comparison view
- [ ] Batch operations
- [ ] Export course template
- [ ] Course cloning
