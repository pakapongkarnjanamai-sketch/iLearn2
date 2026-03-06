using iLearn.Application.DTOs;
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
                includeProperties: "Category,Versions,CourseType"
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
                throw new InvalidOperationException($"รหัสวิชา '{model.CourseCode}' ถูกใช้งานไปแล้ว");
            }

            var course = new Course
            {
                Code = model.CourseCode,
                Title = model.CourseName,
                CategoryId = model.CategoryId,
                Description = model.Description,
                CourseTypeId = model.CourseType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _courseRepo.AddAsync(course);

            var courseVersion = new CourseVersion
            {
                CourseId = course.Id,
                VersionNumber = 1,
                Note = "Initial Create",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _courseVersionRepository.AddAsync(courseVersion);

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
                CourseTypeId = model.CourseType,
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
            course.CourseTypeId = dto.CourseType;

            await _courseRepo.UpdateAsync(course);

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

            // 4. เริ่ม Transaction ทำการตัดความสัมพันธ์ก่อนลบ
            using (var transaction = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
            {
                //// 🌟 [ปรับปรุง] แทนที่จะลบ ให้เรา "ตัดความสัมพันธ์ (Unlink)" เพื่อเก็บประวัติไว้
                //// 1. อัปเดต LearningLog ไม่ให้ผูกติดกับ Version/Resource ที่กำลังจะถูกลบ
                //foreach (var log in learningLogs)
                //{
                //    log.CourseVersionId = null;
                //    log.ResourceId = null;
                //    await _learningLogRepository.UpdateAsync(log);
                //}

                //// 2. อัปเดต Enrollment ไม่ให้ผูกติดกับ Course/Assignment ที่กำลังจะถูกลบ
                //foreach (var enrollment in enrollments)
                //{
                //    enrollment.CourseId = null;
                //    enrollment.AssignmentRuleId = null;
                //    // อาจจะเก็บชื่อคอร์สเดิมไว้ในฟิลด์อื่นถ้ามีการออกแบบเผื่อไว้
                //    await _enrollmentRepository.UpdateAsync(enrollment);
                //}

                // 3. ลบรายการ Assignment ที่ผูกกับคอร์สนี้ทิ้ง
                foreach (var assignment in assignments)
                    await _assignmentRepository.DeleteAsync(assignment);

                // ลบความสัมพันธ์ระหว่าง Version กับ Resource
                foreach (var cr in courseResources)
                    await _courseResourceRepository.DeleteAsync(cr);

                // ลบ Version ต่างๆ ของคอร์ส
                foreach (var v in versions)
                    await _courseVersionRepository.DeleteAsync(v);

                // ลบ Resource และ File ทิ้ง
                foreach (var r in resourcesToDelete)
                    await _resourceRepository.DeleteAsync(r);

                foreach (var f in fileStoragesToDelete)
                    await _fileStorageRepository.DeleteAsync(f);

                // สุดท้าย ลบ Course หลัก
                await _courseRepo.DeleteAsync(course);

                // กดยืนยันการเปลี่ยนแปลงทั้งหมด
                transaction.Complete();
            }

            foreach (var folder in scormFoldersToDelete)
            {
                _scormService.DeleteScormFolder(folder);
            }
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
                        Order = orderIndex++, // 🌟 เก็บค่า Order
                        CreatedAt = DateTime.UtcNow
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
                throw new KeyNotFoundException($"Course ID: {id} ไม่พบในระบบ");

            if (isActive)
            {
                // 1. ตรวจสอบว่ามี CourseVersion ที่เปิดใช้งานอยู่หรือไม่
                var activeVersions = await _courseVersionRepository.GetAsync(v => v.CourseId == id && v.IsActive);
                var activeVersion = activeVersions.FirstOrDefault();

                if (activeVersion == null)
                    throw new InvalidOperationException("ไม่สามารถเปิดใช้งานคอร์สได้ เนื่องจากยังไม่มีเวอร์ชัน (Version) ที่เปิดใช้งานอยู่");

                // 2. ตรวจสอบว่าเวอร์ชันที่ใช้งานอยู่ มีเนื้อหาบทเรียน (CourseResource) หรือไม่
                var courseResources = await _courseResourceRepository.GetAsync(
                    filter: cr => cr.CourseVersionId == activeVersion.Id,
                    includeProperties: "Resource"
                );

                if (!courseResources.Any())
                    throw new InvalidOperationException("ไม่สามารถเปิดใช้งานคอร์สได้ เนื่องจากเวอร์ชันปัจจุบันยังไม่มีการเพิ่มเนื้อหาบทเรียน");

                // 3. ตรวจสอบความสมบูรณ์ของไฟล์/ข้อมูลใน Resource
                foreach (var cr in courseResources)
                {
                    if (cr.Resource == null)
                        throw new InvalidOperationException("ไม่สามารถเปิดใช้งานคอร์สได้ เนื่องจากพบเนื้อหาบทเรียนที่สูญหายหรืออ้างอิงไม่ถูกต้อง");

                    if (!cr.Resource.FileStorageId.HasValue && string.IsNullOrWhiteSpace(cr.Resource.URL))
                        throw new InvalidOperationException($"ไม่สามารถเปิดใช้งานคอร์สได้ เนื่องจากเนื้อหา '{cr.Resource.Name}' ไม่สมบูรณ์ (ไม่มีไฟล์หรือ URL แนบมาด้วย)");
                }
            }
            else
            {
                // 🔒 ตรวจสอบว่ามี Enrollment ที่ยังไม่เสร็จสิ้น (In Progress) อยู่หรือไม่ ก่อนอนุญาตให้ปิดคอร์ส
                var inProgressEnrollments = await _enrollmentRepository.GetAsync(
                    e => e.CourseId == id && !e.IsCompleted
                );

                if (inProgressEnrollments.Any())
                {
                    var count = inProgressEnrollments.Count();
                    throw new InvalidOperationException(
                        $"ไม่สามารถปิดคอร์สได้ เนื่องจากมีผู้เรียนที่กำลังเรียนอยู่ {count} คน กรุณารอให้ผู้เรียนทุกคนเรียนจบก่อน หรือยกเลิก Enrollment ที่เกี่ยวข้องก่อนดำเนินการ"
                    );
                }
            }

            course.IsActive = isActive;
            course.UpdatedAt = DateTime.UtcNow;

            await _courseRepo.UpdateAsync(course);

            return course.IsActive;
        }
    }
}