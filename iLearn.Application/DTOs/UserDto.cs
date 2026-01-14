namespace iLearn.Application.DTOs
{
    // สำหรับส่งข้อมูลกลับ (Response)
    //public class UserDto
    //{
    //    public int Id { get; set; }
    //    public string Nid { get; set; } = string.Empty; // รหัสพนักงาน/นักเรียน

    //    // อาจเพิ่ม RoleName list ในอนาคต
    //}
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
        public List<RoleDto>? Roles { get; set; } = new();
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
    // สำหรับรับข้อมูลสร้างใหม่ (Request)
    public class CreateUserDto
    {
        public string Nid { get; set; } = string.Empty;

    }
}