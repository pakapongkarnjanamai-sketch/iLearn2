using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCourseTypeEnumToTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.RenameColumn(
        name: "Type",
        table: "Courses",
        newName: "CourseTypeId");

            migrationBuilder.CreateTable(
                name: "CourseTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CourseTypes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "วิชาเฉพาะทาง (Rule-based)", true, "Special", null, null },
                    { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "วิชาทั่วไป (Auto-assign)", true, "General", null, null }
                });

            // ภายใน method Up() — เพิ่มก่อนบรรทัดที่สร้าง FK constraint
            // Migrate ค่า enum เดิม (0→Special, 1→General) → table Id (1→Special, 2→General)
            migrationBuilder.Sql("UPDATE Courses SET CourseTypeId = CASE WHEN CourseTypeId = 0 THEN 1 WHEN CourseTypeId = 1 THEN 2 ELSE 1 END");
           
       

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseTypeId",
                table: "Courses",
                column: "CourseTypeId");

        

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_CourseTypes_CourseTypeId",
                table: "Courses",
                column: "CourseTypeId",
                principalTable: "CourseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_CourseTypes_CourseTypeId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "CourseTypes");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseTypeId",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "CourseTypeId",
                table: "Courses",
                newName: "Type");
        }
    }
}
