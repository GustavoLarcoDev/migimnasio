using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryRegistrationAndPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BloqueadoDelivery",
                table: "Negocios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Negocios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionesDeliveryAcumuladas",
                table: "Negocios",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "Negocios",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "Negocios",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoPlanDelivery",
                table: "Negocios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TiposComida",
                table: "Negocios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaLiquidacionComisionesDelivery",
                table: "Negocios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Bloqueado",
                table: "Motorizados",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "Motorizados",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionesAcumuladas",
                table: "Motorizados",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpiracion",
                table: "Motorizados",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Motorizados",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoCedulaFrontal",
                table: "Motorizados",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoCedulaTrasera",
                table: "Motorizados",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoLicencia",
                table: "Motorizados",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoSelfie",
                table: "Motorizados",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoVehiculo",
                table: "Motorizados",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioSuscripcion",
                table: "Motorizados",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TipoPlan",
                table: "Motorizados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoVehiculo",
                table: "Motorizados",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaLiquidacionComisiones",
                table: "Motorizados",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComisionesDelivery",
                columns: table => new
                {
                    ComisionDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoPagador = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotorizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Pagada = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComisionesDelivery", x => x.ComisionDeliveryId);
                    table.ForeignKey(
                        name: "FK_ComisionesDelivery_Motorizados_MotorizadoId",
                        column: x => x.MotorizadoId,
                        principalTable: "Motorizados",
                        principalColumn: "MotorizadoId");
                    table.ForeignKey(
                        name: "FK_ComisionesDelivery_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId");
                    table.ForeignKey(
                        name: "FK_ComisionesDelivery_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "PedidoId");
                });

            migrationBuilder.CreateTable(
                name: "PagosDelivery",
                columns: table => new
                {
                    PagoDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoPagador = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotorizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NumeroConfirmacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosDelivery", x => x.PagoDeliveryId);
                    table.ForeignKey(
                        name: "FK_PagosDelivery_Motorizados_MotorizadoId",
                        column: x => x.MotorizadoId,
                        principalTable: "Motorizados",
                        principalColumn: "MotorizadoId");
                    table.ForeignKey(
                        name: "FK_PagosDelivery_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId");
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesRegistro",
                columns: table => new
                {
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoSolicitud = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    TipoVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Placa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FotoCedulaFrontal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoCedulaTrasera = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoLicencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoSelfie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreNegocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DuenoNegocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitud = table.Column<double>(type: "float", nullable: true),
                    Longitud = table.Column<double>(type: "float", nullable: true),
                    TiposComida = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesRegistro", x => x.SolicitudId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Motorizados_Cedula_Unique",
                table: "Motorizados",
                column: "Cedula",
                unique: true,
                filter: "[Cedula] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComisionesDelivery_MotorizadoId_Pagada",
                table: "ComisionesDelivery",
                columns: new[] { "MotorizadoId", "Pagada" });

            migrationBuilder.CreateIndex(
                name: "IX_ComisionesDelivery_NegocioId_Pagada",
                table: "ComisionesDelivery",
                columns: new[] { "NegocioId", "Pagada" });

            migrationBuilder.CreateIndex(
                name: "IX_ComisionesDelivery_PedidoId",
                table: "ComisionesDelivery",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDelivery_MotorizadoId",
                table: "PagosDelivery",
                column: "MotorizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDelivery_NegocioId",
                table: "PagosDelivery",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosDelivery_TipoPagador_Estado",
                table: "PagosDelivery",
                columns: new[] { "TipoPagador", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRegistro_Cedula_Unique",
                table: "SolicitudesRegistro",
                column: "Cedula",
                unique: true,
                filter: "[TipoSolicitud] = 'motorizado' AND [Cedula] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRegistro_Email",
                table: "SolicitudesRegistro",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesRegistro_Tipo_Estado",
                table: "SolicitudesRegistro",
                columns: new[] { "TipoSolicitud", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComisionesDelivery");

            migrationBuilder.DropTable(
                name: "PagosDelivery");

            migrationBuilder.DropTable(
                name: "SolicitudesRegistro");

            migrationBuilder.DropIndex(
                name: "IX_Motorizados_Cedula_Unique",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "BloqueadoDelivery",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "ComisionesDeliveryAcumuladas",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "TipoPlanDelivery",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "TiposComida",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "UltimaLiquidacionComisionesDelivery",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "Bloqueado",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "ComisionesAcumuladas",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FechaExpiracion",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FotoCedulaFrontal",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FotoCedulaTrasera",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FotoLicencia",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FotoSelfie",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "FotoVehiculo",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "PrecioSuscripcion",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "TipoPlan",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "TipoVehiculo",
                table: "Motorizados");

            migrationBuilder.DropColumn(
                name: "UltimaLiquidacionComisiones",
                table: "Motorizados");
        }
    }
}
