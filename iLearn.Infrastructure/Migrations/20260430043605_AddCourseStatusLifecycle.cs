using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE [Courses]
SET [Status] = CASE
    WHEN [IsActive] = 1 THEN 1
    WHEN EXISTS (
        SELECT 1
        FROM [Enrollments] AS [e]
        WHERE [e].[CourseId] = [Courses].[Id]
          AND [e].[IsDeleted] = 0
    ) THEN 2
    ELSE 0
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");
        }
    }
}
