using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowIQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionInvoiceCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountInReportingCurrency",
                table: "Transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Transactions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToReportingCurrency",
                table: "Transactions",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountInReportingCurrency",
                table: "Invoices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToReportingCurrency",
                table: "Invoices",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            // Backfill: every pre-existing row was implicitly recorded in its owning company's currency,
            // at an effective 1:1 rate (there was no other currency it could have been).
            migrationBuilder.Sql(@"
                UPDATE ""Transactions"" t
                SET ""Currency"" = c.""Currency"",
                    ""AmountInReportingCurrency"" = t.""Amount"",
                    ""ExchangeRateToReportingCurrency"" = 1
                FROM ""Companies"" c
                WHERE t.""CompanyId"" = c.""Id"";
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Invoices"" i
                SET ""Currency"" = c.""Currency"",
                    ""AmountInReportingCurrency"" = i.""Amount"",
                    ""ExchangeRateToReportingCurrency"" = 1
                FROM ""Companies"" c
                WHERE i.""CompanyId"" = c.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountInReportingCurrency",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToReportingCurrency",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AmountInReportingCurrency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToReportingCurrency",
                table: "Invoices");
        }
    }
}
