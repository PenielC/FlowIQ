using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowIQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceEmailsAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Invoices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicToken",
                table: "Invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RemindersPaused",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<int>>(
                name: "ReminderDays",
                table: "Companies",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{1,7,14}'");

            migrationBuilder.AddColumn<bool>(
                name: "ReminderEnabled",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "InvoiceEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReminderDay = table.Column<int>(type: "integer", nullable: true),
                    ToEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceEmails_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PublicToken",
                table: "Invoices",
                column: "PublicToken",
                unique: true,
                filter: "\"PublicToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEmails_InvoiceId_AtUtc",
                table: "InvoiceEmails",
                columns: new[] { "InvoiceId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEmails_InvoiceId_ReminderDay",
                table: "InvoiceEmails",
                columns: new[] { "InvoiceId", "ReminderDay" },
                unique: true,
                filter: "\"Kind\" = 'Reminder' AND \"Status\" <> 'Failed'");

            // Existing invoices pick up the email of the saved customer with the same name, so they can be
            // emailed and reminded without retyping it.
            migrationBuilder.Sql(@"
                UPDATE ""Invoices"" AS i
                SET ""CustomerEmail"" = lower(trim(c.""Email""))
                FROM ""Customers"" AS c
                WHERE c.""CompanyId"" = i.""CompanyId""
                  AND lower(trim(c.""Name"")) = lower(trim(i.""CustomerName""))
                  AND c.""Email"" IS NOT NULL AND trim(c.""Email"") <> ''
                  AND i.""CustomerEmail"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceEmails");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PublicToken",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PublicToken",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RemindersPaused",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReminderDays",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReminderEnabled",
                table: "Companies");
        }
    }
}
