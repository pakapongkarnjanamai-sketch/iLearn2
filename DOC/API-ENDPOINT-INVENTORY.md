# API Endpoint Inventory

Generated from source code attributes in iLearn.API/Controllers.

- GeneratedAt: 2026-06-15 12:13:19
- TotalControllersWithEndpoints: 30
- TotalEndpoints: 165
- TotalHubs: 1

## Summary

### By HTTP Verb

| Verb | Count |
|---|---:|
| GET | 91 |
| POST | 38 |
| DELETE | 19 |
| PUT | 15 |
| PATCH | 2 |

### By Route Family (Class Route Template)

| ClassRouteTemplate | Count |
|---|---:|
| api/[controller] | 102 |
| api/admin/[controller] | 62 |
| api/admin/session | 1 |

### By Authorization Policy (Class-Level)

| Policy | Count |
|---|---:|
| FallbackPolicy(DefaultPolicy) | 69 |
| AdminOnly | 53 |
| SuperAdminOnly | 42 |
| DomainUser | 1 |

## Endpoint Inventory

| # | Verb | Route | Controller | Action | Policy | Source |
|---:|---|---|---|---|---|---|
| 1 | GET | api/admin/AssignmentsCRUD/Get | AssignmentsCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\AssignmentsCRUDController.cs:37 |
| 2 | POST | api/admin/Cache/clear-all | CacheController | ClearAll | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CacheController.cs:21 |
| 3 | DELETE | api/admin/CategoriesCRUD/Delete | CategoriesCRUDController | Delete | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:335 |
| 4 | GET | api/admin/CategoriesCRUD/Get | CategoriesCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:48 |
| 5 | GET | api/admin/CategoriesCRUD/Get/{id} | CategoriesCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:35 |
| 6 | GET | api/admin/CategoriesCRUD/GetDashboard/{id} | CategoriesCRUDController | GetDashboard | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:176 |
| 7 | GET | api/admin/CategoriesCRUD/GetPaged | CategoriesCRUDController | GetPaged | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:101 |
| 8 | GET | api/admin/CategoriesCRUD/GetSummaryStats | CategoriesCRUDController | GetSummaryStats | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:150 |
| 9 | POST | api/admin/CategoriesCRUD/Post | CategoriesCRUDController | Post | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:280 |
| 10 | PUT | api/admin/CategoriesCRUD/Put | CategoriesCRUDController | Put | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CategoriesCRUDController.cs:303 |
| 11 | DELETE | api/admin/ContentItemsCRUD/Delete | ContentItemsCRUDController | Delete | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:326 |
| 12 | GET | api/admin/ContentItemsCRUD/Get | ContentItemsCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:55 |
| 13 | GET | api/admin/ContentItemsCRUD/Get/{id} | ContentItemsCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:67 |
| 14 | GET | api/admin/ContentItemsCRUD/GetByCourse | ContentItemsCRUDController | GetByCourse | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:61 |
| 15 | GET | api/admin/ContentItemsCRUD/GetDashboardStats | ContentItemsCRUDController | GetDashboardStats | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:187 |
| 16 | GET | api/admin/ContentItemsCRUD/GetServerStats | ContentItemsCRUDController | GetServerStats | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:175 |
| 17 | GET | api/admin/ContentItemsCRUD/GetSummaryStats | ContentItemsCRUDController | GetSummaryStats | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:181 |
| 18 | PUT | api/admin/ContentItemsCRUD/Put | ContentItemsCRUDController | Put | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\ContentItemsCRUDController.cs:271 |
| 19 | GET | api/admin/CourseContentItemsCRUD/Get | CourseContentItemsCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseContentItemsCRUDController.cs:23 |
| 20 | GET | api/admin/CoursesCRUD/Get | CoursesCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CoursesCRUDController.cs:30 |
| 21 | GET | api/admin/CoursesCRUD/Get/{id} | CoursesCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CoursesCRUDController.cs:64 |
| 22 | GET | api/admin/CoursesCRUD/GetActive | CoursesCRUDController | GetActive | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CoursesCRUDController.cs:88 |
| 23 | GET | api/admin/CoursesCRUD/GetForLookup | CoursesCRUDController | GetForLookup | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CoursesCRUDController.cs:76 |
| 24 | DELETE | api/admin/CourseTypesCRUD/Delete | CourseTypesCRUDController | Delete | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseTypesCRUDController.cs:115 |
| 25 | GET | api/admin/CourseTypesCRUD/Get | CourseTypesCRUDController | Get | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseTypesCRUDController.cs:36 |
| 26 | GET | api/admin/CourseTypesCRUD/GetSummaryStats | CourseTypesCRUDController | GetSummaryStats | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseTypesCRUDController.cs:54 |
| 27 | POST | api/admin/CourseTypesCRUD/Post | CourseTypesCRUDController | Post | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseTypesCRUDController.cs:91 |
| 28 | PUT | api/admin/CourseTypesCRUD/Put | CourseTypesCRUDController | Put | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseTypesCRUDController.cs:103 |
| 29 | GET | api/admin/CourseVersionsCRUD/Get/{id} | CourseVersionsCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\CourseVersionsCRUDController.cs:24 |
| 30 | GET | api/admin/Dashboard/EnrollmentTrends | DashboardController | GetEnrollmentTrends | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DashboardController.cs:255 |
| 31 | GET | api/admin/Dashboard/LearningActivityTrends | DashboardController | GetLearningActivityTrends | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DashboardController.cs:281 |
| 32 | GET | api/admin/Dashboard/MaintenanceStatus | DashboardController | GetMaintenanceStatus | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DashboardController.cs:309 |
| 33 | GET | api/admin/Dashboard/Overview | DashboardController | GetOverview | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DashboardController.cs:67 |
| 34 | GET | api/admin/Dashboard/RecentAdminActivities | DashboardController | GetRecentAdminActivities | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DashboardController.cs:334 |
| 35 | GET | api/admin/Dashboard/Stats | DashboardController | GetStats | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DashboardController.cs:234 |
| 36 | DELETE | api/admin/DivisionsCRUD/Delete | DivisionsCRUDController | Delete | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\DivisionsCRUDController.cs:158 |
| 37 | GET | api/admin/DivisionsCRUD/Get | DivisionsCRUDController | Get | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\DivisionsCRUDController.cs:42 |
| 38 | GET | api/admin/DivisionsCRUD/GetSummaryStats | DivisionsCRUDController | GetSummaryStats | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\DivisionsCRUDController.cs:101 |
| 39 | POST | api/admin/DivisionsCRUD/Post | DivisionsCRUDController | Post | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\DivisionsCRUDController.cs:134 |
| 40 | PUT | api/admin/DivisionsCRUD/Put | DivisionsCRUDController | Put | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\DivisionsCRUDController.cs:146 |
| 41 | DELETE | api/admin/EnrollmentsCRUD/Delete | EnrollmentsCRUDController | Delete | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\EnrollmentsCRUDController.cs:120 |
| 42 | GET | api/admin/EnrollmentsCRUD/Get | EnrollmentsCRUDController | Get | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\EnrollmentsCRUDController.cs:37 |
| 43 | GET | api/admin/EnrollmentsCRUD/GetSummaryStats | EnrollmentsCRUDController | GetSummaryStats | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\EnrollmentsCRUDController.cs:64 |
| 44 | POST | api/admin/EnrollmentsCRUD/Post | EnrollmentsCRUDController | Post | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\EnrollmentsCRUDController.cs:96 |
| 45 | PUT | api/admin/EnrollmentsCRUD/Put | EnrollmentsCRUDController | Put | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\EnrollmentsCRUDController.cs:108 |
| 46 | DELETE | api/admin/Generic/Delete | GenericController | Delete | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\GenericController.cs:70 |
| 47 | GET | api/admin/Generic/Get | GenericController | Get | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\GenericController.cs:27 |
| 48 | GET | api/admin/Generic/Get/{id} | GenericController | Get | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\GenericController.cs:34 |
| 49 | POST | api/admin/Generic/Post | GenericController | Post | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\GenericController.cs:42 |
| 50 | PUT | api/admin/Generic/Put | GenericController | Put | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\GenericController.cs:55 |
| 51 | DELETE | api/admin/LearnerGroupsCRUD/Delete | LearnerGroupsCRUDController | Delete | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearnerGroupsCRUDController.cs:122 |
| 52 | GET | api/admin/LearnerGroupsCRUD/Get | LearnerGroupsCRUDController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearnerGroupsCRUDController.cs:29 |
| 53 | POST | api/admin/LearnerGroupsCRUD/Post | LearnerGroupsCRUDController | Post | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearnerGroupsCRUDController.cs:53 |
| 54 | PUT | api/admin/LearnerGroupsCRUD/Put | LearnerGroupsCRUDController | Put | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearnerGroupsCRUDController.cs:85 |
| 55 | DELETE | api/admin/LearningLogsCRUD/Delete | LearningLogsCRUDController | Delete | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearningLogsCRUDController.cs:128 |
| 56 | GET | api/admin/LearningLogsCRUD/Get | LearningLogsCRUDController | Get | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearningLogsCRUDController.cs:41 |
| 57 | GET | api/admin/LearningLogsCRUD/GetSummaryStats | LearningLogsCRUDController | GetSummaryStats | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearningLogsCRUDController.cs:70 |
| 58 | POST | api/admin/LearningLogsCRUD/Post | LearningLogsCRUDController | Post | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearningLogsCRUDController.cs:104 |
| 59 | PUT | api/admin/LearningLogsCRUD/Put | LearningLogsCRUDController | Put | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\LearningLogsCRUDController.cs:116 |
| 60 | GET | api/admin/session/me | SessionController | Me | DomainUser | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\SessionController.cs:20 |
| 61 | GET | api/admin/SystemConfig | SystemConfigController | Get | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\SystemConfigController.cs:34 |
| 62 | GET | api/admin/UsersCRUD/Get | UsersCRUDController | Get | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\UsersCRUDController.cs:33 |
| 63 | PUT | api/admin/UsersCRUD/Put | UsersCRUDController | Put | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\Base\UsersCRUDController.cs:117 |
| 64 | DELETE | api/Assignments/{id} | AssignmentsController | Delete | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:107 |
| 65 | POST | api/Assignments/{id}/courses | AssignmentsController | AddCourses | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:387 |
| 66 | DELETE | api/Assignments/{id}/courses/{ruleId} | AssignmentsController | RemoveCourse | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:476 |
| 67 | PATCH | api/Assignments/{id}/extend-due-date | AssignmentsController | ExtendDueDate | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:344 |
| 68 | POST | api/Assignments/{id}/learners | AssignmentsController | AddLearners | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:526 |
| 69 | DELETE | api/Assignments/{id}/learners/{learnerCode} | AssignmentsController | RemoveLearner | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:593 |
| 70 | POST | api/Assignments/{id}/reset-enrollments | AssignmentsController | ResetEnrollments | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:215 |
| 71 | GET | api/Assignments/course/{courseId} | AssignmentsController | GetByCourse | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:96 |
| 72 | GET | api/Assignments/dashboard/{id} | AssignmentsController | GetDashboardData | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:152 |
| 73 | GET | api/Assignments/gantt | AssignmentsController | GetGanttTasks | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:88 |
| 74 | GET | api/Assignments/group/{groupId}/history | AssignmentsController | GetGroupHistory | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:675 |
| 75 | GET | api/Assignments/history | AssignmentsController | GetHistory | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:60 |
| 76 | GET | api/Assignments/lookup-courses | AssignmentsController | GetLookupCourses | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:655 |
| 77 | GET | api/Assignments/reassign-data/{id} | AssignmentsController | GetReassignData | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:176 |
| 78 | GET | api/Assignments/resolve/{assignmentNo} | AssignmentsController | ResolveByNo | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:161 |
| 79 | POST | api/Assignments/validate-before-assign | AssignmentsController | ValidateBeforeAssign | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\AssignmentsController.cs:321 |
| 80 | GET | api/Categories/{id:int} | CategoriesController | GetById | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CategoriesController.cs:51 |
| 81 | GET | api/Categories/lookup | CategoriesController | GetLookup | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CategoriesController.cs:29 |
| 82 | GET | api/ContentItems | ContentItemsController | GetAll | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:56 |
| 83 | DELETE | api/ContentItems/{id} | ContentItemsController | Delete | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:330 |
| 84 | GET | api/ContentItems/{id} | ContentItemsController | GetById | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:177 |
| 85 | GET | api/ContentItems/{id}/content | ContentItemsController | GetContent | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:185 |
| 86 | POST | api/ContentItems/Admin/BatchPublish | ContentItemsController | BatchPublish | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:539 |
| 87 | POST | api/ContentItems/Admin/BatchPublishStream | ContentItemsController | BatchPublishStream | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:587 |
| 88 | POST | api/ContentItems/Admin/BatchUnpublish | ContentItemsController | BatchUnpublish | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:463 |
| 89 | DELETE | api/ContentItems/Admin/BulkDeletePublished | ContentItemsController | BulkDeletePublished | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:1032 |
| 90 | POST | api/ContentItems/Admin/BulkSetPublic | ContentItemsController | BulkSetPublicStreaming | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:817 |
| 91 | GET | api/ContentItems/Admin/OptimizeAnalysis | ContentItemsController | OptimizeAnalysis | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:362 |
| 92 | POST | api/ContentItems/Admin/PreviewBatchUnpublish | ContentItemsController | PreviewBatchUnpublish | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:525 |
| 93 | GET | api/ContentItems/paged | ContentItemsController | GetPaged | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:63 |
| 94 | POST | api/ContentItems/SetPublic | ContentItemsController | SetPublic | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:267 |
| 95 | POST | api/ContentItems/Unpublish | ContentItemsController | Unpublish | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:305 |
| 96 | POST | api/ContentItems/upload | ContentItemsController | Upload | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentItemsController.cs:213 |
| 97 | GET | api/ContentLibrary/lookup | ContentLibraryController | GetLookup | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\ContentLibraryController.cs:23 |
| 98 | GET | api/Courses | CoursesController | GetAll | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:105 |
| 99 | GET | api/Courses/{courseId}/assignments | CoursesController | GetCourseAssignments | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:452 |
| 100 | GET | api/Courses/{courseId}/dashboard | CoursesController | GetDashboard | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:579 |
| 101 | GET | api/Courses/{courseId}/learners | CoursesController | GetCourseLearners | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:391 |
| 102 | GET | api/Courses/{courseId}/version-impact | CoursesController | GetVersionLearnerImpact | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:239 |
| 103 | GET | api/Courses/{courseId}/versions | CoursesController | GetCourseVersions | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:225 |
| 104 | POST | api/Courses/{courseId}/versions | CoursesController | CreateVersion | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:281 |
| 105 | PATCH | api/Courses/{courseId}/versions/{versionId}/set-active | CoursesController | SetActiveVersion | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:370 |
| 106 | DELETE | api/Courses/{id} | CoursesController | Delete | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:174 |
| 107 | GET | api/Courses/{id} | CoursesController | GetById | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:129 |
| 108 | PUT | api/Courses/{id} | CoursesController | Update | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:157 |
| 109 | PUT | api/Courses/{id}/status | CoursesController | UpdateStatus | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:514 |
| 110 | GET | api/Courses/{id}/status-impact | CoursesController | GetStatusImpact | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:563 |
| 111 | GET | api/Courses/course-types-lookup | CoursesController | GetCourseTypesLookup | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:88 |
| 112 | POST | api/Courses/Create | CoursesController | Create | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:139 |
| 113 | POST | api/Courses/create-scorm | CoursesController | CreateCourseWithScorm | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:196 |
| 114 | GET | api/Courses/lookup | CoursesController | GetLookup | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:59 |
| 115 | DELETE | api/Courses/versions/{versionId} | CoursesController | DeleteVersion | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:350 |
| 116 | GET | api/Courses/versions/{versionId} | CoursesController | GetVersion | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:253 |
| 117 | PUT | api/Courses/versions/{versionId} | CoursesController | UpdateVersion | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:317 |
| 118 | GET | api/Courses/versions/{versionId}/readiness | CoursesController | GetVersionReadiness | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\CoursesController.cs:267 |
| 119 | GET | api/Divisions | DivisionsController | GetAll | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DivisionsController.cs:59 |
| 120 | POST | api/Divisions | DivisionsController | Create | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DivisionsController.cs:74 |
| 121 | GET | api/Divisions/GetTree | DivisionsController | GetTree | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DivisionsController.cs:105 |
| 122 | GET | api/Divisions/lookup | DivisionsController | GetLookup | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DivisionsController.cs:36 |
| 123 | GET | api/Divisions/resolve-id | DivisionsController | ResolveId | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\DivisionsController.cs:86 |
| 124 | GET | api/Enrollments/{id} | EnrollmentsController | GetById | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\EnrollmentsController.cs:387 |
| 125 | PUT | api/Enrollments/{id}/completion | EnrollmentsController | UpdateCompletion | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\EnrollmentsController.cs:396 |
| 126 | POST | api/Enrollments/BulkAssign | EnrollmentsController | BulkAssign | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\EnrollmentsController.cs:407 |
| 127 | GET | api/Enrollments/my-courses | EnrollmentsController | GetMyCourses | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\EnrollmentsController.cs:71 |
| 128 | GET | api/Enrollments/player-info/{courseId} | EnrollmentsController | GetPlayerInfoByCourse | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\EnrollmentsController.cs:143 |
| 129 | POST | api/Enrollments/ResetStatus | EnrollmentsController | ResetStatus | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\EnrollmentsController.cs:59 |
| 130 | GET | api/LearnerGroupCategories | LearnerGroupCategoriesController | GetAll | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupCategoriesController.cs:21 |
| 131 | POST | api/LearnerGroupCategories | LearnerGroupCategoriesController | Create | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupCategoriesController.cs:36 |
| 132 | DELETE | api/LearnerGroupCategories/{id} | LearnerGroupCategoriesController | Delete | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupCategoriesController.cs:73 |
| 133 | GET | api/LearnerGroupCategories/{id} | LearnerGroupCategoriesController | GetById | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupCategoriesController.cs:28 |
| 134 | PUT | api/LearnerGroupCategories/{id} | LearnerGroupCategoriesController | Update | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupCategoriesController.cs:51 |
| 135 | GET | api/LearnerGroups | LearnerGroupsController | GetAll | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:23 |
| 136 | POST | api/LearnerGroups | LearnerGroupsController | Create | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:49 |
| 137 | DELETE | api/LearnerGroups/{id} | LearnerGroupsController | Delete | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:96 |
| 138 | GET | api/LearnerGroups/{id} | LearnerGroupsController | GetById | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:40 |
| 139 | PUT | api/LearnerGroups/{id} | LearnerGroupsController | Update | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:71 |
| 140 | POST | api/LearnerGroups/{id}/members | LearnerGroupsController | AddMembers | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:111 |
| 141 | DELETE | api/LearnerGroups/{id}/members/{memberId} | LearnerGroupsController | RemoveMember | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:173 |
| 142 | POST | api/LearnerGroups/{id}/members/confirm | LearnerGroupsController | ConfirmAddMembers | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:150 |
| 143 | POST | api/LearnerGroups/{id}/members/preview | LearnerGroupsController | PreviewAddMembers | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:128 |
| 144 | POST | api/LearnerGroups/{id}/members/remove/confirm | LearnerGroupsController | ConfirmRemoveMembers | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:209 |
| 145 | POST | api/LearnerGroups/{id}/members/remove/preview | LearnerGroupsController | PreviewRemoveMembers | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:187 |
| 146 | GET | api/LearnerGroups/paged | LearnerGroupsController | GetPaged | AdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnerGroupsController.cs:32 |
| 147 | GET | api/Learners/divisions | LearnersController | GetLearnersByDivisions | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:57 |
| 148 | GET | api/Learners/Get | LearnersController | Get | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:149 |
| 149 | GET | api/Learners/GetDepartments | LearnersController | GetDepartments | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:104 |
| 150 | GET | api/Learners/GetDivisions | LearnersController | GetDivisions | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:83 |
| 151 | GET | api/Learners/GetLearnerbyEID/{employeeCode} | LearnersController | GetLearnerbyEID | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:34 |
| 152 | GET | api/Learners/GetPositions | LearnersController | GetPositions | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:134 |
| 153 | GET | api/Learners/GetSections | LearnersController | GetSections | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:119 |
| 154 | GET | api/Learners/profile/{code} | LearnersController | GetProfile | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearnersController.cs:182 |
| 155 | POST | api/LearningLogs/commit-runtime | LearningLogsController | CommitRuntime | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearningLogsController.cs:84 |
| 156 | POST | api/LearningLogs/reset-progress | LearningLogsController | ResetProgress | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearningLogsController.cs:151 |
| 157 | POST | api/LearningLogs/update-progress | LearningLogsController | UpdateProgress | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\LearningLogsController.cs:52 |
| 158 | GET | api/Roles | RolesController | GetAll | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\RolesController.cs:53 |
| 159 | POST | api/Roles | RolesController | Create | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\RolesController.cs:76 |
| 160 | GET | api/Roles/by-division/{divisionId} | RolesController | GetByDivision | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\RolesController.cs:69 |
| 161 | GET | api/Roles/lookup | RolesController | GetLookup | SuperAdminOnly | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\RolesController.cs:28 |
| 162 | GET | api/Users | UsersController | GetAll | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\UsersController.cs:35 |
| 163 | DELETE | api/Users/{id} | UsersController | Delete | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\UsersController.cs:85 |
| 164 | GET | api/Users/{id} | UsersController | GetById | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\UsersController.cs:59 |
| 165 | POST | api/Users/windows-auth | UsersController | GetOrCreateUserFromWindows | FallbackPolicy(DefaultPolicy) | C:\Users\n4734\source\repos\iLearn2\iLearn.API\Controllers\UsersController.cs:96 |

## SignalR Hubs

| # | Hub | Route | Source |
|---:|---|---|---|
| 1 | AdminActivityHub | /hubs/admin-activity | iLearn.API/Program.cs:63 |

