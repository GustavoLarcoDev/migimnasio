using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddCitaRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Citas",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayRsvps_FechaCreacion",
                table: "BirthdayRsvps",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayRegalos_IsActive",
                table: "BirthdayRegalos",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BirthdayComidas_IsActive",
                table: "BirthdayComidas",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BirthdayRsvps_FechaCreacion",
                table: "BirthdayRsvps");

            migrationBuilder.DropIndex(
                name: "IX_BirthdayRegalos_IsActive",
                table: "BirthdayRegalos");

            migrationBuilder.DropIndex(
                name: "IX_BirthdayComidas_IsActive",
                table: "BirthdayComidas");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Citas");
        }
    }
}
