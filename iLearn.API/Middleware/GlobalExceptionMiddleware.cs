using System.Text.Json;
using iLearn.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Middleware
{
    /// <summary>
    /// Global exception handler that converts thrown exceptions into a
    /// <see cref="ProblemDetails"/> response. Replaces the per-action
    /// <c>try/catch + StatusCode(500, ex.Message)</c> blocks that previously
    /// leaked exception details to API clients.
    /// </summary>
    public sealed class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteProblemDetailsAsync(context, ex);
            }
        }

        private async Task WriteProblemDetailsAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "Exception thrown after the response started; cannot rewrite as ProblemDetails.");
                throw ex;
            }

            var (status, title) = MapException(ex);

            // Sanitize request method/path before logging to neutralize potential
            // log-forging via CR/LF or other control characters in the URL or
            // (extremely unlikely) the verb.
            var safeMethod = SanitizeForLog(context.Request.Method);
            var safePath   = SanitizeForLog(context.Request.Path.Value);

            // Always log full exception server-side; never include in client payload outside Development.
            if (status >= 500)
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", safeMethod, safePath);
            else
                _logger.LogWarning(ex, "Handled exception processing {Method} {Path}", safeMethod, safePath);

            var problem = new ProblemDetails
            {
                Status   = status,
                Title    = title,
                Detail   = ex.Message,
                Instance = context.Request.Path
            };

            if (_env.IsDevelopment())
            {
                problem.Extensions["exception"] = ex.GetType().FullName;
                problem.Extensions["stackTrace"] = ex.StackTrace;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                problem,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        private static (int Status, string Title) MapException(Exception ex) => ex switch
        {
            KeyNotFoundException             => (StatusCodes.Status404NotFound,  "ContentItem not found."),
            UnauthorizedAccessException      => (StatusCodes.Status403Forbidden, "Forbidden."),
            InvalidScormPackageException     => (StatusCodes.Status400BadRequest, "Invalid SCORM package."),
            ArgumentException                => (StatusCodes.Status400BadRequest, "Invalid request."),
            InvalidOperationException        => (StatusCodes.Status409Conflict,   "Operation not allowed."),
            OperationCanceledException       => (499 /* client closed request */, "Request cancelled."),
            System.Net.Http.HttpRequestException => (StatusCodes.Status502BadGateway, "Upstream employee service error."),
            _                                => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        private static string SanitizeForLog(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Strip CR/LF/tab and other control characters that could be used to
            // forge fake log entries (CWE-117).
            var buffer = new char[value.Length];
            var written = 0;
            foreach (var ch in value)
            {
                if (ch == '\r' || ch == '\n' || char.IsControl(ch))
                    continue;
                buffer[written++] = ch;
            }
            return new string(buffer, 0, written);
        }
    }
}
