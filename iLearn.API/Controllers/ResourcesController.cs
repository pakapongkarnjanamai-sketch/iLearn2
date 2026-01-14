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
            IScormService scormService)
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

        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetContent(int id)
        {
            // ดึง Resource
            var resources = await _resourceRepo.GetAsync(r => r.Id == id);
            var resource = resources.FirstOrDefault();

            if (resource == null) return NotFound();

            // ถ้าเป็น SCORM ที่ Active แล้ว ให้ Redirect ไปที่ URL ของไฟล์ index.html
            // (Frontend จะเอา URL นี้ไปใส่ใน iframe)
            if (resource.IsActive && !string.IsNullOrEmpty(resource.URL))
            {
                // return Redirect("~/" + resource.URL); // หรือส่ง URL กลับไปให้ Frontend
                // แต่ปกติ API ควรส่ง URL กลับไปให้ Frontend จัดการ iframe
                return Ok(new { url = resource.URL });
            }

            // ถ้าเป็นไฟล์ธรรมดา (PDF, Video) หรือยังไม่ Active ให้ดึงจาก DB
            var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
            if (fileStorage == null || fileStorage.Data == null) return NotFound("File content missing");

            return File(fileStorage.Data, fileStorage.ContentType, fileStorage.Name);
        }

        // 1. Upload เก็บลง DB ก่อน (ยังไม่ Active)
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)] // 100MB
        public async Task<IActionResult> Upload(IFormFile file, int typeId)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            // 1.1 เก็บไฟล์ลง FileStorage
            var fileStorage = new FileStorage
            {
                Name = file.FileName,
                ContentType = file.ContentType,
                Length = file.Length
            };

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileStorage.Data = ms.ToArray();
            }

            var savedFile = await _fileRepo.AddAsync(fileStorage);

            // 1.2 สร้าง Resource (Active = False)
            var resource = new Resource
            {
                Name = file.FileName,
                TypeId = typeId,
                IsActive = false, // รอการกด Activate
                FileStorageId = savedFile.Id
            };

            var savedResource = await _resourceRepo.AddAsync(resource);

            return Ok(savedResource.ToDto());
        }

        // 2. กดปุ่ม SetPublic เพื่อแตกไฟล์และเปิดใช้งาน
        [HttpPost("SetPublic")] // หรือ {id}/activate
        public async Task<IActionResult> SetPublic([FromQuery] int key)
        {
            try
            {
                // 2.1 ดึง Resource
                var resource = await _resourceRepo.GetByIdAsync(key);
                if (resource == null) return NotFound("Resource not found");

                // ถ้า Active อยู่แล้ว จะให้ปิด หรือจะให้ทำใหม่? 
                // ตาม Logic เดิมคือถ้า Active แล้วให้ Deactivate หรือ Activate ใหม่
                // ในที่นี้ขอทำแบบ Activate ใหม่ถ้ายังไม่มี URL หรือสถานะยังไม่ Active

                // 2.2 ดึงไฟล์ออกมาเพื่อเตรียม Process
                var fileStorage = await _fileRepo.GetByIdAsync(resource.FileStorageId ?? 0);
                if (fileStorage == null || fileStorage.Data == null) return NotFound("Associated file not found");

                // 2.3 ตรวจสอบว่าเป็น Zip (SCORM) หรือไม่
                string extension = Path.GetExtension(resource.Name).ToLower();

                if (extension == ".zip")
                {
                    // SCORM Process
                    string folderName = Guid.NewGuid().ToString(); // สร้าง Folder ใหม่เสมอเพื่อไม่ให้ชนของเดิม

                    // เรียก Service แตกไฟล์
                    string launchUrl = await _scormService.ExtractAndParseScormAsync(fileStorage.Data, folderName);

                    // อัปเดต URL และสถานะ
                    resource.URL = $"scorm/{folderName}/{launchUrl}";
                    resource.IsActive = true;
                }
                else
                {
                    // ไฟล์ทั่วไป (Video/PDF) แค่เปิด Active
                    resource.IsActive = true;
                    // resource.URL = ... (อาจจะเป็น URL สำหรับ Download Controller)
                }

                // 2.4 บันทึกการเปลี่ยนแปลง
                await _resourceRepo.UpdateAsync(resource);

                return Ok(resource.ToDto());
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error processing resource");
                return StatusCode(500, $"Error activating resource: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await _resourceRepo.GetByIdAsync(id);
            if (resource == null) return NotFound();

            // 1. ลบไฟล์จริง (SCORM Folder) ถ้ามี
            if (resource.IsActive && !string.IsNullOrEmpty(resource.URL))
            {
                // แกะชื่อ Folder ออกมาจาก URL
                // URL เก็บรูปแบบ: "https://host/course/{Guid}/index.html"
                // เราต้องการแค่ {Guid}

                // วิธีแกะแบบง่าย (ถ้ามั่นใจ Format) หรือจะเก็บ FolderName แยกใน DB ก็ได้
                // สมมติว่า URL เป็น Relative path หรือมี FolderName เป็นส่วนประกอบ
                // แต่ในโค้ดเก่าคุณเก็บ URL = "scorm/{newGuid}/{launchUrl}"
                // ดังนั้นเราต้องดึงค่า newGuid ออกมา

                var parts = resource.URL.Split('/');
                if (parts.Length >= 2)
                {
                    // parts[0] = "scorm", parts[1] = "Guid"
                    _scormService.DeleteScormFolder(parts[1]);
                }
            }

            // 2. ลบ Resource ใน DB
            await _resourceRepo.DeleteAsync(resource);

            // 3. ลบ FileStorage (Clean up)
            if (resource.FileStorageId.HasValue)
            {
                var file = await _fileRepo.GetByIdAsync(resource.FileStorageId.Value);
                if (file != null) await _fileRepo.DeleteAsync(file);
            }

            return NoContent();
        }
    }
}