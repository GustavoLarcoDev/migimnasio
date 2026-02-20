using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModeloTienda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaProductoId",
                table: "Productos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenUrl",
                table: "Productos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriasProducto",
                columns: table => new
                {
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasProducto", x => x.CategoriaId);
                    table.ForeignKey(
                        name: "FK_CategoriasProducto_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesVenta",
                columns: table => new
                {
                    OrdenVentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NumeroOrden = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PorcentajeIva = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoIva = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GastosAdicionales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReciboId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesVenta", x => x.OrdenVentaId);
                    table.ForeignKey(
                        name: "FK_OrdenesVenta_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenesVenta_Recibos_ReciboId",
                        column: x => x.ReciboId,
                        principalTable: "Recibos",
                        principalColumn: "ReciboId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DetallesOrdenVenta",
                columns: table => new
                {
                    DetalleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenVentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesOrdenVenta", x => x.DetalleId);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenVenta_OrdenesVenta_OrdenVentaId",
                        column: x => x.OrdenVentaId,
                        principalTable: "OrdenesVenta",
                        principalColumn: "OrdenVentaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenVenta_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "ProductoId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaProductoId",
                table: "Productos",
                column: "CategoriaProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProducto_NegocioId",
                table: "CategoriasProducto",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenVenta_OrdenVentaId",
                table: "DetallesOrdenVenta",
                column: "OrdenVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenVenta_ProductoId",
                table: "DetallesOrdenVenta",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesVenta_NegocioId",
                table: "OrdenesVenta",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesVenta_ReciboId",
                table: "OrdenesVenta",
                column: "ReciboId");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CategoriasProducto_CategoriaProductoId",
                table: "Productos",
                column: "CategoriaProductoId",
                principalTable: "CategoriasProducto",
                principalColumn: "CategoriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CategoriasProducto_CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "CategoriasProducto");

            migrationBuilder.DropTable(
                name: "DetallesOrdenVenta");

            migrationBuilder.DropTable(
                name: "OrdenesVenta");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ImagenUrl",
                table: "Productos");
        }
    }
}
