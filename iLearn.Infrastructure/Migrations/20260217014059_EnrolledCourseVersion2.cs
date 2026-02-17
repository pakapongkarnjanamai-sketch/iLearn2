using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrolledCourseVersion2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // เปลี่ยนชื่อคอลัมน์จาก EnrolledVersion เป็น EnrolledCourseVersion
            migrationBuilder.RenameColumn(
                name: "EnrolledVersion", // ชื่อคอลัมน์เดิม
                table: "Enrollments",    // ชื่อของตาราง
                newName: "EnrolledCourseVersion" // ชื่อใหม่
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ย้อนกลับจาก EnrolledCourseVersion เป็น EnrolledVersion
            migrationBuilder.RenameColumn(
                name: "EnrolledCourseVersion", // ชื่อคอลัมน์ใหม่
                table: "Enrollments",         // ชื่อของตาราง
                newName: "EnrolledVersion"     // ชื่อเดิม
            );
        }
    }
}
