using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace iLearn.API.Hubs
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminActivityHub : Hub
    {
    }
}
