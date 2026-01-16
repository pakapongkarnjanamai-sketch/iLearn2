using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseResources_Courses_CourseId",
                table: "CourseResources");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseResources_Resources_ResourceId",
                table: "CourseResources");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "CourseResources",
                newName: "CourseVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseResources_CourseId",
                table: "CourseResources",
                newName: "IX_CourseResources_CourseVersionId");

            migrationBuilder.CreateTable(
                name: "CourseVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseVersions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseVersions_CourseId",
                table: "CourseVersions",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseResources_CourseVersions_CourseVersionId",
                table: "CourseResources",
                column: "CourseVersionId",
                principalTable: "CourseVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseResources_Resources_ResourceId",
                table: "CourseResources",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseResources_CourseVersions_CourseVersionId",
                table: "CourseResources");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseResources_Resources_ResourceId",
                table: "CourseResources");

            migrationBuilder.DropTable(
                name: "CourseVersions");

            migrationBuilder.RenameColumn(
                name: "CourseVersionId",
                table: "CourseResources",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_CourseResources_CourseVersionId",
                table: "CourseResources",
                newName: "IX_CourseResources_CourseId");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseResources_Courses_CourseId",
                table: "CourseResources",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseResources_Resources_ResourceId",
                table: "CourseResources",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
