using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class work_scheduleadd_is_active_field : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "employees",
                table: "work_schedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "employees",
                table: "work_schedules");
        }
    }
}
