using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotToEnrollmentAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SnapshotCompleted",
                table: "EnrollmentAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SnapshotCompletedDate",
                table: "EnrollmentAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SnapshotProgress",
                table: "EnrollmentAssignments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SnapshotCompleted",
                table: "EnrollmentAssignments");

            migrationBuilder.DropColumn(
                name: "SnapshotCompletedDate",
                table: "EnrollmentAssignments");

            migrationBuilder.DropColumn(
                name: "SnapshotProgress",
                table: "EnrollmentAssignments");
        }
    }
}
