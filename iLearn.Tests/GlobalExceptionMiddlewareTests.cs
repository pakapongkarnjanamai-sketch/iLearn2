using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using iLearn.API.Middleware;
using Xunit;

namespace iLearn.Tests
{
    public sealed class GlobalExceptionMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_HttpRequestException_Returns502ProblemDetails()
        {
            // Arrange
            RequestDelegate next = (context) => throw new HttpRequestException("Mock connection error");
            var logger = NullLogger<GlobalExceptionMiddleware>.Instance;
            var env = new FakeHostEnvironment { EnvironmentName = "Production" };
            
            var middleware = new GlobalExceptionMiddleware(next, logger, env);
            
            var httpContext = new DefaultHttpContext();
            var responseStream = new MemoryStream();
            httpContext.Response.Body = responseStream;

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.Equal(StatusCodes.Status502BadGateway, httpContext.Response.StatusCode);
            Assert.Equal("application/problem+json", httpContext.Response.ContentType);

            responseStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();

            var problem = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            Assert.NotNull(problem);
            Assert.Equal(StatusCodes.Status502BadGateway, problem.Status);
            Assert.Equal("Upstream employee service error.", problem.Title);
            Assert.Equal("Mock connection error", problem.Detail);
        }

        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Production";
            public string ApplicationName { get; set; } = "iLearn.API";
            public string ContentRootPath { get; set; } = "";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        }
    }
}
