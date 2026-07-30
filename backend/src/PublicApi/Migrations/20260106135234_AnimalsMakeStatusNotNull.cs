using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class AnimalsMakeStatusNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "status",
                table: "animals",
                type: "smallint",
                maxLength: 10,
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldMaxLength: 10,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "status",
                table: "animals",
                type: "smallint",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "smallint",
                oldMaxLength: 10);
        }
    }
}
