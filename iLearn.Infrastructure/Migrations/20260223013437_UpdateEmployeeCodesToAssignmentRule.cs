using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployeeCodesToAssignmentRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeCodes",
                table: "AssignmentRules",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeCodes",
                table: "AssignmentRules");
        }
    }
}
