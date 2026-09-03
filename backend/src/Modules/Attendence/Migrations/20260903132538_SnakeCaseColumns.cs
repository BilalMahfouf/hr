using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Attendence.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "attendance",
                table: "punches",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PunchOccurredAt",
                schema: "attendance",
                table: "punches",
                newName: "punch_occurred_at");

            migrationBuilder.RenameColumn(
                name: "MachineId",
                schema: "attendance",
                table: "punches",
                newName: "machine_id");

            migrationBuilder.RenameColumn(
                name: "EmployeeBadge",
                schema: "attendance",
                table: "punches",
                newName: "employee_badge");

            migrationBuilder.RenameColumn(
                name: "CreatedOnUtc",
                schema: "attendance",
                table: "punches",
                newName: "created_on_utc");

            migrationBuilder.RenameIndex(
                name: "IX_punches_MachineId_EmployeeBadge_PunchOccurredAt",
                schema: "attendance",
                table: "punches",
                newName: "IX_punches_machine_id_employee_badge_punch_occurred_at");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "is_enabled");

            migrationBuilder.RenameColumn(
                name: "IntervalMinutes",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "interval_minutes");

            migrationBuilder.RenameColumn(
                name: "Type",
                schema: "attendance",
                table: "machines",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Port",
                schema: "attendance",
                table: "machines",
                newName: "port");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "attendance",
                table: "machines",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "MachineNumber",
                schema: "attendance",
                table: "machines",
                newName: "machine_number");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                schema: "attendance",
                table: "machines",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                schema: "attendance",
                table: "machines",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "Overtime",
                schema: "attendance",
                table: "attendance_records",
                newName: "overtime");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "attendance",
                table: "attendance_records",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WorkedTime",
                schema: "attendance",
                table: "attendance_records",
                newName: "worked_time");

            migrationBuilder.RenameColumn(
                name: "MachineId",
                schema: "attendance",
                table: "attendance_records",
                newName: "machine_id");

            migrationBuilder.RenameColumn(
                name: "LateTime",
                schema: "attendance",
                table: "attendance_records",
                newName: "late_time");

            migrationBuilder.RenameColumn(
                name: "IsAbsent",
                schema: "attendance",
                table: "attendance_records",
                newName: "is_absent");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                schema: "attendance",
                table: "attendance_records",
                newName: "employee_id");

            migrationBuilder.RenameColumn(
                name: "EarlyLeaveTime",
                schema: "attendance",
                table: "attendance_records",
                newName: "early_leave_time");

            migrationBuilder.RenameColumn(
                name: "CheckOutAt",
                schema: "attendance",
                table: "attendance_records",
                newName: "check_out_at");

            migrationBuilder.RenameColumn(
                name: "CheckInAt",
                schema: "attendance",
                table: "attendance_records",
                newName: "check_in_at");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_records_EmployeeId_CheckInAt",
                schema: "attendance",
                table: "attendance_records",
                newName: "IX_attendance_records_employee_id_check_in_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                schema: "attendance",
                table: "punches",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "punch_occurred_at",
                schema: "attendance",
                table: "punches",
                newName: "PunchOccurredAt");

            migrationBuilder.RenameColumn(
                name: "machine_id",
                schema: "attendance",
                table: "punches",
                newName: "MachineId");

            migrationBuilder.RenameColumn(
                name: "employee_badge",
                schema: "attendance",
                table: "punches",
                newName: "EmployeeBadge");

            migrationBuilder.RenameColumn(
                name: "created_on_utc",
                schema: "attendance",
                table: "punches",
                newName: "CreatedOnUtc");

            migrationBuilder.RenameIndex(
                name: "IX_punches_machine_id_employee_badge_punch_occurred_at",
                schema: "attendance",
                table: "punches",
                newName: "IX_punches_MachineId_EmployeeBadge_PunchOccurredAt");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "is_enabled",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "IsEnabled");

            migrationBuilder.RenameColumn(
                name: "interval_minutes",
                schema: "attendance",
                table: "punch_polling_settings",
                newName: "IntervalMinutes");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "attendance",
                table: "machines",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "port",
                schema: "attendance",
                table: "machines",
                newName: "Port");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "attendance",
                table: "machines",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "machine_number",
                schema: "attendance",
                table: "machines",
                newName: "MachineNumber");

            migrationBuilder.RenameColumn(
                name: "is_active",
                schema: "attendance",
                table: "machines",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                schema: "attendance",
                table: "machines",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "overtime",
                schema: "attendance",
                table: "attendance_records",
                newName: "Overtime");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "attendance",
                table: "attendance_records",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "worked_time",
                schema: "attendance",
                table: "attendance_records",
                newName: "WorkedTime");

            migrationBuilder.RenameColumn(
                name: "machine_id",
                schema: "attendance",
                table: "attendance_records",
                newName: "MachineId");

            migrationBuilder.RenameColumn(
                name: "late_time",
                schema: "attendance",
                table: "attendance_records",
                newName: "LateTime");

            migrationBuilder.RenameColumn(
                name: "is_absent",
                schema: "attendance",
                table: "attendance_records",
                newName: "IsAbsent");

            migrationBuilder.RenameColumn(
                name: "employee_id",
                schema: "attendance",
                table: "attendance_records",
                newName: "EmployeeId");

            migrationBuilder.RenameColumn(
                name: "early_leave_time",
                schema: "attendance",
                table: "attendance_records",
                newName: "EarlyLeaveTime");

            migrationBuilder.RenameColumn(
                name: "check_out_at",
                schema: "attendance",
                table: "attendance_records",
                newName: "CheckOutAt");

            migrationBuilder.RenameColumn(
                name: "check_in_at",
                schema: "attendance",
                table: "attendance_records",
                newName: "CheckInAt");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_records_employee_id_check_in_at",
                schema: "attendance",
                table: "attendance_records",
                newName: "IX_attendance_records_EmployeeId_CheckInAt");
        }
    }
}
