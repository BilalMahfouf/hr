using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Employees.Migrations
{
    /// <inheritdoc />
    public partial class work_sheduleadd_end_day_offset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "end_day_offset",
                schema: "employees",
                table: "work_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_day_offset",
                schema: "employees",
                table: "work_schedules");
        }
    }
}
