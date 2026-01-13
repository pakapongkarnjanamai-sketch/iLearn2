namespace iLearn.Application.DTOs
{
    // สำหรับส่งข้อมูลกลับ (Response)
    public class UserDto
    {
        public int Id { get; set; }
        public string Nid { get; set; } = string.Empty; // รหัสพนักงาน/นักเรียน

        // อาจเพิ่ม RoleName list ในอนาคต
    }

    // สำหรับรับข้อมูลสร้างใหม่ (Request)
    public class CreateUserDto
    {
        public string Nid { get; set; } = string.Empty;

    }
}