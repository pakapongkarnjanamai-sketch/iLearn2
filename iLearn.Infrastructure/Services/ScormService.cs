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
        public string GetScormUrl(string folderName, string launchHref)
        {
            if (!TryNormalizeRelativePath(folderName, out var safeFolderName))
            {
                return string.Empty;
            }

            (string FilePath, string Suffix) safeLaunchHref;
            try
            {
                safeLaunchHref = NormalizeLaunchHrefOrThrow(launchHref, "SCORM contentItem");
            }
            catch (InvalidScormPackageException)
            {
                return string.Empty;
            }

            return CombineUrlSegments(_settings.FileUrl, safeFolderName, safeLaunchHref.FilePath) + safeLaunchHref.Suffix;
        }
        public void DeleteScormFolder(string folderName)
        {
            if (string.IsNullOrEmpty(folderName)) return;

            if (!TryGetSafeDirectoryPath(folderName, out var directoryPath))
            {
                return;
            }

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

            if (!TryGetSafeDirectoryPath(folderName, out var directoryPath))
            {
                return (0, 0);
            }

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

            if (fileContent.LongLength > ScormPackageLimits.MaxCompressedPackageBytes)
            {
                throw new InvalidScormPackageException("SCORM package exceeds the maximum allowed upload size.");
            }

            var safeFolderName = NormalizeRelativePathOrThrow(folderName, "SCORM folder");
            var destinationPath = GetSafePathUnderRoot(_settings.FileUnc, safeFolderName);

            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            Directory.CreateDirectory(destinationPath);

            var tempZipPath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempZipPath, fileContent);

            string manifestRelativePath;

            try
            {
                if (!IsValidZipFile(tempZipPath))
                {
                    throw new InvalidScormPackageException("Uploaded file is not a valid ZIP archive.");
                }

                manifestRelativePath = FindManifestPath(tempZipPath);

                EnsureArchiveEntriesStayUnderPackageRoot(tempZipPath);
                ZipFile.ExtractToDirectory(tempZipPath, destinationPath);
            }
            finally
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            }

            var manifestPath = GetSafePathUnderRoot(destinationPath, manifestRelativePath);
            var manifestInfo = ValidateAndParseManifest(manifestPath);
            var launchHref = CombineManifestRelativeLaunchPath(manifestRelativePath, manifestInfo.LaunchHref);

            return new ScormManifestDto
            {
                LaunchHref = launchHref,
                SchemaVersion = manifestInfo.SchemaVersion,
                FolderName = safeFolderName,
                FullUrl = CombineUrlSegments(_settings.FileUrl, safeFolderName, launchHref)
            };
        }

        public async Task<ScormManifestDto> ExtractAndParseScormFromFileAsync(string zipFilePath, string folderName)
        {
            if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
                throw new ArgumentException("ZIP file path is invalid or file does not exist.");

            var fileLength = new FileInfo(zipFilePath).Length;
            if (fileLength > ScormPackageLimits.MaxCompressedPackageBytes)
            {
                throw new InvalidScormPackageException("SCORM package exceeds the maximum allowed upload size.");
            }

            var safeFolderName = NormalizeRelativePathOrThrow(folderName, "SCORM folder");
            var destinationPath = GetSafePathUnderRoot(_settings.FileUnc, safeFolderName);

            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }
            Directory.CreateDirectory(destinationPath);

            string manifestRelativePath;

            if (!IsValidZipFile(zipFilePath))
            {
                throw new InvalidScormPackageException("Uploaded file is not a valid ZIP archive.");
            }

            manifestRelativePath = FindManifestPath(zipFilePath);

            EnsureArchiveEntriesStayUnderPackageRoot(zipFilePath);
            ZipFile.ExtractToDirectory(zipFilePath, destinationPath);

            var manifestPath = GetSafePathUnderRoot(destinationPath, manifestRelativePath);
            var manifestInfo = ValidateAndParseManifest(manifestPath);
            var launchHref = CombineManifestRelativeLaunchPath(manifestRelativePath, manifestInfo.LaunchHref);

            return new ScormManifestDto
            {
                LaunchHref = launchHref,
                SchemaVersion = manifestInfo.SchemaVersion,
                FolderName = safeFolderName,
                FullUrl = CombineUrlSegments(_settings.FileUrl, safeFolderName, launchHref)
            };
        }

        public void DeleteArchiveFile(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath)) return;

            try
            {
                var fullPath = GetArchiveFullPath(storagePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete archive file '{storagePath}': {ex.Message}");
            }
        }

        public string GetArchiveFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Relative path is empty.");

            var hostUnc = _settings.HostUnc?.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          ?? string.Empty;
            var fullPath = Path.GetFullPath(Path.Combine(hostUnc, relativePath));

            // Path-traversal guard: full path must stay under HostUnc
            if (!fullPath.StartsWith(hostUnc, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Archive path escapes storage root: {relativePath}");
            }

            return fullPath;
        }

        public async Task<string> SavePackageToArchiveAsync(Stream stream, string archiveFileName)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (string.IsNullOrWhiteSpace(archiveFileName))
                throw new ArgumentException("Archive file name is required.");

            // Build paths: {HostUnc}\{CourseFolder}\_archives\{archiveFileName}
            var archivesDir = Path.Combine(_settings.FileUnc, "_archives");
            Directory.CreateDirectory(archivesDir);

            var finalPath = Path.Combine(archivesDir, archiveFileName);
            var tempPath = finalPath + ".tmp";

            try
            {
                // Stream to temp file first to avoid partial files on failure
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920))
                {
                    await stream.CopyToAsync(fs);
                }

                // Atomic move (same volume)
                File.Move(tempPath, finalPath, overwrite: true);
            }
            catch
            {
                // Clean up temp file on failure
                if (File.Exists(tempPath)) File.Delete(tempPath);
                throw;
            }

            // Return relative path from HostUnc
            var courseFolder = (_settings.CourseFolder ?? "Courses").Trim().Trim('/', '\\');
            return Path.Combine(courseFolder, "_archives", archiveFileName);
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

        private string FindManifestPath(string zipPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);

                var manifestPaths = archive.Entries
                    .Where(entry => !IsDirectoryEntry(entry))
                    .Select(entry => TryNormalizeRelativePath(entry.FullName, out var normalizedPath)
                        ? normalizedPath
                        : string.Empty)
                    .Where(path => IsManifestPath(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (manifestPaths.Count == 0)
                {
                    throw new InvalidScormPackageException("SCORM package must contain imsmanifest.xml.");
                }

                if (manifestPaths.Count > 1)
                {
                    throw new InvalidScormPackageException("SCORM package contains multiple imsmanifest.xml files.");
                }

                return manifestPaths[0];
            }
            catch (InvalidScormPackageException)
            {
                throw;
            }
            catch
            {
                throw new InvalidScormPackageException("Unable to inspect SCORM package manifest.");
            }
        }

        private void EnsureArchiveEntriesStayUnderPackageRoot(string zipPath)
        {
            using var archive = ZipFile.OpenRead(zipPath);

            if (archive.Entries.Count > ScormPackageLimits.MaxArchiveEntries)
            {
                throw new InvalidScormPackageException("SCORM package contains too many archive entries.");
            }

            long totalUncompressedBytes = 0;

            foreach (var entry in archive.Entries)
            {
                NormalizeRelativePathOrThrow(entry.FullName, "SCORM archive entry");

                if (IsDirectoryEntry(entry))
                {
                    continue;
                }

                if (entry.Length > ScormPackageLimits.MaxSingleEntryUncompressedBytes)
                {
                    throw new InvalidScormPackageException("SCORM package contains an entry that exceeds the maximum allowed size.");
                }

                try
                {
                    totalUncompressedBytes = checked(totalUncompressedBytes + entry.Length);
                }
                catch (OverflowException)
                {
                    throw new InvalidScormPackageException("SCORM package reports an invalid entry size.");
                }

                if (totalUncompressedBytes > ScormPackageLimits.MaxTotalUncompressedBytes)
                {
                    throw new InvalidScormPackageException("SCORM package expands beyond the maximum allowed size.");
                }
            }
        }

        /// <summary>
        /// ?????????????? Manifest ????????????????????
        /// </summary>
        private (string LaunchHref, string SchemaVersion) ValidateAndParseManifest(string manifestPath)
        {
            // Default Values
            string launchHref = string.Empty;
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
                // ?? ?? ContentItem Href (Launch Page) - ??????
                // ========================================
                var launchHrefParts = NormalizeLaunchHrefOrThrow(FindLaunchPage(xDocument, ns), "SCORM launch");
                launchHref = launchHrefParts.FilePath + launchHrefParts.Suffix;

                if (string.IsNullOrEmpty(launchHref))
                {
                    throw new InvalidScormPackageException(
                        "????? Launch Page (SCO ContentItem) ?? imsmanifest.xml"
                    );
                }

                var manifestDir = Path.GetDirectoryName(manifestPath);
                var launchFilePath = GetSafePathUnderRoot(manifestDir!, launchHrefParts.FilePath);

                if (!File.Exists(launchFilePath))
                {
                    throw new InvalidScormPackageException(
                        $"????????? Launch Page: {launchHref}"
                    );
                }

                Console.WriteLine($"? SCORM Validation Passed:");
                Console.WriteLine($"   ?? Version: {schemaVersion}");
                Console.WriteLine($"   ?? Launch Page: {launchHref}");

                return (launchHref, schemaVersion);
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
            var scoContentItem = xDocument.Descendants(ns + "resource")
                .FirstOrDefault(x =>
                {
                    var typeAttr = x.Attribute("type");
                    if (typeAttr?.Value != "webcontent") return false;

                    var scormTypeAttr = x.Attributes()
                        .FirstOrDefault(a => a.Name.LocalName.Equals("scormType", StringComparison.OrdinalIgnoreCase) ||
                                             a.Name.LocalName.Equals("scormtype", StringComparison.OrdinalIgnoreCase));

                    return scormTypeAttr?.Value.Equals("sco", StringComparison.OrdinalIgnoreCase) == true;
                });

            if (scoContentItem != null)
            {
                var href = scoContentItem.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(href))
                {
                    return href.Replace("\\", "/");
                }
            }
            
            // ??????? 2: ?? resource ????????????? webcontent
            var firstContentItem = xDocument.Descendants(ns + "resource")
                .FirstOrDefault(x =>
                    x.Attribute("type")?.Value == "webcontent" &&
                    x.Attribute("href") != null);

            if (firstContentItem != null)
            {
                var href = firstContentItem.Attribute("href")?.Value;
                if (!string.IsNullOrEmpty(href))
                {
                    return href.Replace("\\", "/");
                }
            }

            return string.Empty;
        }

        private bool TryGetSafeDirectoryPath(string folderName, out string directoryPath)
        {
            directoryPath = string.Empty;

            if (!TryNormalizeRelativePath(folderName, out var safeFolderName))
            {
                return false;
            }

            try
            {
                directoryPath = GetSafePathUnderRoot(_settings.FileUnc, safeFolderName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeRelativePathOrThrow(string path, string description)
        {
            if (TryNormalizeRelativePath(path, out var normalizedPath))
            {
                return normalizedPath;
            }

            throw new InvalidScormPackageException($"Invalid {description} path.");
        }

        private static (string FilePath, string Suffix) NormalizeLaunchHrefOrThrow(string href, string description)
        {
            if (string.IsNullOrWhiteSpace(href) || href.IndexOfAny(['\r', '\n']) >= 0)
            {
                throw new InvalidScormPackageException($"Invalid {description} path.");
            }

            var candidate = href.Trim().Replace('\\', '/');
            var suffixIndex = IndexOfLaunchHrefSuffix(candidate);
            var pathPart = suffixIndex >= 0 ? candidate[..suffixIndex] : candidate;
            var suffix = suffixIndex >= 0 ? candidate[suffixIndex..] : string.Empty;

            return (NormalizeRelativePathOrThrow(pathPart, description), suffix);
        }

        private static int IndexOfLaunchHrefSuffix(string href)
        {
            var queryIndex = href.IndexOf('?');
            var fragmentIndex = href.IndexOf('#');

            if (queryIndex < 0)
            {
                return fragmentIndex;
            }

            if (fragmentIndex < 0)
            {
                return queryIndex;
            }

            return Math.Min(queryIndex, fragmentIndex);
        }

        private static bool TryNormalizeRelativePath(string path, out string normalizedPath)
        {
            normalizedPath = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var candidate = path.Trim().Replace('\\', '/');
            if (candidate.StartsWith("/", StringComparison.Ordinal) ||
                candidate.StartsWith("//", StringComparison.Ordinal) ||
                candidate.Contains(':', StringComparison.Ordinal))
            {
                return false;
            }

            var segments = candidate.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0 || segments.Any(segment => segment == "." || segment == ".."))
            {
                return false;
            }

            normalizedPath = string.Join('/', segments);
            return true;
        }

        private static bool IsManifestPath(string normalizedPath)
        {
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            var fileName = normalizedPath.Split('/').LastOrDefault();
            return string.Equals(fileName, "imsmanifest.xml", StringComparison.OrdinalIgnoreCase);
        }

        private static string CombineManifestRelativeLaunchPath(string manifestRelativePath, string launchHref)
        {
            var launchHrefParts = NormalizeLaunchHrefOrThrow(launchHref, "SCORM launch");
            var manifestDirectory = string.Empty;
            var lastSlashIndex = manifestRelativePath.LastIndexOf('/');
            if (lastSlashIndex >= 0)
            {
                manifestDirectory = manifestRelativePath[..lastSlashIndex];
            }

            var combinedPath = string.IsNullOrWhiteSpace(manifestDirectory)
                ? launchHrefParts.FilePath
                : CombineUrlSegments(manifestDirectory, launchHrefParts.FilePath);

            return NormalizeRelativePathOrThrow(combinedPath, "SCORM launch") + launchHrefParts.Suffix;
        }

        private static string GetSafePathUnderRoot(string rootPath, string relativePath)
        {
            var fullRootPath = Path.GetFullPath(rootPath);
            var fullCandidatePath = Path.GetFullPath(Path.Combine(
                fullRootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

            var rootWithTrailingSeparator = EnsureTrailingDirectorySeparator(fullRootPath);
            if (!fullCandidatePath.StartsWith(rootWithTrailingSeparator, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fullCandidatePath, fullRootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidScormPackageException("SCORM path escapes the configured package root.");
            }

            return fullCandidatePath;
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static bool IsDirectoryEntry(ZipArchiveEntry entry)
        {
            return entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                   entry.FullName.EndsWith("\\", StringComparison.Ordinal);
        }

        private static string CombineUrlSegments(string baseUrl, params string[] segments)
        {
            var cleanedSegments = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                cleanedSegments.Add(baseUrl.TrimEnd('/', '\\'));
            }

            cleanedSegments.AddRange(
                segments
                    .Where(segment => !string.IsNullOrWhiteSpace(segment))
                    .Select(segment => segment.Trim('/', '\\')));

            return string.Join('/', cleanedSegments);
        }
    }
}
