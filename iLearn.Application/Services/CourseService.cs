using iLearn.Application.DTOs;
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
        private readonly IGenericRepository<CourseResource> _courseResourceRepository;
        private readonly IGenericRepository<CourseVersion> _courseVersionRepository;
        private readonly ICourseAssignmentService _assignmentService;

        // เพิ่มสำหรับจัดการลบไฟล์ Resource และ SCORM
        private readonly IGenericRepository<Resource> _resourceRepository;
        private readonly IGenericRepository<FileStorage> _fileStorageRepository;
        private readonly IScormService _scormService;

        private readonly IGenericRepository<Enrollment> _enrollmentRepository;
        private readonly IGenericRepository<LearningLog> _learningLogRepository;
        private readonly IGenericRepository<Assignment> _assignmentRepository;

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
            IScormService scormService)
        {
            _courseRepo = courseRepository;
            _courseResourceRepository = courseResourceRepository;
            _courseVersionRepository = courseVersionRepository;
            _assignmentService = assignmentService;
            
                _enrollmentRepository = enrollmentRepository;
                _learningLogRepository = learningLogRepository;
                _assignmentRepository = assignmentRepository;

            _resourceRepository = resourceRepository;
            _fileStorageRepository = fileStorageRepository;
            _scormService = scormService;
        }

        public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync(bool isActive = true)
        {
            var courses = await _courseRepo.GetAsync(
                filter: c => c.IsActive == isActive,
                includeProperties: "Category,Versions"
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

                resourceList = courseResources.Select(cr => new CourseResourceDto
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
                CourseType = (int)course.Type,
                CategoryId = course.CategoryId,
                IsActive = course.IsActive,
                Resources = resourceList
            };
        }

        public async Task<CourseDto> CreateCourseAsync(CourseCreateDto model)
        {
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                throw new InvalidOperationException($"รหัสวิชา '{model.CourseCode}' ถูกใช้งานไปแล้ว");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                CategoryId = model.CategoryId,
                Description = model.Description,
                Type = (CourseType)model.CourseType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _courseRepo.AddAsync(course);

            // Create initial version
            var courseVersion = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Initial Create",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _courseVersionRepository.AddAsync(courseVersion);

            // Add resources
            if (model.ResourceIds?.Count > 0)
            {
                await AddResourcesToCourseVersionAsync(courseVersion.Id, model.ResourceIds);
            }

            return course.ToDto();
        }

        public async Task<CourseDto> CreateCourseWithScormAsync(CourseCreateDto model)
        {
            if (!await _courseRepo.IsCourseCodeUniqueAsync(model.CourseCode))
            {
                throw new InvalidOperationException($"รหัสวิชา '{model.CourseCode}' ถูกใช้งานไปแล้ว");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Type = (CourseType)model.CourseType,
                IsActive = false, // Draft status
                CreatedAt = DateTime.UtcNow
            };

            await _courseRepo.AddAsync(course);

            var version = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Draft (Initial Upload)",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
            await _courseVersionRepository.AddAsync(version);

            // Add resources if provided
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
                throw new KeyNotFoundException($"Course ID: {id} ไม่พบในระบบ");

            course.Title = dto.CourseName;
            course.Description = dto.Description;
            course.Code = dto.CourseCode;
            course.CategoryId = dto.CategoryId;
            course.Type = (CourseType)dto.CourseType;

            await _courseRepo.UpdateAsync(course);

            // Update course resources in active version
            var versions = await _courseVersionRepository.GetAllAsync();
            var activeVersion = versions.FirstOrDefault(v => v.CourseId == id && v.IsActive);

            if (activeVersion != null)
            {
                await ReplaceVersionResourcesAsync(activeVersion.Id, dto.ResourceIds);
            }

            return course.ToDto();
        }
        public async Task DeleteCourseAsync(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {id} ไม่พบในระบบ");

            // =========================================================
            // 1. ค้นหาข้อมูลที่ต้องจัดการทั้งหมดให้พร้อมก่อน
            // =========================================================
            var assignments = await _assignmentRepository.GetAsync(a => a.CourseId == id);
            var enrollments = await _enrollmentRepository.GetAsync(e => e.CourseId == id);
            var enrollmentIds = enrollments.Select(e => e.Id).ToList();

            var versions = await _courseVersionRepository.GetAsync(v => v.CourseId == id);
            var versionIds = versions.Select(v => v.Id).ToList();

            var courseResources = new List<CourseResource>();
            foreach (var vId in versionIds)
            {
                var crs = await _courseResourceRepository.GetAsync(cr => cr.CourseVersionId == vId);
                courseResources.AddRange(crs);
            }

            var resourceIdsToCheck = courseResources.Select(cr => cr.ResourceId).Distinct().ToList();

            var resourcesToDelete = new List<Resource>();
            var fileStoragesToDelete = new List<FileStorage>();
            var scormFoldersToDelete = new List<string>();

            foreach (var resId in resourceIdsToCheck)
            {
                var otherUsages = await _courseResourceRepository.GetAsync(
                    cr => cr.ResourceId == resId && !versionIds.Contains(cr.CourseVersionId)
                );

                if (!otherUsages.Any())
                {
                    var resource = await _resourceRepository.GetByIdAsync(resId);
                    if (resource != null)
                    {
                        resourcesToDelete.Add(resource);
                        if (resource.FileStorageId.HasValue)
                        {
                            var file = await _fileStorageRepository.GetByIdAsync(resource.FileStorageId.Value);
                            if (file != null)
                            {
                                fileStoragesToDelete.Add(file);
                                string ext = Path.GetExtension(file.Name)?.ToLower() ?? "";
                                if (ext == ".zip" && !string.IsNullOrEmpty(resource.URL))
                                {
                                    scormFoldersToDelete.Add(resource.URL);
                                }
                            }
                        }
                    }
                }
            }

            // =========================================================
            // 🌟 2. เริ่มการทำงานด้วย Transaction (ปลอดภัย 100%)
            // =========================================================
            // ใช้ TransactionScopeAsyncFlowOption.Enabled เพื่อให้ทำงานรองรับ async/await
            using (var transaction = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
            {
                // 2.1 ปลดความสัมพันธ์ (Set Null) ในประวัติการเรียนและการมอบหมายงาน
                //foreach (var assignment in assignments)
                //{
                //    assignment.CourseId = null;
                //    await _assignmentRepository.UpdateAsync(assignment);
                //}

                //foreach (var enrollment in enrollments)
                //{
                //    enrollment.CourseId = null;
                //    enrollment.EnrolledCourseVersion = null;
                //    await _enrollmentRepository.UpdateAsync(enrollment);
                //}

                //foreach (var eId in enrollmentIds)
                //{
                //    var logs = await _learningLogRepository.GetAsync(l => l.EnrollmentId == eId);
                //    foreach (var log in logs)
                //    {
                //        log.CourseVersionId = null;
                //        log.ResourceId = null;
                //        await _learningLogRepository.UpdateAsync(log);
                //    }
                //}

                // 2.2 ลบโครงสร้างไฟล์หลักสูตร (เรียงลำดับจากลูกไปแม่)
                foreach (var cr in courseResources)
                    await _courseResourceRepository.DeleteAsync(cr);

                foreach (var v in versions)
                    await _courseVersionRepository.DeleteAsync(v);

                foreach (var r in resourcesToDelete)
                    await _resourceRepository.DeleteAsync(r);

                foreach (var f in fileStoragesToDelete)
                    await _fileStorageRepository.DeleteAsync(f);

                // 2.3 ลบ Course ตัวแม่
                await _courseRepo.DeleteAsync(course);

                // ยืนยันว่าทุกอย่างทำงานเสร็จสมบูรณ์และไม่มี Error (Commit)
                transaction.Complete();
            }

            // =========================================================
            // 🌟 3. ลบไฟล์จริงบนเซิร์ฟเวอร์ (ทำนอก Transaction)
            // =========================================================
            // เหตุผล: เพราะถ้าลบไฟล์จริงๆ ในโฟลเดอร์ไปแล้ว หาก Database Error จะไม่สามารถเสกไฟล์กลับคืนมาได้ 
            // จึงต้องรอให้ Transaction ของ DB ผ่านชัวร์ๆ ก่อนค่อยทำการลบ Physical file ครับ
            foreach (var folder in scormFoldersToDelete)
            {
                _scormService.DeleteScormFolder(folder);
            }
        }

        public async Task TriggerAssignmentAsync(int courseId)
        {
            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {courseId} ไม่พบในระบบ");

            await _assignmentService.ProcessAssignmentForCourseAsync(courseId);
        }

        // Helper Methods
        private async Task AddResourcesToCourseVersionAsync(int versionId, List<int> resourceIds)
        {
            if (resourceIds?.Count > 0)
            {
                foreach (var resourceId in resourceIds)
                {
                    var courseResource = new CourseResource
                    {
                        CourseVersionId = versionId,
                        ResourceId = resourceId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _courseResourceRepository.AddAsync(courseResource);
                }
            }
        }

        private async Task ReplaceVersionResourcesAsync(int versionId, List<int> newResourceIds)
        {
            // Get current resources
            var allCourseResources = await _courseResourceRepository.GetAllAsync();
            var currentResources = allCourseResources
                .Where(cr => cr.CourseVersionId == versionId)
                .ToList();

            // Delete old resources
            foreach (var item in currentResources)
            {
                await _courseResourceRepository.DeleteAsync(item);
            }

            // Add new resources
            await AddResourcesToCourseVersionAsync(versionId, newResourceIds);
        }

        public async Task<bool> UpdateCourseStatusAsync(int id, bool isActive)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null)
                throw new KeyNotFoundException($"Course ID: {id} ไม่พบในระบบ");

            course.IsActive = isActive;
            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepo.UpdateAsync(course);

            // ถ้าเป็นการ Publish (isActive = true) และเป็น General Course ให้ทำการ Assign ให้อัตโนมัติ
            if (isActive && course.Type == CourseType.General)
            {
                await _assignmentService.ProcessAssignmentForCourseAsync(course.Id);
            }

            return course.IsActive;
        }
    }
}