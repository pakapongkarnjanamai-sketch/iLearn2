using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAssignmentRuleIdToAssignmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentAssignments_Assignments_AssignmentRuleId",
                table: "EnrollmentAssignments");

            migrationBuilder.RenameColumn(
                name: "AssignmentRuleId",
                table: "EnrollmentAssignments",
                newName: "AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentRuleId",
                table: "EnrollmentAssignments",
                newName: "IX_EnrollmentAssignments_EnrollmentId_AssignmentId");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentAssignments_AssignmentRuleId",
                table: "EnrollmentAssignments",
                newName: "IX_EnrollmentAssignments_AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentAssignments_Assignments_AssignmentId",
                table: "EnrollmentAssignments",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EnrollmentAssignments_Assignments_AssignmentId",
                table: "EnrollmentAssignments");

            migrationBuilder.RenameColumn(
                name: "AssignmentId",
                table: "EnrollmentAssignments",
                newName: "AssignmentRuleId");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentId",
                table: "EnrollmentAssignments",
                newName: "IX_EnrollmentAssignments_EnrollmentId_AssignmentRuleId");

            migrationBuilder.RenameIndex(
                name: "IX_EnrollmentAssignments_AssignmentId",
                table: "EnrollmentAssignments",
                newName: "IX_EnrollmentAssignments_AssignmentRuleId");

            migrationBuilder.AddForeignKey(
                name: "FK_EnrollmentAssignments_Assignments_AssignmentRuleId",
                table: "EnrollmentAssignments",
                column: "AssignmentRuleId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
