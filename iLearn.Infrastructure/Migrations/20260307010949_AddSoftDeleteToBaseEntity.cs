using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "UserRoles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Roles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Resources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LearningLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "LearningLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LearningLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FileStorages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FileStorages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FileStorages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Enrollments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Divisions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Divisions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Divisions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CourseVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CourseVersions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CourseVersions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CourseTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CourseTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CourseTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Courses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CourseResources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CourseResources",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CourseResources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Categories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Categories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Assignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "CourseTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeletedAt", "DeletedBy", "IsDeleted" },
                values: new object[] { null, null, false });

            migrationBuilder.UpdateData(
                table: "CourseTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DeletedAt", "DeletedBy", "IsDeleted" },
                values: new object[] { null, null, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "LearningLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FileStorages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FileStorages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FileStorages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CourseVersions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CourseVersions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CourseVersions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CourseTypes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CourseTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CourseTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CourseResources");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CourseResources");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CourseResources");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Assignments");
        }
    }
}
