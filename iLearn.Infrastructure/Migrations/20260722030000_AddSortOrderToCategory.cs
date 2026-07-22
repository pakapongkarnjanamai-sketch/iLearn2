using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 1) Backfill running number per Division, ordered by Id (NULL DivisionId is its own group).
            //    Only active (non-deleted) rows are numbered; soft-deleted rows keep SortOrder = 0.
            migrationBuilder.Sql(@"
                ;WITH CTE AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY ISNULL(DivisionId, -1) ORDER BY Id) AS RowNum
                    FROM Categories
                    WHERE IsDeleted = 0
                )
                UPDATE c
                SET c.SortOrder = CTE.RowNum
                FROM Categories c
                INNER JOIN CTE ON CTE.Id = c.Id;
            ");

            // 2) Strip a leading running-number prefix such as ""12. "" or ""3."" from Name,
            //    now that the number lives in SortOrder. Only strips when everything before
            //    the first '.' is purely digits, so names like ""Node.js Basics"" are untouched.
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.Name = LTRIM(SUBSTRING(c.Name, x.DotPos + 1, LEN(c.Name)))
                FROM Categories c
                CROSS APPLY (SELECT CHARINDEX('.', c.Name) AS DotPos) x
                WHERE x.DotPos > 1
                  AND LEFT(c.Name, x.DotPos - 1) NOT LIKE '%[^0-9]%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: the Name prefix stripped in Up() cannot be restored here — back up
            // Category.Name before deploying to PROD if this migration may need to be reverted.
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Categories");
        }
    }
}
