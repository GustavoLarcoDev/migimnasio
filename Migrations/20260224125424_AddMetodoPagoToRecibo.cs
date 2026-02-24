using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddMetodoPagoToRecibo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Recibos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Recibos");
        }
    }
}
