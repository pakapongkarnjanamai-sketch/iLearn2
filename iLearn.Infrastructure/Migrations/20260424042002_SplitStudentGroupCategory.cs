using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitStudentGroupCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_StudentGroups_ParentId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_Path",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "StudentGroups");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "StudentGroups",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroups_ParentId",
                table: "StudentGroups",
                newName: "IX_StudentGroups_CategoryId");

            migrationBuilder.CreateTable(
                name: "StudentGroupCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DivisionId = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Depth = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StudentGroupCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGroupCategories_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentGroupCategories_StudentGroupCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "StudentGroupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupCategories_DivisionId",
                table: "StudentGroupCategories",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupCategories_ParentId",
                table: "StudentGroupCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupCategories_Path",
                table: "StudentGroupCategories",
                column: "Path");

            // === Data migration ===
            // Promote any StudentGroup that has children (referenced as a parent) AND has
            // no members AND no assignments into a StudentGroupCategory. Preserve Id so
            // existing CategoryId references (formerly ParentId) keep pointing at the
            // correct row. Children whose parent was NOT promoted will have CategoryId
            // nulled out and become root groups.
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT StudentGroupCategories ON;

INSERT INTO StudentGroupCategories (Id, Name, Description, DivisionId, ParentId, Path, Depth, IsActive, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, DeletedAt, DeletedBy)
SELECT sg.Id, sg.Name, sg.Description, sg.DivisionId, sg.CategoryId, NULL, 0,
       sg.IsActive, sg.CreatedAt, sg.UpdatedAt, sg.CreatedBy, sg.UpdatedBy,
       sg.IsDeleted, sg.DeletedAt, sg.DeletedBy
FROM StudentGroups sg
WHERE sg.Id IN (SELECT DISTINCT CategoryId FROM StudentGroups WHERE CategoryId IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM StudentGroupMembers m WHERE m.StudentGroupId = sg.Id)
  AND NOT EXISTS (SELECT 1 FROM Assignments a WHERE a.StudentGroupId = sg.Id);

SET IDENTITY_INSERT StudentGroupCategories OFF;

-- NULL ParentId on categories whose parent wasn't promoted
UPDATE c SET ParentId = NULL
FROM StudentGroupCategories c
WHERE c.ParentId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM StudentGroupCategories p WHERE p.Id = c.ParentId);

-- Recompute Path/Depth on the new category tree
WITH cte AS (
    SELECT Id, ParentId, CAST('/' + CAST(Id AS VARCHAR(20)) + '/' AS NVARCHAR(450)) AS NewPath, 0 AS NewDepth
    FROM StudentGroupCategories WHERE ParentId IS NULL
    UNION ALL
    SELECT c.Id, c.ParentId, CAST(p.NewPath + CAST(c.Id AS VARCHAR(20)) + '/' AS NVARCHAR(450)), p.NewDepth + 1
    FROM StudentGroupCategories c
    INNER JOIN cte p ON c.ParentId = p.Id
)
UPDATE sgc SET Path = cte.NewPath, Depth = cte.NewDepth
FROM StudentGroupCategories sgc
INNER JOIN cte ON cte.Id = sgc.Id;

-- Remove promoted rows from StudentGroups (they now exist as categories)
DELETE FROM StudentGroups WHERE Id IN (SELECT Id FROM StudentGroupCategories);

-- NULL CategoryId for remaining StudentGroups whose target wasn't promoted
UPDATE sg SET CategoryId = NULL
FROM StudentGroups sg
WHERE sg.CategoryId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM StudentGroupCategories c WHERE c.Id = sg.CategoryId);
");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_StudentGroupCategories_CategoryId",
                table: "StudentGroups",
                column: "CategoryId",
                principalTable: "StudentGroupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_StudentGroupCategories_CategoryId",
                table: "StudentGroups");

            migrationBuilder.DropTable(
                name: "StudentGroupCategories");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "StudentGroups",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentGroups_CategoryId",
                table: "StudentGroups",
                newName: "IX_StudentGroups_ParentId");

            migrationBuilder.AddColumn<int>(
                name: "Depth",
                table: "StudentGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "StudentGroups",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

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
    }
}
