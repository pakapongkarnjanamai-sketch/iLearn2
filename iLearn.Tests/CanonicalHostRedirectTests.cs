extern alias ILearnUserApp;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ILearnUserApp::iLearn.User.Middleware;
using Xunit;

namespace iLearn.Tests
{
    public sealed class CanonicalHostRedirectTests
    {
        [Theory]
        [InlineData("https://ap-ntc2138-qawb.nikonoa.net/iLearn", "GET", "ap-ntc2138-qawb", "/iLearn", "/health/smoke", "?key=123", "https://ap-ntc2138-qawb.nikonoa.net/iLearn/health/smoke?key=123")]
        [InlineData("https://ap-ntc2137-prwb.nikonoa.net/iLearn", "GET", "ap-ntc2137-prwb", "/iLearn", "/Courses/scorm/index.html", "", "https://ap-ntc2137-prwb.nikonoa.net/iLearn/Courses/scorm/index.html")]
        [InlineData("http://ap-ntc2138-qawb.nikonoa.net:8080/iLearn", "GET", "ap-ntc2138-qawb", "/iLearn", "/home", "?a=1&b=2", "http://ap-ntc2138-qawb.nikonoa.net:8080/iLearn/home?a=1&b=2")]
        public void TryGetCanonicalRedirect_ShortHost_ReturnsRedirectUrl(
            string hostUrlConfig,
            string method,
            string requestHost,
            string pathBase,
            string path,
            string queryString,
            string expectedRedirectUrl)
        {
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                method,
                requestHost,
                pathBase,
                path,
                queryString,
                out var redirectUrl);

            Assert.True(success);
            Assert.Equal(expectedRedirectUrl, redirectUrl);
        }

        [Theory]
        [InlineData("ap-ntc2138-qawb.nikonoa.net")]
        [InlineData("AP-NTC2138-QAWB.NIKONOA.NET")]
        public void TryGetCanonicalRedirect_CanonicalHost_ReturnsFalse(string requestHost)
        {
            var hostUrlConfig = "https://ap-ntc2138-qawb.nikonoa.net/iLearn";
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                "GET",
                requestHost,
                "/iLearn",
                "/health/smoke",
                "",
                out var redirectUrl);

            Assert.False(success);
            Assert.Null(redirectUrl);
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("127.0.0.1")]
        [InlineData("127.0.0.2")]
        [InlineData("::1")]
        [InlineData("[::1]")]
        public void TryGetCanonicalRedirect_LocalhostRequest_ReturnsFalse(string requestHost)
        {
            var hostUrlConfig = "https://ap-ntc2138-qawb.nikonoa.net/iLearn";
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                "GET",
                requestHost,
                "/iLearn",
                "/health/smoke",
                "",
                out var redirectUrl);

            Assert.False(success);
            Assert.Null(redirectUrl);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public void TryGetCanonicalRedirect_NonGetHeadMethods_ReturnsFalse(string method)
        {
            var hostUrlConfig = "https://ap-ntc2138-qawb.nikonoa.net/iLearn";
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                method,
                "ap-ntc2138-qawb",
                "/iLearn",
                "/api/submit",
                "",
                out var redirectUrl);

            Assert.False(success);
            Assert.Null(redirectUrl);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test name convention")]
        public void TryGetCanonicalRedirect_HeadMethod_Redirects()
        {
            var hostUrlConfig = "https://ap-ntc2138-qawb.nikonoa.net/iLearn";
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                "HEAD",
                "ap-ntc2138-qawb",
                "/iLearn",
                "/health/smoke",
                "",
                out var redirectUrl);

            Assert.True(success);
            Assert.Equal("https://ap-ntc2138-qawb.nikonoa.net/iLearn/health/smoke", redirectUrl);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-valid-url")]
        [InlineData("/relative/path")]
        public void TryGetCanonicalRedirect_HostUrlEmptyOrInvalid_ReturnsFalse(string? hostUrlConfig)
        {
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                "GET",
                "ap-ntc2138-qawb",
                "/iLearn",
                "/health/smoke",
                "",
                out var redirectUrl);

            Assert.False(success);
            Assert.Null(redirectUrl);
        }

        [Theory]
        [InlineData("http://localhost/iLearn")]
        [InlineData("https://127.0.0.1/iLearn")]
        public void TryGetCanonicalRedirect_CanonicalHostIsLocalhost_ReturnsFalse(string hostUrlConfig)
        {
            var success = CanonicalHostRedirectHelper.TryGetCanonicalRedirect(
                hostUrlConfig,
                "GET",
                "ap-ntc2138-qawb",
                "/iLearn",
                "/health/smoke",
                "",
                out var redirectUrl);

            Assert.False(success);
            Assert.Null(redirectUrl);
        }

        [Fact]
        public async Task Middleware_InvokeAsync_ShortHost_Sets307AndLocationHeader()
        {
            // Arrange
            var calledNext = false;
            RequestDelegate next = (context) =>
            {
                calledNext = true;
                return Task.CompletedTask;
            };

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "FileSettings:HostUrl", "https://ap-ntc2138-qawb.nikonoa.net/iLearn" }
            };
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var middleware = new CanonicalHostRedirectMiddleware(next, config);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Host = new HostString("ap-ntc2138-qawb");
            httpContext.Request.PathBase = new PathString("/iLearn");
            httpContext.Request.Path = new PathString("/health/smoke");

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.False(calledNext);
            Assert.Equal(StatusCodes.Status307TemporaryRedirect, httpContext.Response.StatusCode);
            Assert.Equal("https://ap-ntc2138-qawb.nikonoa.net/iLearn/health/smoke", httpContext.Response.Headers.Location);
        }

        [Fact]
        public async Task Middleware_InvokeAsync_CanonicalHost_CallsNextDelegate()
        {
            // Arrange
            var calledNext = false;
            RequestDelegate next = (context) =>
            {
                calledNext = true;
                return Task.CompletedTask;
            };

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "FileSettings:HostUrl", "https://ap-ntc2138-qawb.nikonoa.net/iLearn" }
            };
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var middleware = new CanonicalHostRedirectMiddleware(next, config);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Host = new HostString("ap-ntc2138-qawb.nikonoa.net");
            httpContext.Request.PathBase = new PathString("/iLearn");
            httpContext.Request.Path = new PathString("/health/smoke");

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            Assert.True(calledNext);
            Assert.NotEqual(StatusCodes.Status307TemporaryRedirect, httpContext.Response.StatusCode);
        }
    }
}
