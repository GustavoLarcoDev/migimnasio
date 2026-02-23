using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddMetodoPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetodosPago",
                columns: table => new
                {
                    MetodoPagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroCuenta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Cedula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NombreTitular = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ImagenQR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instrucciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsPredeterminado = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodosPago", x => x.MetodoPagoId);
                    table.ForeignKey(
                        name: "FK_MetodosPago_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetodosPago_NegocioId",
                table: "MetodosPago",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_MetodosPago_NegocioId_IsActive",
                table: "MetodosPago",
                columns: new[] { "NegocioId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetodosPago");
        }
    }
}
