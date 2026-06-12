using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCachedFileLengthToContentItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CachedFileLength",
                table: "ContentItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ci SET ci.CachedFileLength = fs.[Length]
                  FROM ContentItems ci
                  INNER JOIN FileStorages fs ON ci.FileStorageId = fs.Id
                  WHERE ci.FileStorageId IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CachedFileLength",
                table: "ContentItems");
        }
    }
}
