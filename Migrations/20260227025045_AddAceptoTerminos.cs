using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddAceptoTerminos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceptoTerminos",
                table: "Vendedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptoTerminos",
                table: "Vendedores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AceptoTerminos",
                table: "Negocios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAceptoTerminos",
                table: "Negocios",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceptoTerminos",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "FechaAceptoTerminos",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "AceptoTerminos",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "FechaAceptoTerminos",
                table: "Negocios");
        }
    }
}
