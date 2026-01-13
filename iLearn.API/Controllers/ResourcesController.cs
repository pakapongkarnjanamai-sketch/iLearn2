using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly IGenericRepository<Resource> _resourceRepo;
        private readonly IGenericRepository<FileStorage> _fileRepo;
        private readonly IScormService _scormService;
        public ResourcesController(
     IGenericRepository<Resource> resourceRepo,
     IGenericRepository<FileStorage> fileRepo,
     IScormService scormService) // <--- เพิ่มตรงนี้
        {
            _resourceRepo = resourceRepo;
            _fileRepo = fileRepo;
            _scormService = scormService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var resources = await _resourceRepo.GetAllAsync();
            return Ok(resources.Select(r => r.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resource = await _resourceRepo.GetByIdAsync(id);
            if (resource == null) return NotFound();
            return Ok(resource.ToDto());
        }

        // Endpoint สำหรับดึงไฟล์ไปแสดงผล (Video Player / SCORM)
        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetContent(int id)
        {
            // ต้อง Include FileStorage มาด้วย (Repo อาจต้องเพิ่ม Method GetWithInclude หรือใช้ GetAsync)
            // ในที่นี้สมมติว่าใช้ GetAsync ได้ หรือต้องไปแก้ GenericRepo ให้รองรับ Include
            var resources = await _resourceRepo.GetAsync(r => r.Id == id);
            var resource = resources.FirstOrDefault();

            if (resource == null) return NotFound();

            // ดึงข้อมูลไฟล์จริงๆ
            var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
            if (fileStorage == null || fileStorage.Data == null) return NotFound("File content missing");

            // ส่งไฟล์กลับไป (FileResult)
            return File(fileStorage.Data, fileStorage.ContentType, fileStorage.Name);
        }

        // Upload File (รับเป็น Form Data)
        [HttpPost("upload")]
        [RequestSizeLimit(100_000_000)] // จำกัด 100MB (ปรับได้)
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] int typeId)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            // 1. เตรียมเก็บไฟล์ลง DB (FileStorage)
            var fileStorage = new FileStorage
            {
                Name = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length
            };

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileStorage.Data = ms.ToArray(); // แปลงเป็น byte[] 💾
            }

            // บันทึก FileStorage ก่อนเพื่อให้ได้ ID (หรือจะ Add พร้อมกันก็ได้ถ้า EF Config ไว้)
            var savedFile = await _fileRepo.AddAsync(fileStorage);

            var resource = new Resource
            {
                Name = file.FileName,
                TypeId = typeId,
                IsActive = true,
                FileStorageId = savedFile.Id
            };

            // --- เพิ่ม Logic SCORM ตรงนี้ ---
            if (Path.GetExtension(file.FileName).ToLower() == ".zip")
            {
                // แตกไฟล์ไปที่ wwwroot/scorm/{resourceId} (ต้องใช้ ID ที่ยังไม่เกิด หรือใช้ GUID)
                string folderName = Guid.NewGuid().ToString();

                // เรียก Service ให้ทำงาน
                string launchUrl = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);

                // บันทึก URL ลง Resource (ต้องไปเพิ่ม Property URL ใน Resource Entity ถ้ายังไม่มี)
                resource.URL = $"scorm/{folderName}/{launchUrl}";
            }
            // ----------------------------

            var savedResource = await _resourceRepo.AddAsync(resource);
            return Ok(savedResource.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _resourceRepo.GetByIdAsync(id);
            if (resource == null) return NotFound();

            // ลบ Resource
            await _resourceRepo.DeleteAsync(resource);

            // ลบ FileStorage ด้วย (ถ้าไม่ได้ทำ Cascade Delete ใน DB)
            if (resource.FileStorageId.HasValue)
            {
                var file = await _fileRepo.GetByIdAsync(resource.FileStorageId.Value);
                if (file != null) await _fileRepo.DeleteAsync(file);
            }

            return NoContent();
        }
    }
}