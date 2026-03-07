using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeliverySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComisionesDelivery");

            migrationBuilder.DropTable(
                name: "DetallesPedido");

            migrationBuilder.DropTable(
                name: "PagosDelivery");

            migrationBuilder.DropTable(
                name: "SolicitudesRegistro");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Motorizados");

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
                name: "TipoPlanDelivery",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "TiposComida",
                table: "Negocios");

            migrationBuilder.DropColumn(
                name: "UltimaLiquidacionComisionesDelivery",
                table: "Negocios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "Motorizados",
                columns: table => new
                {
                    MotorizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Bloqueado = table.Column<bool>(type: "bit", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ComisionesAcumuladas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FotoCedulaFrontal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoCedulaTrasera = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoLicencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoSelfie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDisponible = table.Column<bool>(type: "bit", nullable: false),
                    Latitud = table.Column<double>(type: "float", nullable: true),
                    Longitud = table.Column<double>(type: "float", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Placa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PrecioSuscripcion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoPlan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TipoVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TotalEntregas = table.Column<int>(type: "int", nullable: false),
                    UltimaLiquidacionComisiones = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Vehiculo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motorizados", x => x.MotorizadoId);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesRegistro",
                columns: table => new
                {
                    SolicitudId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Ciudad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DuenoNegocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FotoCedulaFrontal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoCedulaTrasera = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoLicencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoSelfie = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FotoVehiculo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitud = table.Column<double>(type: "float", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Longitud = table.Column<double>(type: "float", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreNegocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Placa = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoSolicitud = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TiposComida = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesRegistro", x => x.SolicitudId);
                });

            migrationBuilder.CreateTable(
                name: "PagosDelivery",
                columns: table => new
                {
                    PagoDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotorizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumeroConfirmacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoPagador = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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
                name: "Pedidos",
                columns: table => new
                {
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotorizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanceladoPor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CostoComida = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostoEnvio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DireccionEntrega = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaCancelado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaConfirmado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEntregado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaListo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaPreparando = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRecogido = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaTomado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LatitudCliente = table.Column<double>(type: "float", nullable: false),
                    LatitudRestaurante = table.Column<double>(type: "float", nullable: false),
                    LongitudCliente = table.Column<double>(type: "float", nullable: false),
                    LongitudRestaurante = table.Column<double>(type: "float", nullable: false),
                    MotorizadoConfirmoRecepcion = table.Column<bool>(type: "bit", nullable: false),
                    NombreCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RazonCancelacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RestauranteConfirmoEntrega = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TelefonoCliente = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.PedidoId);
                    table.ForeignKey(
                        name: "FK_Pedidos_Motorizados_MotorizadoId",
                        column: x => x.MotorizadoId,
                        principalTable: "Motorizados",
                        principalColumn: "MotorizadoId");
                    table.ForeignKey(
                        name: "FK_Pedidos_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComisionesDelivery",
                columns: table => new
                {
                    ComisionDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MotorizadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Pagada = table.Column<bool>(type: "bit", nullable: false),
                    TipoPagador = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
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
                name: "DetallesPedido",
                columns: table => new
                {
                    DetallePedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Confirmado = table.Column<bool>(type: "bit", nullable: false),
                    NombreProducto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Rechazado = table.Column<bool>(type: "bit", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesPedido", x => x.DetallePedidoId);
                    table.ForeignKey(
                        name: "FK_DetallesPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "PedidoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesPedido_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "ProductoId");
                });

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
                name: "IX_DetallesPedido_PedidoId",
                table: "DetallesPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesPedido_ProductoId",
                table: "DetallesPedido",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Motorizados_Cedula_Unique",
                table: "Motorizados",
                column: "Cedula",
                unique: true,
                filter: "[Cedula] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Motorizados_Email_Unique",
                table: "Motorizados",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motorizados_IsActive_IsDisponible",
                table: "Motorizados",
                columns: new[] { "IsActive", "IsDisponible" });

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
                name: "IX_Pedidos_Estado",
                table: "Pedidos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_MotorizadoId",
                table: "Pedidos",
                column: "MotorizadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_NegocioId",
                table: "Pedidos",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_NegocioId_Estado",
                table: "Pedidos",
                columns: new[] { "NegocioId", "Estado" });

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
    }
}
