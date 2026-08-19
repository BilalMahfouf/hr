using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Attendence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePunchDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_records_EmployeeId_PunchDate",
                schema: "attendance",
                table: "attendance_records");

            migrationBuilder.DropColumn(
                name: "PunchDate",
                schema: "attendance",
                table: "attendance_records");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "EmployeeId", "CheckInAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records");

            migrationBuilder.AddColumn<DateTime>(
                name: "PunchDate",
                schema: "attendance",
                table: "attendance_records",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_EmployeeId_PunchDate",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "EmployeeId", "PunchDate" });
        }
    }
}
