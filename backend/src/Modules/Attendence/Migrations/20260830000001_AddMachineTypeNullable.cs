using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Modules.Attendence.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineTypeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "attendance",
                table: "machines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE attendance.machines
                SET ""Type"" = 'ZKTecoGateway'
                WHERE ""Type"" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                schema: "attendance",
                table: "machines");
        }
    }
}
