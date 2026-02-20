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
                table: "AssignmentRules");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentRules_Roles_RoleId",
                table: "AssignmentRules");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentRules_DivisionId",
                table: "AssignmentRules");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentRules_RoleId",
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "DivisionId",
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "AssignmentRules");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AssignmentRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Division",
                table: "AssignmentRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "AssignmentRules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "AssignmentRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "AssignmentRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "AssignmentRules",
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
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "Division",
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "AssignmentRules");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "AssignmentRules");

            migrationBuilder.AddColumn<int>(
                name: "DivisionId",
                table: "AssignmentRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "AssignmentRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_DivisionId",
                table: "AssignmentRules",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRules_RoleId",
                table: "AssignmentRules",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentRules_Divisions_DivisionId",
                table: "AssignmentRules",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentRules_Roles_RoleId",
                table: "AssignmentRules",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }
    }
}
