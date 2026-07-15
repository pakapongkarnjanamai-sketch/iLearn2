using Microsoft.AspNetCore.SignalR;

namespace iLearn.API.Services
{
    /// <summary>
    /// Maps SignalR connection user identity to NID (strips domain prefix).
    /// Must produce the same value as <see cref="iLearn.Infrastructure.Services.CurrentUserService.UserId"/>.
    /// </summary>
    public class NidUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var identity = connection.User?.Identity;
            if (identity == null || !identity.IsAuthenticated)
                return null;

            var fullName = identity.Name;
            if (string.IsNullOrEmpty(fullName))
                return null;

            // Strip domain prefix: "DOMAIN\nid" → "nid"
            // Must match CurrentUserService.UserId normalization exactly
            var backslashIndex = fullName.LastIndexOf('\\');
            return backslashIndex >= 0 ? fullName[(backslashIndex + 1)..] : fullName;
        }
    }
}
