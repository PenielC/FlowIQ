using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowIQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoData",
                table: "Companies",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LogoData",
                table: "Companies");
        }
    }
}
