using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeDeleteForWorkSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_work_schedules_employee_groups_employee_group_id",
                schema: "employees",
                table: "work_schedules");

            migrationBuilder.AddForeignKey(
                name: "fk_work_schedules_employee_groups_employee_group_id",
                schema: "employees",
                table: "work_schedules",
                column: "employee_group_id",
                principalSchema: "employees",
                principalTable: "employee_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_work_schedules_employee_groups_employee_group_id",
                schema: "employees",
                table: "work_schedules");

            migrationBuilder.AddForeignKey(
                name: "fk_work_schedules_employee_groups_employee_group_id",
                schema: "employees",
                table: "work_schedules",
                column: "employee_group_id",
                principalSchema: "employees",
                principalTable: "employee_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
