using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUser;

        public NotificationsController(
            INotificationService notificationService,
            ICurrentUserService currentUser)
        {
            _notificationService = notificationService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int take = 20)
        {
            var result = await _notificationService.GetForUserAsync(_currentUser.UserId, unreadOnly, take);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync(_currentUser.UserId);
            return Ok(new { success = true, data = new { unreadCount = count } });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            try
            {
                var unreadCount = await _notificationService.MarkReadAsync(_currentUser.UserId, id);
                return Ok(new { success = true, data = new { unreadCount } });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "Notification not found." });
            }
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var unreadCount = await _notificationService.MarkAllReadAsync(_currentUser.UserId);
            return Ok(new { success = true, data = new { unreadCount } });
        }
    }
}
