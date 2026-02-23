using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartDateEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentRules_Divisions_DivisionId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentRules_Roles_RoleId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentRules_DivisionId",
                table: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentRules_RoleId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Assignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Assignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Assignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Assignments");

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_DivisionId",
                table: "Assignments",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_RoleId",
                table: "Assignments",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentRules_Divisions_DivisionId",
                table: "Assignments",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentRules_Roles_RoleId",
                table: "Assignments",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }
    }
}
