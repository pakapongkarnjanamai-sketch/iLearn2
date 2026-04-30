using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using iLearn.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace iLearn.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IGenericRepository<CourseContentItem> _courseContentItemRepository;
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;

        private readonly IGenericRepository<ContentItem> _contentItemRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository;
        private readonly IScormService _scormService;

        private readonly IGenericRepository<Enrollment> _enrollmentRepository;
        private readonly IGenericRepository<LearningLog> _learningLogRepository;
        private readonly IGenericRepository<Assignment> _assignmentRepository;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILearnerApiService _learnerApiService;
        private readonly IAdminActivityService _adminActivityService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly ICourseVersionService _versionService;

        public CourseService(
            ICourseRepository courseRepository,
            IGenericRepository<CourseContentItem> courseContentItemRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService,
            IGenericRepository<ContentItem> contentItemRepository,
            IGenericRepository<FileStorage> fileStorageRepository,
            IGenericRepository<Enrollment> enrollmentRepository,
            IGenericRepository<LearningLog> learningLogRepository,
            IGenericRepository<Assignment> assignmentRepository,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepository,
            IScormService scormService,
            IUnitOfWork unitOfWork,
            ILearnerApiService learnerApiService,
            IAdminActivityService adminActivityService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            ICourseVersionService versionService)
        {
            _courseRepo = courseRepository;
            _courseContentItemRepository = courseContentItemRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;

            _enrollmentRepository = enrollmentRepository;
            _learningLogRepository = learningLogRepository;
            _assignmentRepository = assignmentRepository;
            _enrollmentAssignmentRepository = enrollmentAssignmentRepository;

            _contentItemRepository = contentItemRepository;
            _fileStorageRepository = fileStorageRepository;
            _scormService = scormService;
            _unitOfWork = unitOfWork;
            _learnerApiService = learnerApiService;
            _adminActivityService = adminActivityService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _versionService = versionService;
        }

        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true)
        {
            var targetStatuses = isActive
                ? new[] { CourseStatus.Open }
                : new[] { CourseStatus.Draft, CourseStatus.Closed, CourseStatus.Retired };

            var courses = await _courseRepo.GetAsync(
                filter: c => targetStatuses.Contains(c.Status),
                includeProperties: "Category,Versions,CourseType"
            );

            return courses.Select(c => c.ToDto()).ToList();
        }

        public async Task<IEnumerable<CourseDto>> GetCoursesByDivisionNameAsync(string divisionName, bool isActive = true)
        {
            var targetStatuses = isActive
                ? new[] { CourseStatus.Open }
                : new[] { CourseStatus.Draft, CourseStatus.Closed, CourseStatus.Retired };

            // Course -> Category -> Division (ผ่าน Category.DivisionId)
            // กรองเฉพาะ Course ที่อยู่ใน Category ของ Division ที่ตรงกับชื่อ
            var courses = await _courseRepo.GetAsync(
                filter: c => targetStatuses.Contains(c.Status)
                          && c.Category != null
                          && c.Category.Division != null
                          && c.Category.Division.Name == divisionName,
                includeProperties: "Category,Category.Division,Versions,CourseType"
            );

            return courses.Select(c => c.ToDto()).ToList();
        }

        public async Task<CourseDetailDto> GetCourseByIdAsync(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                return null;

            var versions = await _courseVersionRepository.GetAllAsync();
            var targetVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive)
                ?? versions.Where(v => v.CourseId == id)
                           .OrderByDescending(v => v.VersionNumber)
                           .FirstOrDefault();

            var contentItemList = new List<CourseContentItemDto>();
            if (targetVersion != null)
            {
                var courseContentItems = await _courseContentItemRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == targetVersion.Id,
                    includeProperties: "ContentItem"
                );

                // 🌟 [แก้ไขที่นี่] เพิ่ม .OrderBy(cr => cr.Order) เพื่อเรียงลำดับ ContentItem
                contentItemList = courseContentItems.OrderBy(cr => cr.Order).Select(cr => new CourseContentItemDto
                {
                    Id = cr.ContentItem.Id,
                    Name = cr.ContentItem.Name,
                    TypeId = cr.ContentItem.TypeId,
                    TypeName = cr.ContentItem.TypeId == 2 ? "Exam" : "Learn",
                    IsActive = cr.ContentItem.IsActive,
                    URL = cr.ContentItem.URL
                }).ToList();
            }

            return new CourseDetailDto
            {
                Id = course.Id,
                CourseCode = course.Code,
                CourseName = course.Title,
                Description = course.Description,
                CourseType = course.CourseTypeId,
                CategoryId = course.CategoryId,
                IsActive = course.IsActive,
                Status = course.Status,
                CanAssign = course.CanAssign,
                CanLearnerAccess = course.CanLearnerAccess,
                ContentItems = contentItemList
            };
        }

        public async Task<CourseDto> CreateCourseAsync(CourseCreateDto model)
        {
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                throw new InvalidOperationException($"Course code '{model.CourseCode}' is already in use.");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                CategoryId = model.CategoryId,
                Description = model.Description,
                CourseTypeId = model.CourseType,
                IsActive = false,
                Status = CourseStatus.Draft
            };

            await _courseRepo.AddAsync(course);

            var courseVersion = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Draft (Pending Content)",
                IsActive = false
            };
            await _courseVersionRepository.AddAsync(courseVersion);

            if (model.ContentItemIds?.Count > 0)
            {
                await AddContentItemsToCourseVersionAsync(courseVersion.Id, model.ContentItemIds);
            }

            await _adminActivityService.LogAsync(
                actionType: "CreateCourse",
                entityType: nameof(Course),
                entityId: course.Id,
                title: $"Created course {course.Code}",
                description: $"Created course '{course.Title}' with version 1.",
                divisionId: _currentUser.DivisionId);

            return course.ToDto();
        }

        public async Task<CourseDto> CreateCourseWithScormAsync(CourseCreateDto model)
        {
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                throw new InvalidOperationException($"Course code '{model.CourseCode}' is already in use.");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                Description = model.Description,
                CategoryId = model.CategoryId,
                CourseTypeId = model.CourseType,
                IsActive = false,
                Status = CourseStatus.Draft
            };

            await _courseRepo.AddAsync(course);

            var version = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Draft (Initial Upload)",
                IsActive = false
            };
            await _courseVersionRepository.AddAsync(version);

            if (model.ContentItemIds?.Count > 0)
            {
                await AddContentItemsToCourseVersionAsync(version.Id, model.ContentItemIds);
            }

            return course.ToDto();
        }

        public async Task<CourseDto> UpdateCourseAsync(int id, CourseCreateDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {id} was not found.");

            course.Title = dto.CourseName;
            course.Description = dto.Description;
            course.Code = dto.CourseCode;
            course.CategoryId = dto.CategoryId;
            course.CourseTypeId = dto.CourseType;

            await _courseRepo.UpdateAsync(course);

            var versions = await _courseVersionRepository.GetAllAsync();
            var activeVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

            if (activeVersion != null && dto.ContentItemIds?.Count > 0)
            {
                await ReplaceVersionContentItemsAsync(activeVersion.Id, dto.ContentItemIds);
            }

            await _adminActivityService.LogAsync(
                actionType: "UpdateCourse",
                entityType: nameof(Course),
                entityId: course.Id,
                title: $"Updated course {course.Code}",
                description: $"Updated course '{course.Title}'.",
                divisionId: _currentUser.DivisionId);

            return course.ToDto();
        }

        public async Task DeleteCourseAsync(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {id} was not found.");

            // ── Guard: prevent deletion if active learners exist ──────────────
            // นับเฉพาะ Enrollment ที่: ยังไม่จบ + เคยเปิดเรียน + มี Progress จริง (> 0)
            // กรอง "zombie enrollment" (StartDate ค้างแต่ไม่มี Progress) ออก
            var inProgressCount = await _enrollmentRepository.CountAsync(
                e => e.CourseId == id && !e.IsCompleted && e.StartDate != null && e.Progress > 0
            );
            if (inProgressCount > 0)
                throw new InvalidOperationException(
                    $"Cannot delete this course because {inProgressCount} learner(s) are currently in progress."
                );

            // ── รวบรวม ContentItem + FileStorage ที่ต้องจัดการ ────────────
            var assignments = await _assignmentRepository.GetAsync(a => a.CourseId == id);
            var versions    = await _courseVersionRepository.GetAsync(v => v.CourseId == id);
            var versionIds  = versions.Select(v => v.Id).ToList();

            var courseContentItems = new List<CourseContentItem>();
            foreach (var vId in versionIds)
            {
                var crs = await _courseContentItemRepository.GetAsync(cr => cr.CourseVersionId == vId);
                courseContentItems.AddRange(crs);
            }

            // หา FileStorage + SCORM folder ที่ไม่ได้ใช้โดย course อื่น → Hard Delete ทีหลัง
            var contentItemIdsToCheck  = courseContentItems.Select(cr => cr.ContentItemId).Distinct().ToList();
            var contentItemsToSoftDel  = new List<ContentItem>();
            var fileStoragesToHardDel = new List<FileStorage>();
            var scormFoldersToDelete  = new List<string>();

            foreach (var resId in contentItemIdsToCheck)
            {
                // ตรวจว่า ContentItem นี้ถูกใช้โดย Course อื่นด้วยหรือเปล่า (ผ่าน CourseContentItem ที่ไม่ใช่ version ของ course นี้)
                var otherUsages = await _courseContentItemRepository.GetAsync(
                    cr => cr.ContentItemId == resId && !versionIds.Contains(cr.CourseVersionId)
                );

                var contentItem = await _contentItemRepository.GetByIdAsync(resId);
                if (contentItem == null) continue;

                contentItemsToSoftDel.Add(contentItem);

                // Hard Delete FileStorage เฉพาะ ContentItem ที่ไม่ได้แชร์กับ Course อื่น
                if (!otherUsages.Any() && contentItem.FileStorageId.HasValue)
                {
                    var file = await _fileStorageRepository.GetByIdAsync(contentItem.FileStorageId.Value);
                    if (file != null)
                    {
                        fileStoragesToHardDel.Add(file);
                        string ext = Path.GetExtension(file.Name)?.ToLower() ?? "";
                        if (ext == ".zip" && !string.IsNullOrEmpty(contentItem.URL))
                            scormFoldersToDelete.Add(contentItem.URL);
                    }
                }
            }

            // ── Soft Delete: Course, Version, CourseContentItem, ContentItem, Assignment ──
            // ── Hard Delete: FileStorage (bytes) + SCORM folders ────────────────────
            // Soft-delete Assignments
            foreach (var a in assignments)
                await _assignmentRepository.DeleteAsync(a);

            // Soft-delete CourseContentItems (linking table)
            foreach (var cr in courseContentItems)
                await _courseContentItemRepository.DeleteAsync(cr);

            // Soft-delete CourseVersions
            foreach (var v in versions)
                await _courseVersionRepository.DeleteAsync(v);

            // Soft-delete ContentItems (LearningLog.ContentItemId ยังอ้างอิงได้)
            foreach (var r in contentItemsToSoftDel)
                await _contentItemRepository.DeleteAsync(r);

            // Hard-delete FileStorage — ลบ binary data จริง ไม่มี FK จากที่ไหนอ้างอิงมา
            foreach (var f in fileStoragesToHardDel)
                await _fileStorageRepository.HardDeleteAsync(f);

            // Soft-delete Course หลัก (Enrollment + LearningLog ยังอยู่ครบ)
            await _courseRepo.DeleteAsync(course);

            // ── ลบ SCORM folder จาก disk หลัง transaction สำเร็จ ──────
            foreach (var folder in scormFoldersToDelete)
                _scormService.DeleteScormFolder(folder);
        }

 

        private async Task AddContentItemsToCourseVersionAsync(int versionId, List<int> contentItemIds)
        {
            if (contentItemIds?.Count > 0)
            {
                // กำหนดตัวแปรสำหรับลำดับ Order เริ่มต้นจาก 1
                int orderIndex = 1;
                foreach (var contentItemId in contentItemIds)
                {
                    var courseContentItem = new CourseContentItem
                    {
                        CourseVersionId = versionId,
                        ContentItemId = contentItemId,
                        Order = orderIndex++ // 🌟 เก็บค่า Order
                    };
                    await _courseContentItemRepository.AddAsync(courseContentItem);
                }
            }
        }

        private async Task ReplaceVersionContentItemsAsync(int versionId, List<int> newContentItemIds)
        {
            var allCourseContentItems = await _courseContentItemRepository.GetAllAsync();
            var currentContentItems = allCourseContentItems
                .Where(cr => cr.CourseVersionId == versionId)
                .ToList();

            foreach (var item in currentContentItems)
            {
                await _courseContentItemRepository.DeleteAsync(item);
            }

            await AddContentItemsToCourseVersionAsync(versionId, newContentItemIds);
        }

        public async Task<bool> UpdateCourseStatusAsync(int id, bool isActive)
        {
            var result = await UpdateCourseStatusAsync(id, isActive ? CourseStatus.Open : CourseStatus.Closed);
            return result.IsActive;
        }

        public async Task<CourseStatusResultDto> UpdateCourseStatusAsync(int id, CourseStatus status)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {id} was not found.");

            if (status == CourseStatus.Open)
            {
                var activeVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == id && v.IsActive);
                var activeVersion = activeVersions.FirstOrDefault();

                if (activeVersion == null)
                    throw new InvalidOperationException("Cannot activate the course because no active version exists.");

                var readiness = await _versionService.GetVersionReadinessAsync(activeVersion.Id);
                if (!readiness.IsReady)
                {
                    var issues = readiness.Issues.Select(issue => new ContentItemReadinessIssue(
                        issue.ContentItemId,
                        issue.ContentItemName,
                        issue.Reason)).ToList();

                    throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(readiness.ContentItemCount, issues));
                }
            }

            if (status == CourseStatus.Retired)
            {
                var openEnrollmentCount = await _enrollmentRepository.CountAsync(
                    e => e.CourseId == id && !e.IsCompleted && !e.IsDeleted);

                if (openEnrollmentCount > 0)
                {
                    throw new InvalidOperationException(
                        $"Cannot retire the course because {openEnrollmentCount} learner(s) still have open enrollments. " +
                        $"Close the course first, then wait for completion or cancel the related enrollments."
                    );
                }
            }

            var oldStatus = course.Status;
            course.Status = status;
            course.IsActive = status == CourseStatus.Open;

            await _courseRepo.UpdateAsync(course);

            await _adminActivityService.LogAsync(
                actionType: "UpdateCourseStatus",
                entityType: nameof(Course),
                entityId: course.Id,
                title: $"Changed course {course.Code} status to {status}",
                description: $"Changed course '{course.Title}' from {oldStatus} to {status}.",
                divisionId: _currentUser.DivisionId);

            return new CourseStatusResultDto
            {
                CourseId = course.Id,
                Status = course.Status,
                IsActive = course.IsActive,
                CanAssign = course.CanAssign,
                CanLearnerAccess = course.CanLearnerAccess,
                Impact = await GetCourseStatusImpactAsync(course.Id)
            };
        }

        public async Task<CourseStatusImpactDto> GetCourseStatusImpactAsync(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {id} was not found.");

            var enrollments = await _enrollmentRepository.GetAsync(e => e.CourseId == id && !e.IsDeleted);
            var assignments = await _assignmentRepository.GetAsync(
                a => !a.IsDeleted && (a.CourseId == id || a.AssignmentCourses.Any(ac => ac.CourseId == id && !ac.IsDeleted)),
                includeProperties: "AssignmentCourses");
            var now = _dateTime.Now;

            var notStartedCount = enrollments.Count(e => !e.IsCompleted && e.Progress <= 0);
            var inProgressCount = enrollments.Count(e => !e.IsCompleted && e.Progress > 0);
            var completedCount = enrollments.Count(e => e.IsCompleted);
            var activeAssignmentCount = assignments.Count(a =>
                (!a.StartDate.HasValue || a.StartDate.Value <= now) &&
                (!a.DueDate.HasValue || a.DueDate.Value >= now));
            var futureAssignmentCount = assignments.Count(a => a.StartDate.HasValue && a.StartDate.Value > now);

            return new CourseStatusImpactDto
            {
                CourseId = course.Id,
                CurrentStatus = course.Status,
                NotStartedCount = notStartedCount,
                InProgressCount = inProgressCount,
                CompletedCount = completedCount,
                ActiveAssignmentCount = activeAssignmentCount,
                FutureAssignmentCount = futureAssignmentCount,
                CanOpen = await CanOpenCourseAsync(course.Id),
                CanRetire = notStartedCount + inProgressCount == 0,
                Message = course.Status == CourseStatus.Open
                    ? "Closing this course stops new assignments. Existing assigned learners can continue learning."
                    : "Opening this course makes it available for new assignments when the active version is ready."
            };
        }

        private async Task<bool> CanOpenCourseAsync(int courseId)
        {
            var activeVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == courseId && v.IsActive);
            var activeVersion = activeVersions.FirstOrDefault();
            if (activeVersion == null)
            {
                return false;
            }

            var readiness = await _versionService.GetVersionReadinessAsync(activeVersion.Id);
            return readiness.IsReady;
        }

        // ── Dashboard / Aggregation Operations ─────────────────────────────

        public async Task<List<CourseLearnerDto>> GetCourseLearnersAsync(int courseId)
        {
            var enrollments = await _enrollmentRepository.GetAsync(
                e => e.CourseId == courseId,
                includeProperties: "AssignmentLinks"
            );

            if (!enrollments.Any())
                return [];

            var codes = enrollments.Select(e => e.LearnerCode).Distinct().ToList();
            Dictionary<string, ExternalLearnerDto> learnerMap;
            try
            {
                learnerMap = await _learnerApiService.GetLearnersByCodesAsync(codes);
            }
            catch
            {
                learnerMap = new Dictionary<string, ExternalLearnerDto>();
            }

            var now = _dateTime.Now;

            return enrollments.Select(e =>
            {
                var learner = learnerMap.GetValueOrDefault(e.LearnerCode);
                var effectiveStart = e.AssignmentLinks.Any() ? e.AssignmentLinks.Min(a => a.StartDate) : e.StartDate;
                var effectiveDue   = e.AssignmentLinks.Any() ? e.AssignmentLinks.Max(a => a.DueDate)   : e.DueDate;

                var status = AssignmentStatusKeys.GetScheduledLearnerStatus(
                    e.IsCompleted,
                    e.Progress,
                    effectiveStart,
                    effectiveDue,
                    now);

                return new CourseLearnerDto
                {
                    Id            = e.Id,
                    LearnerCode   = e.LearnerCode,
                    LearnerName   = learner?.Name ?? e.LearnerCode,
                    Division      = learner?.Division,
                    Department    = learner?.Department,
                    Position      = learner?.Position,
                    Progress      = Math.Round(e.Progress),
                    IsCompleted   = e.IsCompleted,
                    CompletedDate = e.CompletedDate,
                    StartDate     = effectiveStart,
                    DueDate       = effectiveDue,
                    Status        = status
                };
            })
            .OrderBy(x => x.IsCompleted)
            .ThenByDescending(x => x.Progress)
            .ToList();
        }

        public async Task<List<CourseAssignmentHistoryDto>> GetCourseAssignmentsAsync(int courseId)
        {
            var assignments = await _assignmentRepository.GetAsync(
                r => r.CourseId == courseId
                  && (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value),
                includeProperties: "Course"
            );

            if (!assignments.Any())
                return [];

            var allIds = assignments.Select(a => a.Id).ToList();
            var links = await _enrollmentAssignmentRepository.GetAsync(
                ea => allIds.Contains(ea.AssignmentId),
                includeProperties: "Enrollment"
            );

            var now = _dateTime.Now;

            return assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Select(g =>
                {
                    var first   = g.First();
                    var ruleIds = g.Select(a => a.Id).ToList();

                    var relatedLinks = links
                        .Where(ea => ruleIds.Contains(ea.AssignmentId) && ea.Enrollment != null)
                        .ToList();

                    bool allDone = relatedLinks.Any()
                        && relatedLinks.All(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);

                    string status = AssignmentDashboardService.CalculateStatus(
                        relatedLinks.Any(), allDone, first.StartDate, first.DueDate, now);

                    var done  = relatedLinks.Count(ea => ea.SnapshotCompleted || ea.Enrollment!.IsCompleted);
                    var total = relatedLinks.Count;
                    var pct   = total > 0 ? Math.Round((double)done / total * 100) : 0;

                    return new CourseAssignmentHistoryDto
                    {
                        Id                      = first.Id,
                        AssignmentNo            = g.Key,
                        Description             = first.Description,
                        StartDate               = first.StartDate,
                        DueDate                 = first.DueDate,
                        Status                  = status,
                        CompletedEnrollmentCount = done,
                        TotalEnrollmentCount     = total,
                        CompletionPct            = pct,
                        LearnerGroupId           = first.LearnerGroupId
                    };
                })
                .OrderByDescending(x => x.AssignmentNo)
                .ToList();
        }

        public async Task<CourseDashboardDto> GetCourseDashboardAsync(int courseId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course == null)
                return null;

            var versions = await _versionService.GetCourseVersionsAsync(courseId);
            var enrollments = await _enrollmentRepository.GetAsync(
                e => e.CourseId == courseId
            );
            var assignments = await _assignmentRepository.GetAsync(
                r => r.CourseId == courseId
                  && (!_currentUser.DivisionId.HasValue || r.DivisionId == _currentUser.DivisionId.Value)
            );

            var assignmentGroups = assignments
                .Where(r => !string.IsNullOrEmpty(r.AssignmentNo))
                .GroupBy(r => r.AssignmentNo)
                .Count();

            return new CourseDashboardDto
            {
                Course = course,
                Versions = versions,
                Kpi = new CourseDashboardKpiDto
                {
                    VersionCount    = versions.Count(),
                    LearnerCount    = enrollments.Count,
                    CompletedCount  = enrollments.Count(e => e.IsCompleted),
                    AssignmentCount = assignmentGroups
                }
            };
        }
    }
}