using iLearn.Application.Common;
using iLearn.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace iLearn.Tests
{
    public sealed class ScormUploadValidationTests
    {
        [Fact]
        public void EnsureValidScormPackageUpload_AllowsZipSignatureAndExpectedMetadata()
        {
            var file = CreateFormFile("course.zip", "application/zip", CreateZipHeaderBytes());

            ScormUploadValidation.EnsureValidScormPackageUpload(file);
        }

        [Fact]
        public void EnsureValidScormPackageUpload_RejectsUnexpectedExtension()
        {
            var file = CreateFormFile("course.html", "application/zip", CreateZipHeaderBytes());

            var exception = Assert.Throws<InvalidScormPackageException>(() =>
                ScormUploadValidation.EnsureValidScormPackageUpload(file));

            Assert.Contains(".zip", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValidScormPackageUpload_RejectsUnexpectedContentType()
        {
            var file = CreateFormFile("course.zip", "text/html", CreateZipHeaderBytes());

            var exception = Assert.Throws<InvalidScormPackageException>(() =>
                ScormUploadValidation.EnsureValidScormPackageUpload(file));

            Assert.Contains(".zip", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void EnsureValidScormPackageUpload_RejectsInvalidZipSignature()
        {
            var file = CreateFormFile("course.zip", "application/zip", new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C });

            var exception = Assert.Throws<InvalidScormPackageException>(() =>
                ScormUploadValidation.EnsureValidScormPackageUpload(file));

            Assert.Contains("ZIP archive", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("a.zip", "a")]
        [InlineData("a.ZIP", "a")]
        [InlineData("a.b.zip", "a.b")]
        [InlineData("a", "a")]
        [InlineData("", "")]
        [InlineData("a.zipx", "a.zipx")]
        public void StripArchiveExtension_TrimsOnlyTrailingZipSuffix(string input, string expected)
        {
            var result = ScormUploadValidation.StripArchiveExtension(input);

            Assert.Equal(expected, result);
        }

        private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
        {
            var stream = new MemoryStream(content);

            return new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static byte[] CreateZipHeaderBytes()
        {
            return new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };
        }
    }
}