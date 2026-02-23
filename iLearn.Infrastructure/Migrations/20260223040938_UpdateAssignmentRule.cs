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
                table: "Assignments");

            migrationBuilder.RenameColumn(
                name: "Section",
                table: "Assignments",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Position",
                table: "Assignments",
                newName: "AssignmentNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Assignments",
                newName: "Section");

            migrationBuilder.RenameColumn(
                name: "AssignmentNo",
                table: "Assignments",
                newName: "Position");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
