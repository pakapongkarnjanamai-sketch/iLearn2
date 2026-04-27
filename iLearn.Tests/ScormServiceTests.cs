using iLearn.Application.Common;
using iLearn.Application.Exceptions;
using iLearn.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text;

namespace iLearn.Tests
{
    public sealed class ScormServiceTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly ScormService _service;

        public ScormServiceTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "iLearn.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);

            _service = new ScormService(Options.Create(new FileSettings
            {
                HostUrl = "https://files.example.local",
                HostUnc = _tempRoot,
                CourseFolder = "course"
            }));
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_RejectsLaunchHrefThatEscapesPackageRoot()
        {
            Directory.CreateDirectory(Path.Combine(_tempRoot, "course"));
            await File.WriteAllTextAsync(Path.Combine(_tempRoot, "course", "outside.html"), "outside");

            var packageBytes = CreateZip(
                ("imsmanifest.xml", CreateManifest("../outside.html")));

            await Assert.ThrowsAsync<InvalidScormPackageException>(() =>
                _service.ExtractAndParseScormAsync(packageBytes, "package-a"));
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_RejectsArchiveEntryThatEscapesPackageRoot()
        {
            var packageBytes = CreateZip(
                ("imsmanifest.xml", CreateManifest("index.html")),
                ("../outside.html", "outside"),
                ("index.html", "ok"));

            await Assert.ThrowsAsync<InvalidScormPackageException>(() =>
                _service.ExtractAndParseScormAsync(packageBytes, "package-b"));
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_AllowsNestedLaunchPageWithinPackageRoot()
        {
            var packageBytes = CreateZip(
                ("imsmanifest.xml", CreateManifest("launch/index.html")),
                ("launch/index.html", "ok"));

            var result = await _service.ExtractAndParseScormAsync(packageBytes, "package-c");

            Assert.Equal("launch/index.html", result.ResourceHref);
            Assert.Equal("https://files.example.local/course/package-c/launch/index.html", result.FullUrl);
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_AllowsManifestUnderWrappingPackageFolder()
        {
            var packageBytes = CreateZip(
                ("wrapped/imsmanifest.xml", CreateManifest("launch/index.html")),
                ("wrapped/launch/index.html", "ok"));

            var result = await _service.ExtractAndParseScormAsync(packageBytes, "package-wrapped");

            Assert.Equal("wrapped/launch/index.html", result.ResourceHref);
            Assert.Equal("https://files.example.local/course/package-wrapped/wrapped/launch/index.html", result.FullUrl);
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_AllowsLaunchHrefWithQueryString()
        {
            var packageBytes = CreateZip(
                ("wrapped/imsmanifest.xml", CreateManifest("launch/index.html?content=playing")),
                ("wrapped/launch/index.html", "ok"));

            var result = await _service.ExtractAndParseScormAsync(packageBytes, "package-query");

            Assert.Equal("wrapped/launch/index.html?content=playing", result.ResourceHref);
            Assert.Equal("https://files.example.local/course/package-query/wrapped/launch/index.html?content=playing", result.FullUrl);
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_RejectsPackageWithMultipleManifests()
        {
            var packageBytes = CreateZip(
                ("imsmanifest.xml", CreateManifest("index.html")),
                ("nested/imsmanifest.xml", CreateManifest("index.html")),
                ("index.html", "ok"));

            await Assert.ThrowsAsync<InvalidScormPackageException>(() =>
                _service.ExtractAndParseScormAsync(packageBytes, "package-multiple"));
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_RejectsPackageWithoutManifest()
        {
            var packageBytes = CreateZip(("index.html", "ok"));

            await Assert.ThrowsAsync<InvalidScormPackageException>(() =>
                _service.ExtractAndParseScormAsync(packageBytes, "package-missing-manifest"));
        }

        [Fact]
        public void GetScormUrl_ReturnsEmptyString_ForUnsafeStoredPaths()
        {
            var result = _service.GetScormUrl("package-d", "../outside.html");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetScormUrl_AllowsStoredLaunchHrefWithQueryString()
        {
            var result = _service.GetScormUrl("package-d", "shared/launchpage.html?content=playing");

            Assert.Equal("https://files.example.local/course/package-d/shared/launchpage.html?content=playing", result);
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_RejectsArchiveWithTooManyEntries()
        {
            var entries = new List<(string Path, string Content)>
            {
                ("imsmanifest.xml", CreateManifest("launch/index.html")),
                ("launch/index.html", "ok")
            };

            for (var index = 0; index < ScormPackageLimits.MaxArchiveEntries; index++)
            {
                entries.Add(($"assets/{index}.txt", "x"));
            }

            var packageBytes = CreateZip(entries.ToArray());

            await Assert.ThrowsAsync<InvalidScormPackageException>(() =>
                _service.ExtractAndParseScormAsync(packageBytes, "package-e"));
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_RejectsArchiveThatExpandsBeyondAllowedSize()
        {
            var packageBytes = CreateZip(
                new ZipEntrySpec("imsmanifest.xml", TextContent: CreateManifest("launch/index.html")),
                new ZipEntrySpec("launch/index.html", TextContent: "ok"),
                new ZipEntrySpec("assets/chunk-1.bin", RepeatedByteCount: 90L * 1024 * 1024),
                new ZipEntrySpec("assets/chunk-2.bin", RepeatedByteCount: 90L * 1024 * 1024),
                new ZipEntrySpec("assets/chunk-3.bin", RepeatedByteCount: 90L * 1024 * 1024));

            await Assert.ThrowsAsync<InvalidScormPackageException>(() =>
                _service.ExtractAndParseScormAsync(packageBytes, "package-f"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        private static byte[] CreateZip(params (string Path, string Content)[] entries)
        {
            return CreateZip(entries.Select(entry => new ZipEntrySpec(entry.Path, TextContent: entry.Content)).ToArray());
        }

        private static byte[] CreateZip(params ZipEntrySpec[] entries)
        {
            using var memory = new MemoryStream();
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entry in entries)
                {
                    var zipEntry = archive.CreateEntry(entry.Path);
                    using var entryStream = zipEntry.Open();

                    if (entry.TextContent is not null)
                    {
                        using var writer = new StreamWriter(entryStream, Encoding.UTF8, 1024, leaveOpen: true);
                        writer.Write(entry.TextContent);
                        writer.Flush();
                    }
                    else
                    {
                        WriteRepeatedBytes(entryStream, entry.RepeatedByteCount);
                    }
                }
            }

            return memory.ToArray();
        }

        private static void WriteRepeatedBytes(Stream stream, long totalBytes)
        {
            var buffer = new byte[8192];
            long remainingBytes = totalBytes;

            while (remainingBytes > 0)
            {
                var bytesToWrite = (int)Math.Min(buffer.Length, remainingBytes);
                stream.Write(buffer, 0, bytesToWrite);
                remainingBytes -= bytesToWrite;
            }
        }

        private static string CreateManifest(string launchHref)
        {
            return $"""
<?xml version="1.0" encoding="UTF-8"?>
<manifest identifier="com.example.course" version="1.0"
          xmlns="http://www.imsglobal.org/xsd/imscp_rootv1p1p2"
          xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
  <metadata>
    <schema>ADL SCORM</schema>
    <schemaversion>1.2</schemaversion>
  </metadata>
  <organizations default="org1">
    <organization identifier="org1">
      <title>Test Course</title>
      <item identifier="item1" identifierref="res1">
        <title>Launch</title>
      </item>
    </organization>
  </organizations>
  <resources>
    <resource identifier="res1" type="webcontent" adlcp:scormType="sco" href="{launchHref}" />
  </resources>
</manifest>
""";
        }

        private sealed record ZipEntrySpec(string Path, string? TextContent = null, long RepeatedByteCount = 0);
    }
}