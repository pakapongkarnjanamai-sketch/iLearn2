using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteFilteredUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentId",
                table: "EnrollmentAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssignmentNo_CourseId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentCourses_AssignmentId_CourseId",
                table: "AssignmentCourses");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentId",
                table: "EnrollmentAssignments",
                columns: new[] { "EnrollmentId", "AssignmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentNo_CourseId",
                table: "Assignments",
                columns: new[] { "AssignmentNo", "CourseId" },
                unique: true,
                filter: "[AssignmentNo] IS NOT NULL AND [CourseId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCourses_AssignmentId_CourseId",
                table: "AssignmentCourses",
                columns: new[] { "AssignmentId", "CourseId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentId",
                table: "EnrollmentAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_AssignmentNo_CourseId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentCourses_AssignmentId_CourseId",
                table: "AssignmentCourses");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentId",
                table: "EnrollmentAssignments",
                columns: new[] { "EnrollmentId", "AssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentNo_CourseId",
                table: "Assignments",
                columns: new[] { "AssignmentNo", "CourseId" },
                unique: true,
                filter: "[AssignmentNo] IS NOT NULL AND [CourseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCourses_AssignmentId_CourseId",
                table: "AssignmentCourses",
                columns: new[] { "AssignmentId", "CourseId" },
                unique: true);
        }
    }
}
