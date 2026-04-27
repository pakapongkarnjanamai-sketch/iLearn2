using iLearn.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;

namespace iLearn.Application.Common
{
    public static class ScormUploadValidation
    {
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/zip",
            "application/x-zip",
            "application/x-zip-compressed",
            "application/x-compressed",
            "multipart/x-zip",
            "application/octet-stream"
        };

        private static readonly byte[][] ZipSignatures =
        {
            new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            new byte[] { 0x50, 0x4B, 0x05, 0x06 },
            new byte[] { 0x50, 0x4B, 0x07, 0x08 }
        };

        public static void EnsureValidScormPackageUpload(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                throw new InvalidScormPackageException("A SCORM package file is required.");
            }

            if (file.Length > ScormPackageLimits.MaxCompressedPackageBytes)
            {
                throw new InvalidScormPackageException(
                    $"SCORM package exceeds the maximum allowed size of {ScormPackageLimits.MaxCompressedPackageBytes / (1024 * 1024)} MB.");
            }

            var safeFileName = NormalizeUploadedFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName) ||
                !string.Equals(Path.GetExtension(safeFileName), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidScormPackageException("Only SCORM .zip packages are allowed.");
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                throw new InvalidScormPackageException("Only SCORM .zip packages are allowed.");
            }

            if (!HasZipSignature(file))
            {
                throw new InvalidScormPackageException("Uploaded file is not a valid ZIP archive.");
            }
        }

        public static string NormalizeUploadedFileName(string? fileName)
        {
            return Path.GetFileName(fileName ?? string.Empty).Trim();
        }

        private static bool HasZipSignature(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            Span<byte> header = stackalloc byte[4];
            var bytesRead = stream.Read(header);

            if (bytesRead < header.Length)
            {
                return false;
            }

            foreach (var signature in ZipSignatures)
            {
                if (header.SequenceEqual(signature))
                {
                    return true;
                }
            }

            return false;
        }
    }
}