using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Attendence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attendance");

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<string>(type: "text", nullable: false),
                    PunchDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WorkedTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Overtime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    LateTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EarlyLeaveTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsAbsent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "punches",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeBadge = table.Column<int>(type: "integer", nullable: false),
                    PunchOccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_punches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_EmployeeId_PunchDate",
                schema: "attendance",
                table: "attendance_records",
                columns: new[] { "EmployeeId", "PunchDate" });

            migrationBuilder.CreateIndex(
                name: "IX_punches_MachineId_EmployeeBadge_PunchOccurredAt",
                schema: "attendance",
                table: "punches",
                columns: new[] { "MachineId", "EmployeeBadge", "PunchOccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "punches",
                schema: "attendance");
        }
    }
}
