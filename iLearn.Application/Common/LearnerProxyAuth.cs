using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace iLearn.Application.Common
{
    public sealed class LearnerProxyAuthOptions
    {
        public const string SectionName = "LearnerProxyAuth";

        public string SharedSecret { get; set; } = string.Empty;

        public int TimestampToleranceSeconds { get; set; } = 300;
    }

    public static class LearnerProxyAuthHeaders
    {
        public const string StudentCode = "X-iLearn-Learner-Code";
        public const string Timestamp = "X-iLearn-Learner-Timestamp";
        public const string Signature = "X-iLearn-Learner-Signature";
    }

    public static class LearnerProxyAuthSignature
    {
        public static string CreateTimestamp(DateTimeOffset utcNow) =>
            utcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        public static string Compute(
            string sharedSecret,
            string studentCode,
            string timestamp,
            string method,
            string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(sharedSecret))
                throw new InvalidOperationException("Learner proxy shared secret is not configured.");

            var payload = string.Join('\n',
                studentCode.Trim(),
                timestamp.Trim(),
                method.Trim().ToUpperInvariant(),
                NormalizeAbsolutePath(absolutePath));

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        public static string NormalizeAbsolutePath(string? absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
                return "/";

            return absolutePath.Trim();
        }
    }
}