using iLearn.Application.Interfaces.Services;
using System.IO.Compression;
using System.Xml.Linq;

namespace iLearn.Infrastructure.Services
{
    public class ScormService : IScormService
    {
        private readonly string _webRootPath;

        public ScormService()
        {
            // กำหนด Path ที่จะแตกไฟล์ (เช่น wwwroot/scorm)
            // ในทางปฏิบัติคุณอาจรับค่านี้ผ่าน Constructor หรือ Configuration
            var currentDirectory = Directory.GetCurrentDirectory();
            _webRootPath = Path.Combine(currentDirectory, "wwwroot", "scorm");
        }

        public async Task<string> ExtractAndParseScormAsync(byte[] fileContent, string folderName)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new ArgumentException("File content is empty.");

            // 1. เตรียม Path ปลายทาง
            var destinationPath = Path.Combine(_webRootPath, folderName);
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true); // ลบของเก่าถ้ามี (Re-upload)
            }
            Directory.CreateDirectory(destinationPath);

            // 2. สร้างไฟล์ Zip ชั่วคราวเพื่อเตรียมแตกไฟล์
            var tempZipPath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempZipPath, fileContent);

            try
            {
                // 3. แตกไฟล์ (Unzip)
                ZipFile.ExtractToDirectory(tempZipPath, destinationPath);
            }
            finally
            {
                // ลบไฟล์ zip ชั่วคราวทิ้งเสมอ
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }

            // 4. อ่าน imsmanifest.xml เพื่อหาทางเข้า (Launch URL)
            var manifestPath = Path.Combine(destinationPath, "imsmanifest.xml");
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("imsmanifest.xml not found in the SCORM package.");
            }

            return ParseManifest(manifestPath);
        }

        private string ParseManifest(string manifestPath)
        {
            var xDocument = XDocument.Load(manifestPath);
            if (xDocument.Root == null) throw new Exception("Invalid manifest file.");

            // ตรวจสอบ Version จาก xmlns
            var defaultNamespace = xDocument.Root.GetDefaultNamespace().NamespaceName;
            string? resourceHref = null;

            if (defaultNamespace.Contains("imscp_v1p1")) // SCORM 1.2
            {
                XNamespace ns = defaultNamespace;
                XNamespace adlcp = "http://www.adlnet.org/xsd/adlcp_rootv1p2";

                // หา Resource ที่เป็น scormtype="sco"
                var resource = xDocument.Descendants(ns + "resource")
                    .FirstOrDefault(x =>
                        (string?)x.Attribute("type") == "webcontent" &&
                        (string?)x.Attribute(adlcp + "scormtype") == "sco");

                resourceHref = resource?.Attribute("href")?.Value;
            }
            else // SCORM 2004 or others
            {
                // ใช้ Logic กลางๆ หรือปรับตาม Namespace จริงของ 2004
                XNamespace ns = xDocument.Root.GetDefaultNamespace();

                // พยายามหา resource แรกที่มี href
                var resource = xDocument.Descendants(ns + "resource")
                    .FirstOrDefault(x =>
                        (string?)x.Attribute("type") == "webcontent" &&
                        x.Attribute("href") != null);

                resourceHref = resource?.Attribute("href")?.Value;
            }

            if (string.IsNullOrEmpty(resourceHref))
            {
                // Fallback: ถ้าหาไม่เจอ ให้ลองหาไฟล์ index.html หรือ story.html (Articulate)
                return "index.html";
            }

            return resourceHref;
        }
    }
}