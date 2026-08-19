using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class ClientMergeIntoFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "first_name",
                table: "owners");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "owners",
                newName: "full_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "owners",
                newName: "last_name");

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "owners",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
