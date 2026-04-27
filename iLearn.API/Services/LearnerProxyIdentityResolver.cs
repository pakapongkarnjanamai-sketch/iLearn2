using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using iLearn.Application.Common;
using Microsoft.Extensions.Options;

namespace iLearn.API.Services
{
    public interface ILearnerProxyIdentityResolver
    {
        bool TryResolveStudentCode(HttpContext context, out string studentCode, out int statusCode, out string errorMessage);
    }

    public sealed class LearnerProxyIdentityResolver : ILearnerProxyIdentityResolver
    {
        private readonly LearnerProxyAuthOptions _options;
        private readonly ILogger<LearnerProxyIdentityResolver> _logger;

        public LearnerProxyIdentityResolver(
            IOptions<LearnerProxyAuthOptions> options,
            ILogger<LearnerProxyIdentityResolver> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public bool TryResolveStudentCode(HttpContext context, out string studentCode, out int statusCode, out string errorMessage)
        {
            studentCode = string.Empty;

            if (string.IsNullOrWhiteSpace(_options.SharedSecret))
            {
                statusCode = StatusCodes.Status503ServiceUnavailable;
                errorMessage = "Learner proxy authentication is not configured.";
                return false;
            }

            if (!TryGetSingleHeader(context, LearnerProxyAuthHeaders.StudentCode, out var requestedStudentCode) ||
                string.IsNullOrWhiteSpace(requestedStudentCode))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorMessage = "Missing learner identity header.";
                return false;
            }

            if (!TryGetSingleHeader(context, LearnerProxyAuthHeaders.Timestamp, out var timestampValue) ||
                !long.TryParse(timestampValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestampSeconds))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorMessage = "Missing or invalid learner proxy timestamp.";
                return false;
            }

            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var maxSkew = Math.Max(30, _options.TimestampToleranceSeconds);
            if (Math.Abs(nowSeconds - timestampSeconds) > maxSkew)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                errorMessage = "Learner proxy timestamp expired.";
                return false;
            }

            if (!TryGetSingleHeader(context, LearnerProxyAuthHeaders.Signature, out var actualSignature) ||
                string.IsNullOrWhiteSpace(actualSignature))
            {
                statusCode = StatusCodes.Status400BadRequest;
                errorMessage = "Missing learner proxy signature.";
                return false;
            }

            var signedPath = LearnerProxyAuthSignature.NormalizeAbsolutePath($"{context.Request.PathBase}{context.Request.Path}");
            var expectedSignature = LearnerProxyAuthSignature.Compute(
                _options.SharedSecret,
                requestedStudentCode,
                timestampValue,
                context.Request.Method,
                signedPath);

            var actualBytes = Encoding.UTF8.GetBytes(actualSignature.Trim());
            var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            if (actualBytes.Length != expectedBytes.Length ||
                !CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
            {
                _logger.LogWarning(
                    "Rejected learner proxy request for {Path}. Caller={Caller}",
                    signedPath,
                    context.User.Identity?.Name ?? "(anonymous)");

                statusCode = StatusCodes.Status401Unauthorized;
                errorMessage = "Invalid learner proxy signature.";
                return false;
            }

            studentCode = requestedStudentCode.Trim();
            statusCode = StatusCodes.Status200OK;
            errorMessage = string.Empty;
            return true;
        }

        private static bool TryGetSingleHeader(HttpContext context, string headerName, out string value)
        {
            value = string.Empty;

            if (!context.Request.Headers.TryGetValue(headerName, out var values) || values.Count != 1)
                return false;

            value = values[0]?.Trim() ?? string.Empty;
            return true;
        }
    }
}