using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLearningLogTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "LastAccessDate",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "LessonLocation",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "LessonStatus",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "ScoreMax",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "ScoreMin",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "ScoreRaw",
                table: "LearningLogs");

            migrationBuilder.RenameColumn(
                name: "TotalTime",
                table: "LearningLogs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "SuspendData",
                table: "LearningLogs",
                newName: "SessionTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessed",
                table: "LearningLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "Progress",
                table: "LearningLogs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "LearningLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "LearningLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAccessed",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "LearningLogs");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "LearningLogs",
                newName: "TotalTime");

            migrationBuilder.RenameColumn(
                name: "SessionTime",
                table: "LearningLogs",
                newName: "SuspendData");

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "LearningLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "LearningLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CourseId",
                table: "LearningLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "LearningLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessDate",
                table: "LearningLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonLocation",
                table: "LearningLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonStatus",
                table: "LearningLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreMax",
                table: "LearningLogs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreMin",
                table: "LearningLogs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScoreRaw",
                table: "LearningLogs",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
