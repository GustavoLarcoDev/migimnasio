using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModeloRestaurante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Receta",
                table: "Productos",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionEntrega",
                table: "OrdenesVenta",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmpleadoId",
                table: "OrdenesVenta",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MesaId",
                table: "OrdenesVenta",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoOrden",
                table: "OrdenesVenta",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Negocios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MenusRestaurante",
                columns: table => new
                {
                    MenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoMenu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrecioFijo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ContenidoHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estilo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenusRestaurante", x => x.MenuId);
                    table.ForeignKey(
                        name: "FK_MenusRestaurante_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesas",
                columns: table => new
                {
                    MesaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Capacidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesas", x => x.MesaId);
                    table.ForeignKey(
                        name: "FK_Mesas_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesVenta_EmpleadoId",
                table: "OrdenesVenta",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesVenta_MesaId",
                table: "OrdenesVenta",
                column: "MesaId");

            migrationBuilder.CreateIndex(
                name: "IX_MenusRestaurante_NegocioId",
                table: "MenusRestaurante",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesas_NegocioId",
                table: "Mesas",
                column: "NegocioId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesVenta_Empleados_EmpleadoId",
                table: "OrdenesVenta",
                column: "EmpleadoId",
                principalTable: "Empleados",
                principalColumn: "EmpleadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesVenta_Mesas_MesaId",
                table: "OrdenesVenta",
                column: "MesaId",
                principalTable: "Mesas",
                principalColumn: "MesaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesVenta_Empleados_EmpleadoId",
                table: "OrdenesVenta");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesVenta_Mesas_MesaId",
                table: "OrdenesVenta");

            migrationBuilder.DropTable(
                name: "MenusRestaurante");

            migrationBuilder.DropTable(
                name: "Mesas");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesVenta_EmpleadoId",
                table: "OrdenesVenta");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesVenta_MesaId",
                table: "OrdenesVenta");

            migrationBuilder.DropColumn(
                name: "Receta",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "DireccionEntrega",
                table: "OrdenesVenta");

            migrationBuilder.DropColumn(
                name: "EmpleadoId",
                table: "OrdenesVenta");

            migrationBuilder.DropColumn(
                name: "MesaId",
                table: "OrdenesVenta");

            migrationBuilder.DropColumn(
                name: "TipoOrden",
                table: "OrdenesVenta");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Negocios");
        }
    }
}
