using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowIQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsNewSeenAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LinkLabel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Audience = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShowOnDashboard = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUpdates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductUpdateDismissals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUpdateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DismissedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUpdateDismissals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductUpdateDismissals_ProductUpdates_ProductUpdateId",
                        column: x => x.ProductUpdateId,
                        principalTable: "ProductUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductUpdateDismissals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateDismissals_ProductUpdateId",
                table: "ProductUpdateDismissals",
                column: "ProductUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdateDismissals_UserId_ProductUpdateId",
                table: "ProductUpdateDismissals",
                columns: new[] { "UserId", "ProductUpdateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUpdates_PublishedAtUtc",
                table: "ProductUpdates",
                column: "PublishedAtUtc");

            // The first "What's new" entries: the features shipped this week, published so existing users see them.
            migrationBuilder.Sql(@"
                INSERT INTO ""ProductUpdates""
                    (""Id"", ""Title"", ""Summary"", ""LinkUrl"", ""LinkLabel"", ""Audience"", ""ShowOnDashboard"", ""PublishedAtUtc"", ""CreatedAtUtc"", ""CreatedBy"")
                VALUES
                ('8f2a1c10-0001-4a6b-9c1e-0f1d2c3b4a01', 'Let FinFlow chase late payers', 'Turn on payment reminders and FinFlow emails your customers a friendly reminder when an invoice is overdue. You choose the days, and you can leave any invoice out.', '/settings', 'Turn on reminders', 'OwnersAndAdmins', true, timestamptz '2026-10-01 10:05:00+00', timestamptz '2026-10-01 10:05:00+00', 'seed'),
                ('8f2a1c10-0002-4a6b-9c1e-0f1d2c3b4a02', 'Email invoices straight to customers', 'Send an invoice from FinFlow in one click. Your customer gets it by email with a link to view and download it, and their replies come to you.', '/invoices', 'Open invoices', 'Everyone', true, timestamptz '2026-10-01 10:04:00+00', timestamptz '2026-10-01 10:04:00+00', 'seed'),
                ('8f2a1c10-0003-4a6b-9c1e-0f1d2c3b4a03', 'Plan for school fees and home rent', 'Tell FinFlow about money you regularly take out for yourself, and your forecast shows the dip on the day it happens instead of smoothing it away.', '/forecasting', 'Add planned withdrawals', 'Everyone', false, timestamptz '2026-10-01 10:03:00+00', timestamptz '2026-10-01 10:03:00+00', 'seed'),
                ('8f2a1c10-0004-4a6b-9c1e-0f1d2c3b4a04', 'Owner Drawings and Owner Contribution', 'Record money you take out of, or put into, the business yourself. It counts in your cash balance but not as business expenses or revenue.', '/transactions', 'Add a transaction', 'Everyone', false, timestamptz '2026-10-01 10:02:00+00', timestamptz '2026-10-01 10:02:00+00', 'seed'),
                ('8f2a1c10-0005-4a6b-9c1e-0f1d2c3b4a05', 'Overdue invoices are marked automatically', 'Unpaid invoices now switch to Overdue the day after they''re due, so you can see at a glance who''s late.', '/invoices', 'See invoices', 'Everyone', false, timestamptz '2026-10-01 10:01:00+00', timestamptz '2026-10-01 10:01:00+00', 'seed');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductUpdateDismissals");

            migrationBuilder.DropTable(
                name: "ProductUpdates");

            migrationBuilder.DropColumn(
                name: "WhatsNewSeenAtUtc",
                table: "Users");
        }
    }
}
