using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/admin/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheController> _logger;

        public CacheController(IMemoryCache cache, ILogger<CacheController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        [HttpPost("clear-all")]
        public IActionResult ClearAll()
        {
            if (_cache is not MemoryCache memoryCache)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "The API cache provider does not support clearing all entries."
                });
            }

            memoryCache.Compact(1.0);

            _logger.LogInformation(
                "All API memory cache cleared by {Identity}",
                User.Identity?.Name ?? "(unknown)");

            return Ok(new
            {
                success = true,
                message = "All API cached data cleared."
            });
        }
    }
}