using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddUltimoRecordatorioCobro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoRecordatorioCobro",
                table: "Clientes",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UltimoRecordatorioCobro",
                table: "Clientes");
        }
    }
}
