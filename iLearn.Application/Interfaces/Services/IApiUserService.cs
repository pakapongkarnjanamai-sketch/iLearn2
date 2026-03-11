using iLearn.Application.DTOs;
using iLearn.Domain.Common;

namespace iLearn.Application.Interfaces.Services
{
    public interface IApiUserService
    {
        Task<ApiResponse<UserDto>> GetOrCreateUserAsync(string windowsIdentity, bool forceRefresh = false);
    }
}
