using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAssignmentRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "AssignmentRules");

            migrationBuilder.RenameColumn(
                name: "Section",
                table: "AssignmentRules",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "AssignmentRules",
                newName: "AssignmentNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "AssignmentRules",
                newName: "Section");

            migrationBuilder.RenameColumn(
                name: "AssignmentNo",
                table: "AssignmentRules",
                newName: "Position");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AssignmentRules",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
