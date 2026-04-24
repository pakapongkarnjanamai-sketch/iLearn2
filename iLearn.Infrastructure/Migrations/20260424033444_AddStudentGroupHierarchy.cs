using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentGroupHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Depth",
                table: "StudentGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "StudentGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "StudentGroups",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_ParentId",
                table: "StudentGroups",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_Path",
                table: "StudentGroups",
                column: "Path");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_StudentGroups_ParentId",
                table: "StudentGroups",
                column: "ParentId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_StudentGroups_ParentId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_ParentId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_Path",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "StudentGroups");
        }
    }
}
