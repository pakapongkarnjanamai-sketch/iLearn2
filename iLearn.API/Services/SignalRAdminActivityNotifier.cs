using iLearn.API.Hubs;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace iLearn.API.Services
{
    public class SignalRAdminActivityNotifier : IAdminActivityRealtimeNotifier
    {
        private readonly IHubContext<AdminActivityHub> _hubContext;

        public SignalRAdminActivityNotifier(IHubContext<AdminActivityHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyCreatedAsync(AdminActivityDto activity)
        {
            return _hubContext.Clients.All.SendAsync("AdminActivityCreated", activity);
        }
    }
}
