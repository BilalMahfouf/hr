using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Attendence.Migrations
{
    /// <inheritdoc />
    public partial class attendance_attendance_records_remove_unique_index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "EmployeeId", "CheckInAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "EmployeeId", "CheckInAt" },
                unique: true);
        }
    }
}
