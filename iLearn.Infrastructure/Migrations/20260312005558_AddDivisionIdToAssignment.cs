using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionIdToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "StudentGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_DivisionId",
                table: "StudentGroups",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DivisionId",
                table: "Assignments",
                column: "DivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_Divisions_DivisionId",
                table: "Assignments",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_Divisions_DivisionId",
                table: "StudentGroups",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_Divisions_DivisionId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_Divisions_DivisionId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_DivisionId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_DivisionId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Assignments");
        }
    }
}
