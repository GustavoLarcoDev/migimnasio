using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWhatsAppFieldsFromNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppAccessToken",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "WhatsAppEnabled",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "WhatsAppPhoneNumberId",
                table: "Negocios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppAccessToken",
                table: "Negocios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppEnabled",
                table: "Negocios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppPhoneNumberId",
                table: "Negocios",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
