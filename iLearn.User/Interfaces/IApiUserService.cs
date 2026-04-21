using iLearn.Application.DTOs;
using iLearn.Domain.Common;

namespace iLearn.User.Interfaces
{
    public interface IApiUserService
    {
        Task<ApiResponse<UserDto>> GetOrCreateUserAsync(string windowsIdentity);
    }
}
