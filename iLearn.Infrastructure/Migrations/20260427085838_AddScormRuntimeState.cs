using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScormRuntimeState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScormRuntimeStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    ScormVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LessonLocation = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SuspendData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LessonStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CompletionStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SuccessStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RawScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    SessionTime = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TotalTime = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Entry = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Exit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastCommittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CmiSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ScormRuntimeStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScormRuntimeStates_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScormRuntimeStates_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScormRuntimeStates_EnrollmentId_ResourceId",
                table: "ScormRuntimeStates",
                columns: new[] { "EnrollmentId", "ResourceId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ScormRuntimeStates_LastCommittedAtUtc",
                table: "ScormRuntimeStates",
                column: "LastCommittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScormRuntimeStates_ResourceId",
                table: "ScormRuntimeStates",
                column: "ResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScormRuntimeStates");
        }
    }
}
