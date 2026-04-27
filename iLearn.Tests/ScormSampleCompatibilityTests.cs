using iLearn.Application.Common;
using iLearn.Application.DTOs;
using iLearn.Application.Exceptions;
using iLearn.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace iLearn.Tests
{
    public sealed class ScormSampleCompatibilityTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly ScormService _service;

        public ScormSampleCompatibilityTests()
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
        public async Task ExtractAndParseScormAsync_SupportsUseCaseScorm12And2004Packages()
        {
            var packagePaths = GetSamplePackages("USECASE")
                .Where(path => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (packagePaths.Count == 0)
            {
                return;
            }

            foreach (var packagePath in packagePaths)
            {
                var result = await ExtractPackageAsync(packagePath);

                Assert.Contains(result.SchemaVersion, new[] { "1.2", "2004" });
                Assert.False(string.IsNullOrWhiteSpace(result.ResourceHref));
            }
        }

        [Fact]
        public async Task ExtractAndParseScormAsync_SupportsGolfExamplesForScorm12And2004Only()
        {
            var packagePaths = GetSamplePackages("AllGolfExamples")
                .Where(path => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (packagePaths.Count == 0)
            {
                return;
            }

            foreach (var packagePath in packagePaths)
            {
                if (Path.GetFileName(packagePath).Contains("SCORM11", StringComparison.OrdinalIgnoreCase))
                {
                    await Assert.ThrowsAsync<InvalidScormPackageException>(() => ExtractPackageAsync(packagePath));
                    continue;
                }

                var result = await ExtractPackageAsync(packagePath);

                Assert.Contains(result.SchemaVersion, new[] { "1.2", "2004" });
                Assert.False(string.IsNullOrWhiteSpace(result.ResourceHref));
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        private async Task<ScormManifestDto> ExtractPackageAsync(string packagePath)
        {
            var packageBytes = await File.ReadAllBytesAsync(packagePath);
            var folderName = Path.GetFileNameWithoutExtension(packagePath).Replace('.', '-');

            return await _service.ExtractAndParseScormAsync(packageBytes, folderName);
        }

        private static IEnumerable<string> GetSamplePackages(string sampleSetName)
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                return Enumerable.Empty<string>();
            }

            var sampleDirectory = Path.Combine(repoRoot, "SampleSCORM", sampleSetName);
            return Directory.Exists(sampleDirectory)
                ? Directory.GetFiles(sampleDirectory, "*.zip", SearchOption.TopDirectoryOnly)
                : Enumerable.Empty<string>();
        }

        private static string? FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(Environment.CurrentDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "iLearn.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}