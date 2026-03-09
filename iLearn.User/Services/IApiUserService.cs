namespace iLearn.User.Services
{
    public class UserDto
    {
        public int Id { get; set; }
        public string NID { get; set; } = string.Empty;
        public string EmployeeID { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime LastLogin { get; set; }
        public List<RoleDto> Roles { get; set; } = new();
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class CreateUserRequest
    {
        public string WindowsIdentity { get; set; } = string.Empty;
    }

    public interface IApiUserService
    {
        Task<ApiResponse<UserDto>> GetOrCreateUserAsync(string windowsIdentity);
    }
}
