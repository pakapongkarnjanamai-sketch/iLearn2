using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace iLearn.Admin.Services
{
    public class ApiUserService : IApiUserService
    {
        private const string ClientName = "iLearnAPI";
        private const string WindowsAuthEndpoint = "users/windows-auth";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiUserService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiUserService(
            IHttpClientFactory httpClientFactory,
            ILogger<ApiUserService> logger,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClientFactory.CreateClient(ClientName);
            _logger = logger;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };
        }

        public async Task<ApiResponse<UserDto>> GetOrCreateUserAsync(string windowsIdentity, bool forceRefresh = false)
        {
            var cacheKey = GetCacheKey(windowsIdentity);

            if (!forceRefresh && TryGetCachedUser(cacheKey, out var cachedUser))
            {
                return cachedUser;
            }

            try
            {
                var request = new CreateUserRequest { WindowsIdentity = windowsIdentity };
                var requestUri = GetRequestUri(forceRefresh);

                using var response = await PostAsCurrentWindowsIdentityAsync(requestUri, request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseContent, _jsonOptions);

                    if (result?.Success == true)
                    {
                        _cache.Set(cacheKey, result, CacheDuration);
                    }

                    return result ?? CreateFailureResponse("Invalid response");
                }

                _logger.LogError("Failed to get/create user. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                return CreateFailureResponse($"API call failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API to get/create user: {WindowsIdentity}", windowsIdentity);
                return CreateFailureResponse("An error occurred while processing the request");
            }
        }

        private bool TryGetCachedUser(string cacheKey, out ApiResponse<UserDto> cachedUser)
        {
            if (_cache.TryGetValue(cacheKey, out ApiResponse<UserDto>? cachedValue) && cachedValue != null)
            {
                cachedUser = cachedValue;
                return true;
            }

            cachedUser = null!;
            return false;
        }

        private StringContent CreateRequestContent(CreateUserRequest request)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private async Task<HttpResponseMessage> PostAsCurrentWindowsIdentityAsync(string requestUri, CreateUserRequest request)
        {
            var windowsIdentity = _httpContextAccessor.HttpContext?.User.Identity as WindowsIdentity;
            if (OperatingSystem.IsWindows() && windowsIdentity != null)
            {
                var accessToken = windowsIdentity.AccessToken;
                if (!accessToken.IsInvalid)
                {
                    // Run impersonated operation on a thread pool thread to avoid blocking the async context
                    return await Task.Run(() =>
                        WindowsIdentity.RunImpersonated(accessToken, async () =>
                        {
                            using var content = CreateRequestContent(request);
                            return await _httpClient.PostAsync(requestUri, content);
                        })
                    );
                }
            }

            using var fallbackContent = CreateRequestContent(request);
            return await _httpClient.PostAsync(requestUri, fallbackContent);
        }

        private static string GetCacheKey(string windowsIdentity) => $"api_user_{windowsIdentity}";

        private static string GetRequestUri(bool forceRefresh)
            => forceRefresh ? $"{WindowsAuthEndpoint}?_refresh=1" : WindowsAuthEndpoint;

        private static ApiResponse<UserDto> CreateFailureResponse(string message)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = message
            };
        }
    }
}
