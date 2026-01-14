using iLearn.Application.Common;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Xml.Linq;

namespace iLearn.Infrastructure.Services
{
    public class ScormService : IScormService
    {
        private readonly FileSettings _settings;

        // Inject FileSettings ผ่าน IOptions (ค่ามาจาก appsettings.json)
        public ScormService(IOptions<FileSettings> settings)
        {
            _settings = settings.Value;
        }

        public void DeleteScormFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return;

            // หา path จริงจาก URL หรือ ResourceID
            // สมมติว่า folderName คือชื่อโฟลเดอร์ที่เรา Gen ไว้ตอน Upload (Guid)
            var directoryPath = Path.Combine(_settings.FileUnc, folderName);

            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath, true); // true = ลบไฟล์ข้างในด้วย
                }
                catch (Exception ex)
                {
                    // Log warning แต่ไม่ต้อง throw error เพื่อให้การลบใน DB ทำงานต่อได้
                    Console.WriteLine($"Cannot delete folder {directoryPath}: {ex.Message}");
                }
            }
        }

        public async Task<string> ExtractAndParseScormAsync(byte[] fileContent, string folderName)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new ArgumentException("File content is empty.");

            // 1. ใช้ Path จาก Config (HostUnc) ผสมกับ CourseFolder
            // ตัวอย่าง: \\ap-ntc2137-prwb\wwwroot\iLearn\course\{folderName}
            var destinationPath = Path.Combine(_settings.FileUnc, folderName);

            // ตรวจสอบและสร้างโฟลเดอร์ปลายทาง
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true); // ลบของเก่าออกถ้ามี
            }
            Directory.CreateDirectory(destinationPath);

            // 2. สร้างไฟล์ Zip ชั่วคราวในเครื่องที่รัน API (Temp Folder)
            var tempZipPath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempZipPath, fileContent);

            try
            {
                // 3. แตกไฟล์ไปยังโฟลเดอร์ปลายทาง (UNC Path)
                ZipFile.ExtractToDirectory(tempZipPath, destinationPath);
            }
            finally
            {
                // ลบไฟล์ Zip ชั่วคราวทิ้งเสมอเพื่อประหยัดพื้นที่
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }

            // 4. อ่านไฟล์ imsmanifest.xml เพื่อหาไฟล์เริ่มต้น (Launch Page)
            var manifestPath = Path.Combine(destinationPath, "imsmanifest.xml");
            string launchPage = ParseManifest(manifestPath);

            // 5. คืนค่า URL เต็มรูปแบบสำหรับเรียกใช้งาน
            // ตัวอย่าง: https://ap-ntc2137-prwb/iLearn/course/{folderName}/index.html
            // ใช้ '/' เชื่อม URL เสมอ ไม่ใช้ Path.Combine เพราะเป็น HTTP Path
            return $"{_settings.FileUrl}/{folderName}/{launchPage}";
        }

        private string ParseManifest(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                // ถ้าไม่มี manifest ให้เดาว่าเป็น index.html หรือ story.html (กรณีไม่ใช่ SCORM มาตรฐาน)
                return "index.html";
            }

            try
            {
                var xDocument = XDocument.Load(manifestPath);
                if (xDocument.Root == null) return "index.html";

                // ตรวจสอบ Namespace เพื่อรองรับทั้ง SCORM 1.2 และ 2004
                XNamespace ns = xDocument.Root.GetDefaultNamespace();
                string? resourceHref = null;

                // ลองหาแบบ SCORM 1.2 / 2004 ทั่วไป
                var resource = xDocument.Descendants(ns + "resource")
                    .FirstOrDefault(x =>
                        (string?)x.Attribute("type") == "webcontent" &&
                        // เช็คทั้ง scormType (2004) และ scormtype (1.2)
                        (x.Attributes().Any(a => a.Name.LocalName.ToLower() == "scormtype" && a.Value.ToLower() == "sco"))
                    );

                // ถ้าหาแบบเจาะจงไม่เจอ ให้เอา resource ตัวแรกที่มี href
                if (resource == null)
                {
                    resource = xDocument.Descendants(ns + "resource")
                        .FirstOrDefault(x =>
                            (string?)x.Attribute("type") == "webcontent" &&
                            x.Attribute("href") != null);
                }

                resourceHref = resource?.Attribute("href")?.Value;

                return string.IsNullOrEmpty(resourceHref) ? "index.html" : resourceHref;
            }
            catch
            {
                // กรณีไฟล์ XML เสีย หรืออ่านไม่ได้ ให้ Default กลับไปที่ index.html
                return "index.html";
            }
        }
    }
}