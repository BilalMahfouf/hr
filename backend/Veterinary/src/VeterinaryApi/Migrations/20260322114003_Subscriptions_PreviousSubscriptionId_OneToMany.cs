using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeterinaryApi.Migrations
{
    /// <inheritdoc />
    public partial class Subscriptions_PreviousSubscriptionId_OneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriptions_previous_subscription_id",
                table: "subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_previous_subscription_id",
                table: "subscriptions",
                column: "previous_subscription_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_subscriptions_previous_subscription_id",
                table: "subscriptions");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_previous_subscription_id",
                table: "subscriptions",
                column: "previous_subscription_id",
                unique: true);
        }
    }
}
