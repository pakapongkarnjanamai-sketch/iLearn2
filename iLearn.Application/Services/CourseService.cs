using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
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
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;

        private readonly IGenericRepository<Resource> _resourceRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository;
        private readonly IScormService _scormService;

        private readonly IGenericRepository<Enrollment> _enrollmentRepository;
        private readonly IGenericRepository<LearningLog> _learningLogRepository;
        private readonly IGenericRepository<Assignment> _assignmentRepository;
        private readonly IGenericRepository<EnrollmentAssignment> _enrollmentAssignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStudentApiService _studentApiService;
        private readonly IAdminActivityService _adminActivityService;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTime _dateTime;
        private readonly ICourseVersionService _versionService;

        public CourseService(
            ICourseRepository courseRepository,
            IGenericRepository<CourseResource> courseResourceRepository,
            IGenericRepository<CourseVersion> courseVersionRepository,
            ICourseAssignmentService assignmentService,
            IGenericRepository<Resource> resourceRepository,
            IGenericRepository<FileStorage> fileStorageRepository,
            IGenericRepository<Enrollment> enrollmentRepository,
            IGenericRepository<LearningLog> learningLogRepository,
            IGenericRepository<Assignment> assignmentRepository,
            IGenericRepository<EnrollmentAssignment> enrollmentAssignmentRepository,
            IScormService scormService,
            IUnitOfWork unitOfWork,
            IStudentApiService studentApiService,
            IAdminActivityService adminActivityService,
            ICurrentUserService currentUser,
            IDateTime dateTime,
            ICourseVersionService versionService)
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;

            _enrollmentRepository = enrollmentRepository;
            _learningLogRepository = learningLogRepository;
            _assignmentRepository = assignmentRepository;
            _enrollmentAssignmentRepository = enrollmentAssignmentRepository;

            _resourceRepository = resourceRepository;
            _fileStorageRepository = fileStorageRepository;
            _scormService = scormService;
            _unitOfWork = unitOfWork;
            _studentApiService = studentApiService;
            _adminActivityService = adminActivityService;
            _currentUser = currentUser;
            _dateTime = dateTime;
            _versionService = versionService;
        }

        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true)
        {
            var courses = await _courseRepo.GetAsync(
                filter: c => c.IsActive == isActive,
                includeProperties: "Category,Versions,CourseType"
            );

            return courses.Select(c => c.ToDto()).ToList();
        }

        public async Task<IEnumerable<CourseDto>> GetCoursesByDivisionNameAsync(string divisionName, bool isActive = true)
        {
            // Course -> Category -> Division (ผ่าน Category.DivisionId)
            // กรองเฉพาะ Course ที่อยู่ใน Category ของ Division ที่ตรงกับชื่อ
            var courses = await _courseRepo.GetAsync(
                filter: c => c.IsActive == isActive
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

            var resourceList = new List<CourseResourceDto>();
            if (targetVersion != null)
            {
                var courseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == targetVersion.Id,
                    includeProperties: "Resource"
                );

                // 🌟 [แก้ไขที่นี่] เพิ่ม .OrderBy(cr => cr.Order) เพื่อเรียงลำดับ Resource
                resourceList = courseResources.OrderBy(cr => cr.Order).Select(cr => new CourseResourceDto
                {
                    Id = cr.Resource.Id,
                    Name = cr.Resource.Name,
                    TypeId = cr.Resource.TypeId,
                    TypeName = cr.Resource.TypeId == 2 ? "Exam" : "Learn",
                    IsActive = cr.Resource.IsActive,
                    URL = cr.Resource.URL
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
                Resources = resourceList
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
                IsActive = false
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

            if (model.ResourceIds?.Count > 0)
            {
                await AddResourcesToCourseVersionAsync(courseVersion.Id, model.ResourceIds);
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
                IsActive = false // Draft status
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

            if (model.ResourceIds?.Count > 0)
            {
                await AddResourcesToCourseVersionAsync(version.Id, model.ResourceIds);
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

            if (activeVersion != null && dto.ResourceIds?.Count > 0)
            {
                await ReplaceVersionResourcesAsync(activeVersion.Id, dto.ResourceIds);
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

            // ── รวบรวม Resource + FileStorage ที่ต้องจัดการ ────────────
            var assignments = await _assignmentRepository.GetAsync(a => a.CourseId == id);
            var versions    = await _courseVersionRepository.GetAsync(v => v.CourseId == id);
            var versionIds  = versions.Select(v => v.Id).ToList();

            var courseResources = new List<CourseResource>();
            foreach (var vId in versionIds)
            {
                var crs = await _courseResourceRepository.GetAsync(cr => cr.CourseVersionId == vId);
                courseResources.AddRange(crs);
            }

            // หา FileStorage + SCORM folder ที่ไม่ได้ใช้โดย course อื่น → Hard Delete ทีหลัง
            var resourceIdsToCheck  = courseResources.Select(cr => cr.ResourceId).Distinct().ToList();
            var resourcesToSoftDel  = new List<Resource>();
            var fileStoragesToHardDel = new List<FileStorage>();
            var scormFoldersToDelete  = new List<string>();

            foreach (var resId in resourceIdsToCheck)
            {
                // ตรวจว่า Resource นี้ถูกใช้โดย Course อื่นด้วยหรือเปล่า (ผ่าน CourseResource ที่ไม่ใช่ version ของ course นี้)
                var otherUsages = await _courseResourceRepository.GetAsync(
                    cr => cr.ResourceId == resId && !versionIds.Contains(cr.CourseVersionId)
                );

                var resource = await _resourceRepository.GetByIdAsync(resId);
                if (resource == null) continue;

                resourcesToSoftDel.Add(resource);

                // Hard Delete FileStorage เฉพาะ Resource ที่ไม่ได้แชร์กับ Course อื่น
                if (!otherUsages.Any() && resource.FileStorageId.HasValue)
                {
                    var file = await _fileStorageRepository.GetByIdAsync(resource.FileStorageId.Value);
                    if (file != null)
                    {
                        fileStoragesToHardDel.Add(file);
                        string ext = Path.GetExtension(file.Name)?.ToLower() ?? "";
                        if (ext == ".zip" && !string.IsNullOrEmpty(resource.URL))
                            scormFoldersToDelete.Add(resource.URL);
                    }
                }
            }

            // ── Soft Delete: Course, Version, CourseResource, Resource, Assignment ──
            // ── Hard Delete: FileStorage (bytes) + SCORM folders ────────────────────
            // Soft-delete Assignments
            foreach (var a in assignments)
                await _assignmentRepository.DeleteAsync(a);

            // Soft-delete CourseResources (linking table)
            foreach (var cr in courseResources)
                await _courseResourceRepository.DeleteAsync(cr);

            // Soft-delete CourseVersions
            foreach (var v in versions)
                await _courseVersionRepository.DeleteAsync(v);

            // Soft-delete Resources (LearningLog.ResourceId ยังอ้างอิงได้)
            foreach (var r in resourcesToSoftDel)
                await _resourceRepository.DeleteAsync(r);

            // Hard-delete FileStorage — ลบ binary data จริง ไม่มี FK จากที่ไหนอ้างอิงมา
            foreach (var f in fileStoragesToHardDel)
                await _fileStorageRepository.HardDeleteAsync(f);

            // Soft-delete Course หลัก (Enrollment + LearningLog ยังอยู่ครบ)
            await _courseRepo.DeleteAsync(course);

            // ── ลบ SCORM folder จาก disk หลัง transaction สำเร็จ ──────
            foreach (var folder in scormFoldersToDelete)
                _scormService.DeleteScormFolder(folder);
        }

 

        private async Task AddResourcesToCourseVersionAsync(int versionId, List<int> resourceIds)
        {
            if (resourceIds?.Count > 0)
            {
                // กำหนดตัวแปรสำหรับลำดับ Order เริ่มต้นจาก 1
                int orderIndex = 1;
                foreach (var resourceId in resourceIds)
                {
                    var courseResource = new CourseResource
                    {
                        CourseVersionId = versionId,
                        ResourceId = resourceId,
                        Order = orderIndex++ // 🌟 เก็บค่า Order
                    };
                    await _courseResourceRepository.AddAsync(courseResource);
                }
            }
        }

        private async Task ReplaceVersionResourcesAsync(int versionId, List<int> newResourceIds)
        {
            var allCourseResources = await _courseResourceRepository.GetAllAsync();
            var currentResources = allCourseResources
                .Where(cr => cr.CourseVersionId == versionId)
                .ToList();

            foreach (var item in currentResources)
            {
                await _courseResourceRepository.DeleteAsync(item);
            }

            await AddResourcesToCourseVersionAsync(versionId, newResourceIds);
        }

        public async Task<bool> UpdateCourseStatusAsync(int id, bool isActive)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course with ID {id} was not found.");

            if (isActive)
            {
                // 1. ตรวจสอบว่ามี CourseVersion ที่เปิดใช้งานอยู่หรือไม่
                var activeVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == id && v.IsActive);
                var activeVersion = activeVersions.FirstOrDefault();

                if (activeVersion == null)
                    throw new InvalidOperationException("Cannot activate the course because no active version exists.");

                var readiness = await _versionService.GetVersionReadinessAsync(activeVersion.Id);
                if (!readiness.IsReady)
                {
                    var issues = readiness.Issues.Select(issue => new ResourceReadinessIssue(
                        issue.ResourceId,
                        issue.ResourceName,
                        issue.Reason)).ToList();

                    throw new InvalidOperationException(CourseContentReadiness.BuildActivationErrorMessage(readiness.ResourceCount, issues));
                }
            }
            else
            {
                // 🔒 นับเฉพาะ Enrollment ที่ผู้เรียนเรียนจริงแล้ว โดยต้องผ่านทุกเงื่อนไขต่อไปนี้:
                //   1. ยังไม่เสร็จ (IsCompleted = false)
                //   2. เริ่มเรียนไปแล้ว (StartDate != null)
                //   3. มี Progress จริง (Progress > 0) — กรอง "zombie enrollment" ออก
                //      (Enrollment ที่ Assignment ถูกยกเลิกแต่ StartDate ถูก set ไว้แล้ว)
                var activeEnrollments = await _enrollmentRepository.GetAsync(
                    e => e.CourseId == id && !e.IsCompleted && e.StartDate != null && e.Progress > 0
                );

                if (activeEnrollments.Any())
                {
                    var count = activeEnrollments.Count();
                    throw new InvalidOperationException(
                        $"Cannot deactivate the course because {count} learner(s) are currently in progress. " +
                        $"Please wait until all learners complete or cancel the related enrollments first."
                    );
                }
            }

            course.IsActive = isActive;

            await _courseRepo.UpdateAsync(course);

            return course.IsActive;
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

            var codes = enrollments.Select(e => e.StudentCode).Distinct().ToList();
            Dictionary<string, ExternalStudentDto> studentMap;
            try
            {
                studentMap = await _studentApiService.GetStudentsByCodesAsync(codes);
            }
            catch
            {
                studentMap = new Dictionary<string, ExternalStudentDto>();
            }

            var now = _dateTime.Now;

            return enrollments.Select(e =>
            {
                var student = studentMap.GetValueOrDefault(e.StudentCode);
                var effectiveStart = e.AssignmentLinks.Any() ? e.AssignmentLinks.Min(a => a.StartDate) : e.StartDate;
                var effectiveDue   = e.AssignmentLinks.Any() ? e.AssignmentLinks.Max(a => a.DueDate)   : e.DueDate;

                string status;
                if (e.IsCompleted)
                    status = "Completed";
                else if (effectiveStart.HasValue && effectiveStart > now)
                    status = "Upcoming";
                else if (effectiveDue.HasValue && effectiveDue < now)
                    status = "Expired";
                else if (e.Progress > 0)
                    status = "InProgress";
                else
                    status = "NotStarted";

                return new CourseLearnerDto
                {
                    Id            = e.Id,
                    StudentCode   = e.StudentCode,
                    StudentName   = student?.Name ?? e.StudentCode,
                    Division      = student?.Division,
                    Department    = student?.Department,
                    Position      = student?.Position,
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
                        StudentGroupId           = first.StudentGroupId
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