using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class PaymentsModuleLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Upgrade uncustomized standard navigation in existing tenant/demo copies.
            migrationBuilder.Sql("""
                UPDATE "MenuItems" SET "RequiredModule" = 'payments', "Title" = 'Payments', "Url" = '/payments', "Icon" = 'CreditCard', "RequiredPermissionKey" = 'payments.view'
                WHERE "Key" = 'features' AND "Url" = '/features' AND "RequiredModule" IN ('showcase', 'Showcase');
                UPDATE "MenuItems" SET "RequiredModule" = 'payments', "Url" = '/payments'
                WHERE "Key" = 'features-payments' AND "Url" = '/features/payments' AND "RequiredModule" IN ('showcase', 'Showcase');
                """);

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"),
                columns: new[] { "Description", "Icon", "RequiredModule", "RequiredPermissionKey", "Title", "Url" },
                values: new object[] { "Payment history and checkout.", "CreditCard", "payments", "payments.view", "Payments", "/payments" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                columns: new[] { "RequiredModule", "Url" },
                values: new object[] { "payments", "/payments" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10615"),
                column: "Url",
                value: "/payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"),
                columns: new[] { "Description", "Icon", "RequiredModule", "RequiredPermissionKey", "Title", "Url" },
                values: new object[] { "Reusable platform capability showcases.", "MenuOpen", "showcase", null, "Features", "/features" });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"),
                columns: new[] { "RequiredModule", "Url" },
                values: new object[] { "showcase", "/features/payments" });

            migrationBuilder.UpdateData(
                table: "MenuRoutes",
                keyColumn: "Id",
                keyValue: new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10615"),
                column: "Url",
                value: "/features/payments");
        }
    }
}
