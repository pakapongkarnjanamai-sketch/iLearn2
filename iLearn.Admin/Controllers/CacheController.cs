using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace iLearn.Admin.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class CacheController : Controller
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<CacheController> logger)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ClearAll()
        {
            if (_cache is not MemoryCache memoryCache)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "The Admin cache provider does not support clearing all entries."
                });
            }

            memoryCache.Compact(1.0);

            try
            {
                var client = _httpClientFactory.CreateClient("iLearnAPI");
                using var response = await client.PostAsync("admin/cache/clear-all", content: null);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ClearAll cache API call failed with status {StatusCode}. Response: {Response}",
                        response.StatusCode,
                        responseContent);

                    return StatusCode(502, new
                    {
                        success = false,
                        message = "Admin cache cleared, but API cache could not be cleared."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "All Admin and API cached data cleared."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing API cache from Admin.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Admin cache cleared, but API cache could not be cleared."
                });
            }
        }
    }
}