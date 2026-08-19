using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentToVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "payment_amount",
                table: "visits",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte>(
                name: "payment_status",
                table: "visits",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_amount",
                table: "visits");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "visits");
        }
    }
}
