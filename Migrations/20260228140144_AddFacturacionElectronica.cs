using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturacionElectronica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgenteRetencion",
                table: "Negocios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificadoP12Base64",
                table: "Negocios",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificadoPasswordEncriptado",
                table: "Negocios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoEstablecimiento",
                table: "Negocios",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContribuyenteEspecial",
                table: "Negocios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionMatriz",
                table: "Negocios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FacturacionElectronicaActiva",
                table: "Negocios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NombreComercial",
                table: "Negocios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ObligadoContabilidad",
                table: "Negocios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PuntoEmision",
                table: "Negocios",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "Negocios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegimenContribuyente",
                table: "Negocios",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ruc",
                table: "Negocios",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SriAmbiente",
                table: "Negocios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FacturasElectronicas",
                columns: table => new
                {
                    FacturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReciboId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Establecimiento = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    PuntoEmision = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Secuencial = table.Column<int>(type: "int", nullable: false),
                    NumeroCompleto = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: true),
                    ClaveAcceso = table.Column<string>(type: "nvarchar(49)", maxLength: 49, nullable: true),
                    CompradorIdentificacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompradorTipoIdentificacion = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    CompradorRazonSocial = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompradorEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompradorDireccion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalSinImpuestos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDescuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoIva = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FormaPagoSri = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    XmlAutorizadoComprimido = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    EstadoSri = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "nvarchar(49)", maxLength: 49, nullable: true),
                    FechaAutorizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MensajesSri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RidePdf = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    DetalleItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntentosEnvio = table.Column<int>(type: "int", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimoIntento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasElectronicas", x => x.FacturaId);
                    table.ForeignKey(
                        name: "FK_FacturasElectronicas_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturasElectronicas_Recibos_ReciboId",
                        column: x => x.ReciboId,
                        principalTable: "Recibos",
                        principalColumn: "ReciboId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_NegocioId_ClaveAcceso",
                table: "FacturasElectronicas",
                columns: new[] { "NegocioId", "ClaveAcceso" },
                unique: true,
                filter: "[ClaveAcceso] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_NegocioId_EstadoSri",
                table: "FacturasElectronicas",
                columns: new[] { "NegocioId", "EstadoSri" });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_NegocioId_FechaEmision",
                table: "FacturasElectronicas",
                columns: new[] { "NegocioId", "FechaEmision" });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_Numeracion",
                table: "FacturasElectronicas",
                columns: new[] { "NegocioId", "Establecimiento", "PuntoEmision", "Secuencial" },
                unique: true,
                filter: "[Establecimiento] IS NOT NULL AND [PuntoEmision] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasElectronicas_ReciboId",
                table: "FacturasElectronicas",
                column: "ReciboId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturasElectronicas");

            migrationBuilder.DropColumn(
                name: "AgenteRetencion",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "CertificadoP12Base64",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "CertificadoPasswordEncriptado",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "CodigoEstablecimiento",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "ContribuyenteEspecial",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "DireccionMatriz",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "FacturacionElectronicaActiva",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "NombreComercial",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "ObligadoContabilidad",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "PuntoEmision",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "RazonSocial",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "RegimenContribuyente",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "Ruc",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "SriAmbiente",
                table: "Negocios");
        }
    }
}
