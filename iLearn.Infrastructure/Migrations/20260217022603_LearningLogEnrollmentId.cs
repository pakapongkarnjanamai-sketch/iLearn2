using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LearningLogEnrollmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnrollmentId",
                table: "LearningLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LearningLogs_EnrollmentId",
                table: "LearningLogs",
                column: "EnrollmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningLogs_Enrollments_EnrollmentId",
                table: "LearningLogs",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningLogs_Enrollments_EnrollmentId",
                table: "LearningLogs");

            migrationBuilder.DropIndex(
                name: "IX_LearningLogs_EnrollmentId",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "LearningLogs");
        }
    }
}
