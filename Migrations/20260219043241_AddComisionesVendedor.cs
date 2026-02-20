using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddComisionesVendedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreBanco",
                table: "Vendedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCedula",
                table: "Vendedores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroCuenta",
                table: "Vendedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComisionesVendedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreVendedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreNegocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MontoComision = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoComision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiasContratados = table.Column<int>(type: "int", nullable: false),
                    PrecioNegocio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Pagada = table.Column<bool>(type: "bit", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComisionesVendedor", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComisionesVendedor");

            migrationBuilder.DropColumn(
                name: "NombreBanco",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "NumeroCedula",
                table: "Vendedores");

            migrationBuilder.DropColumn(
                name: "NumeroCuenta",
                table: "Vendedores");
        }
    }
}
