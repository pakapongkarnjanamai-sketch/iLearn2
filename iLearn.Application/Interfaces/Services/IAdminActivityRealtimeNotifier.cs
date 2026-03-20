using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IAdminActivityRealtimeNotifier
    {
        Task NotifyCreatedAsync(AdminActivityDto activity);
    }
}
