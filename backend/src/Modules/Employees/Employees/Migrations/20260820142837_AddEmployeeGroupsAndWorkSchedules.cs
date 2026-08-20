using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeGroupsAndWorkSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "employees");

            migrationBuilder.CreateTable(
                name: "employee_groups",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    number_of_rotations = table.Column<byte>(type: "smallint", nullable: false),
                    is_security = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_schedules",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    shift_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    break_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    break_end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    allowed_check_in_lateness_minutes = table.Column<int>(type: "integer", nullable: false),
                    allowed_check_out_earliness_minutes = table.Column<int>(type: "integer", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_schedules_employee_groups_employee_group_id",
                        column: x => x.employee_group_id,
                        principalSchema: "employees",
                        principalTable: "employee_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_groups_name",
                schema: "employees",
                table: "employee_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_schedules_employee_group_id",
                schema: "employees",
                table: "work_schedules",
                column: "employee_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_schedules",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "employee_groups",
                schema: "employees");
        }
    }
}
