using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Xml.Linq;

namespace iLearn.Infrastructure.Services
{
    public class ScormService : IScormService
    {
        private readonly FileSettings _settings;
        private static readonly string[] AllowedScormVersions = { "1.2", "2004" };

        public ScormService(IOptions<FileSettings> settings)
        {
            _settings = settings.Value;
        }
        public string GetScormUrl(string folderName, string resourceHref)
        {
            return Path.Combine(_settings.FileUrl, folderName, resourceHref);
        }
        public void DeleteScormFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return;

            var directoryPath = Path.Combine(_settings.FileUnc, folderName);

            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.Delete(directoryPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cannot delete folder {directoryPath}: {ex.Message}");
                }
            }
        }

        public (int FileCount, long TotalSize) GetFolderInfo(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return (0, 0);

            var directoryPath = Path.Combine(_settings.FileUnc, folderName);
            if (!Directory.Exists(directoryPath)) return (0, 0);

            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                long totalSize = files.Sum(f => new FileInfo(f).Length);
                return (files.Length, totalSize);
            }
            catch
            {
                return (0, 0);
            }
        }

        public async Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new ArgumentException("File content is empty.");

            // 1. ????? Path ???????
            var destinationPath = Path.Combine(_settings.FileUnc, folderName);

            // ???????????????????
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            Directory.CreateDirectory(destinationPath);

            // 2. ????????? Zip ????????
            var tempZipPath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempZipPath, fileContent);

            try
            {
                // 3. ?????????????? Zip ??????????
                if (!IsValidZipFile(tempZipPath))
                {
                    throw new InvalidScormPackageException("???????????????????????? ZIP ??????????");
                }

                // 4. ???????????? imsmanifest.xml ?? root ??? zip
                if (!ContainsManifestFile(tempZipPath))
                {
                    throw new InvalidScormPackageException(
                        "???? ZIP ????? 'imsmanifest.xml' ??????? root - ????????????? SCORM ??????????"
                    );
                }

                // 5. ???????
                ZipFile.ExtractToDirectory(tempZipPath, destinationPath);
            }
            finally
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }

            // 6. ?????????????? Manifest
            var manifestPath = Path.Combine(destinationPath, "imsmanifest.xml");
            var manifestInfo = ValidateAndParseManifest(manifestPath);

            // 7. ????? DTO ??????????????????
            return new ScormManifestDto
            {
                ResourceHref = manifestInfo.ResourceHref,
                SchemaVersion = manifestInfo.SchemaVersion,
                FolderName = folderName,
                FullUrl = $"{_settings.FileUrl}/{folderName}/{manifestInfo.ResourceHref}"
            };
        }

        /// <summary>
        /// ?????????????????? ZIP ??????????
        /// </summary>
        private bool IsValidZipFile(string filePath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(filePath);
                return archive.Entries.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ???????????? imsmanifest.xml ?? root ??? zip
        /// </summary>
        private bool ContainsManifestFile(string zipPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                return archive.Entries.Any(e =>
                    e.FullName.Equals("imsmanifest.xml", StringComparison.OrdinalIgnoreCase) &&
                    !e.FullName.Contains("/") && !e.FullName.Contains("\\")
                );
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ?????????????? Manifest ????????????????????
        /// </summary>
        private (string ResourceHref, string SchemaVersion) ValidateAndParseManifest(string manifestPath)
        {
            // Default Values
            string resourceHref = string.Empty;
            string schemaVersion = string.Empty;

            if (!File.Exists(manifestPath))
            {
                throw new InvalidScormPackageException("????????? imsmanifest.xml");
            }

            try
            {
                var xDocument = XDocument.Load(manifestPath);
                if (xDocument.Root == null)
                {
                    throw new InvalidScormPackageException("???? imsmanifest.xml ??????????????????");
                }

                XNamespace ns = xDocument.Root.GetDefaultNamespace();

                // ========================================
                // ?? ??????? SCORM Version (??????)
                // ========================================
                schemaVersion = DetectScormVersion(xDocument, ns);

                if (string.IsNullOrEmpty(schemaVersion))
                {
                    throw new InvalidScormPackageException(
                        "????????????????????? SCORM ??? - ???????????????? imsmanifest.xml"
                    );
                }

                if (!AllowedScormVersions.Contains(schemaVersion))
                {
                    throw new InvalidScormPackageException(
                        $"??????????????? SCORM ???????? 1.2 ??? 2004 ???????? (??????: {schemaVersion})"
                    );
                }

                // ========================================
                // ?? ?? Resource Href (Launch Page) - ??????
                // ========================================
                resourceHref = FindLaunchPage(xDocument, ns);

                if (string.IsNullOrEmpty(resourceHref))
                {
                    throw new InvalidScormPackageException(
                        "????? Launch Page (SCO Resource) ?? imsmanifest.xml"
                    );
                }

                // ?????????????? Launch Page ??????????
                var manifestDir = Path.GetDirectoryName(manifestPath);
                var launchFilePath = Path.Combine(manifestDir!, resourceHref.Replace("/", "\\"));

                if (!File.Exists(launchFilePath))
                {
                    throw new InvalidScormPackageException(
                        $"????????? Launch Page: {resourceHref}"
                    );
                }

                Console.WriteLine($"? SCORM Validation Passed:");
                Console.WriteLine($"   ?? Version: {schemaVersion}");
                Console.WriteLine($"   ?? Launch Page: {resourceHref}");

                return (resourceHref, schemaVersion);
            }
            catch (InvalidScormPackageException)
            {
                throw; // ?????? exception ??????????????
            }
            catch (Exception ex)
            {
                throw new InvalidScormPackageException(
                    $"??????????????????????????? imsmanifest.xml: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// ??????????????? SCORM
        /// </summary>
        private string DetectScormVersion(XDocument xDocument, XNamespace ns)
        {
            // ??????? 1: ?????????? metadata/schemaversion
            var metadata = xDocument.Descendants(ns + "metadata").FirstOrDefault();
            if (metadata != null)
            {
                var schemaVersionElement = metadata.Descendants(ns + "schemaversion").FirstOrDefault();
                if (schemaVersionElement != null)
                {
                    string version = schemaVersionElement.Value.Trim();

                    // SCORM 2004
                    if (version.Contains("2004", StringComparison.OrdinalIgnoreCase) ||
                        version.Contains("1.3", StringComparison.OrdinalIgnoreCase) ||
                        version.Contains("CAM", StringComparison.OrdinalIgnoreCase))
                    {
                        return "2004";
                    }

                    // SCORM 1.2
                    if (version.Contains("1.2", StringComparison.OrdinalIgnoreCase))
                    {
                        return "1.2";
                    }
                }
            }

            // ??????? 2: ?????????? Root Element Attributes
            var versionAttr = xDocument.Root?.Attribute("version");
            if (versionAttr != null)
            {
                string version = versionAttr.Value.Trim();
                if (version.Contains("2004") || version.Contains("1.3"))
                {
                    return "2004";
                }
                if (version.Contains("1.2"))
                {
                    return "1.2";
                }
            }

            // ??????? 3: ?????????? Namespace
            string namespaceUri = xDocument.Root?.Name.NamespaceName ?? string.Empty;

            // SCORM 2004 Namespaces
            if (namespaceUri.Contains("imscp_v1p1", StringComparison.OrdinalIgnoreCase) ||
                namespaceUri.Contains("imscp_v1p2", StringComparison.OrdinalIgnoreCase) ||
                namespaceUri.Contains("adlcp:"))
            {
                return "2004";
            }

            // SCORM 1.2 Namespace
            if (namespaceUri.Contains("imscp_rootv1p1p2", StringComparison.OrdinalIgnoreCase))
            {
                return "1.2";
            }

            // ??????? 4: ?????????? schemaLocation
            var schemaLocation = xDocument.Root?.Attributes()
                .FirstOrDefault(a => a.Name.LocalName == "schemaLocation");

            if (schemaLocation != null)
            {
                string location = schemaLocation.Value;
                if (location.Contains("2004", StringComparison.OrdinalIgnoreCase))
                {
                    return "2004";
                }
                if (location.Contains("1.2", StringComparison.OrdinalIgnoreCase))
                {
                    return "1.2";
                }
            }

            return string.Empty; // ?????????????
        }

        /// <summary>
        /// ????? Launch Page ??? Manifest
        /// </summary>
        private string FindLaunchPage(XDocument xDocument, XNamespace ns)
        {
            // ??????? 1: ?? SCO (Sharable Content Object)
            var scoResource = xDocument.Descendants(ns + "resource")
                .FirstOrDefault(x =>
                {
                    var typeAttr = x.Attribute("type");
                    if (typeAttr?.Value != "webcontent") return false;

                    var scormTypeAttr = x.Attributes()
                        .FirstOrDefault(a => a.Name.LocalName.Equals("scormType", StringComparison.OrdinalIgnoreCase) ||
                                             a.Name.LocalName.Equals("scormtype", StringComparison.OrdinalIgnoreCase));

                    return scormTypeAttr?.Value.Equals("sco", StringComparison.OrdinalIgnoreCase) == true;
                });

            if (scoResource != null)
            {
                var href = scoResource.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(href))
                {
                    return href.Replace("\\", "/");
                }
            }
            
            // ??????? 2: ?? resource ????????????? webcontent
            var firstResource = xDocument.Descendants(ns + "resource")
                .FirstOrDefault(x =>
                    x.Attribute("type")?.Value == "webcontent" &&
                    x.Attribute("href") != null);

            if (firstResource != null)
            {
                var href = firstResource.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(href))
                {
                    return href.Replace("\\", "/");
                }
            }

            return string.Empty;
        }
    }
}
