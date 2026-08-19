using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PublicApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscriptions_tenant_id",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_subscriptions_tenant_id_doctor_id_status",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_tenant_id",
                table: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_tenant_id_slug",
                table: "subscription_plans");

            migrationBuilder.DropIndex(
                name: "ix_subscription_payments_tenant_id",
                table: "subscription_payments");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "subscription_payments");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "notifications",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_notifications_tenant_id",
                table: "notifications",
                newName: "ix_notifications_user_id");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "notification_push_subscriptions",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_notification_push_subscriptions_tenant_id",
                table: "notification_push_subscriptions",
                newName: "ix_notification_push_subscriptions_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_doctor_id_status",
                table: "subscriptions",
                columns: new[] { "doctor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_slug",
                table: "subscription_plans",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscriptions_doctor_id_status",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_slug",
                table: "subscription_plans");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "notifications",
                newName: "tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_notifications_user_id",
                table: "notifications",
                newName: "ix_notifications_tenant_id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "notification_push_subscriptions",
                newName: "tenant_id");

            migrationBuilder.RenameIndex(
                name: "ix_notification_push_subscriptions_user_id",
                table: "notification_push_subscriptions",
                newName: "ix_notification_push_subscriptions_tenant_id");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "subscriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "subscription_plans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "subscription_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_tenant_id",
                table: "subscriptions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_tenant_id_doctor_id_status",
                table: "subscriptions",
                columns: new[] { "tenant_id", "doctor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_tenant_id",
                table: "subscription_plans",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_tenant_id_slug",
                table: "subscription_plans",
                columns: new[] { "tenant_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_payments_tenant_id",
                table: "subscription_payments",
                column: "tenant_id");
        }
    }
}
