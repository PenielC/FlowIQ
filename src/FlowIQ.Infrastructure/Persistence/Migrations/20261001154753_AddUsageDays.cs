using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowIQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Feature = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    LastAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageDays_CompanyId_Feature",
                table: "UsageDays",
                columns: new[] { "CompanyId", "Feature" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageDays_Day_CompanyId",
                table: "UsageDays",
                columns: new[] { "Day", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageDays_UserId_Feature_Day",
                table: "UsageDays",
                columns: new[] { "UserId", "Feature", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageDays");
        }
    }
}
