using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentRuleToEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentRuleId",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_AssignmentRuleId",
                table: "Enrollments",
                column: "AssignmentRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_AssignmentRules_AssignmentRuleId",
                table: "Enrollments",
                column: "AssignmentRuleId",
                principalTable: "Assignments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_AssignmentRules_AssignmentRuleId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_AssignmentRuleId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "AssignmentRuleId",
                table: "Enrollments");
        }
    }
}
