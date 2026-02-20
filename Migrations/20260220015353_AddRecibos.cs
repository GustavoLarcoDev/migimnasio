using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddRecibos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Recibos",
                columns: table => new
                {
                    ReciboId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroRecibo = table.Column<int>(type: "int", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoRecibo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DestinatarioEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DestinatarioNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NegocioNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Concepto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ContenidoHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recibos", x => x.ReciboId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sugerencias_NegocioId",
                table: "Sugerencias",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_NegocioId",
                table: "Productos",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_NegocioId",
                table: "Notificaciones",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_NegocioId_ClienteId_Fecha",
                table: "Notificaciones",
                columns: new[] { "NegocioId", "ClienteId", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Negocios_Email_Unique",
                table: "Negocios",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_NegocioId",
                table: "MovimientosInventario",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_NegocioId",
                table: "Logs",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_NegocioId_Fecha",
                table: "Logs",
                columns: new[] { "NegocioId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NegocioId",
                table: "Clientes",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NegocioId_FechaQueTermina",
                table: "Clientes",
                columns: new[] { "NegocioId", "FechaQueTermina" });

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_NegocioId_FechaCreacion",
                table: "Recibos",
                columns: new[] { "NegocioId", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_NegocioId_NumeroRecibo",
                table: "Recibos",
                columns: new[] { "NegocioId", "NumeroRecibo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Recibos");

            migrationBuilder.DropIndex(
                name: "IX_Sugerencias_NegocioId",
                table: "Sugerencias");

            migrationBuilder.DropIndex(
                name: "IX_Productos_NegocioId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_NegocioId",
                table: "Notificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Notificaciones_NegocioId_ClienteId_Fecha",
                table: "Notificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Negocios_Email_Unique",
                table: "Negocios");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_NegocioId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_Logs_NegocioId",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_NegocioId_Fecha",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_NegocioId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_NegocioId_FechaQueTermina",
                table: "Clientes");
        }
    }
}
