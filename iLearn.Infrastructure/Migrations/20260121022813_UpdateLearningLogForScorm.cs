using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLearningLogForScorm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "LearningLogs",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "LearnTime",
                table: "LearningLogs",
                newName: "SuspendData");

            migrationBuilder.RenameColumn(
                name: "ExamTime",
                table: "LearningLogs",
                newName: "LessonLocation");

            migrationBuilder.RenameColumn(
                name: "ContentId",
                table: "LearningLogs",
                newName: "CourseVersionId");

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

            migrationBuilder.AddColumn<string>(
                name: "TotalTime",
                table: "LearningLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "LastAccessDate",
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

            migrationBuilder.DropColumn(
                name: "TotalTime",
                table: "LearningLogs");

            migrationBuilder.RenameColumn(
                name: "SuspendData",
                table: "LearningLogs",
                newName: "LearnTime");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "LearningLogs",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "LessonLocation",
                table: "LearningLogs",
                newName: "ExamTime");

            migrationBuilder.RenameColumn(
                name: "CourseVersionId",
                table: "LearningLogs",
                newName: "ContentId");
        }
    }
}
