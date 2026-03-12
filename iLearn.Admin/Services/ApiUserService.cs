using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace iLearn.Admin.Services
{
    public class ApiUserService : IApiUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiUserService> _logger;
        private readonly IMemoryCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiUserService(IHttpClientFactory httpClientFactory, ILogger<ApiUserService> logger, IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("iLearnAPI");
            _logger = logger;
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null
            };
        }

        public async Task<ApiResponse<UserDto>> GetOrCreateUserAsync(string windowsIdentity, bool forceRefresh = false)
        {
            try
            {
                var cacheKey = $"api_user_{windowsIdentity}";

                if (!forceRefresh && _cache.TryGetValue(cacheKey, out ApiResponse<UserDto>? cachedUser))
                    return cachedUser!;

                var request = new CreateUserRequest { WindowsIdentity = windowsIdentity };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("users/windows-auth", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseContent, _jsonOptions);

                    if (result?.Success == true)
                        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

                    return result ?? new ApiResponse<UserDto> { Success = false, Message = "Invalid response" };
                }

                _logger.LogError("Failed to get/create user. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);

                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"API call failed: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling API to get/create user: {WindowsIdentity}", windowsIdentity);
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while processing the request"
                };
            }
        }
    }
}
