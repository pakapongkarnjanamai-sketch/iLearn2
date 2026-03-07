using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentAssignmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "EnrollmentAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    AssignmentRuleId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentAssignments_Assignments_AssignmentRuleId",
                        column: x => x.AssignmentRuleId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentAssignments_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentAssignments_AssignmentRuleId",
                table: "EnrollmentAssignments",
                column: "AssignmentRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentAssignments_EnrollmentId_AssignmentRuleId",
                table: "EnrollmentAssignments",
                columns: new[] { "EnrollmentId", "AssignmentRuleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnrollmentAssignments");

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
                name: "FK_Enrollments_Assignments_AssignmentRuleId",
                table: "Enrollments",
                column: "AssignmentRuleId",
                principalTable: "Assignments",
                principalColumn: "Id");
        }
    }
}
