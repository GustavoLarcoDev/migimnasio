using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddModeloArtesanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoNegocio",
                table: "Negocios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Asignar valor por defecto a negocios existentes
            migrationBuilder.Sql("UPDATE Negocios SET TipoNegocio = 'membresias' WHERE TipoNegocio IS NULL");

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Especialidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.EmpleadoId);
                    table.ForeignKey(
                        name: "FK_Empleados_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiciosNegocio",
                columns: table => new
                {
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ItemsIncluidos = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EsCombo = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaDeActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosNegocio", x => x.ServicioId);
                    table.ForeignKey(
                        name: "FK_ServiciosNegocio_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorariosEmpleado",
                columns: table => new
                {
                    HorarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraInicio = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    HoraFin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosEmpleado", x => x.HorarioId);
                    table.ForeignKey(
                        name: "FK_HorariosEmpleado_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorariosExcepcion",
                columns: table => new
                {
                    ExcepcionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsDiaLibre = table.Column<bool>(type: "bit", nullable: false),
                    HoraInicio = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    HoraFin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosExcepcion", x => x.ExcepcionId);
                    table.ForeignKey(
                        name: "FK_HorariosExcepcion_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    CitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreEmpleado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreServicio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrecioServicio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaHoraInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordatorioEnviado = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaDeActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.CitaId);
                    table.ForeignKey(
                        name: "FK_Citas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "ClienteId");
                    table.ForeignKey(
                        name: "FK_Citas_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId");
                    table.ForeignKey(
                        name: "FK_Citas_Negocios_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocios",
                        principalColumn: "NegocioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Citas_ServiciosNegocio_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "ServiciosNegocio",
                        principalColumn: "ServicioId");
                });

            migrationBuilder.CreateTable(
                name: "PagosCita",
                columns: table => new
                {
                    PagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoServicio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoExtra = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DetalleExtra = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Propina = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EsRegalo = table.Column<bool>(type: "bit", nullable: false),
                    MotivoRegalo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NombreCliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombreServicio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosCita", x => x.PagoId);
                    table.ForeignKey(
                        name: "FK_PagosCita_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "CitaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ClienteId",
                table: "Citas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_EmpleadoId_Horario",
                table: "Citas",
                columns: new[] { "EmpleadoId", "FechaHoraInicio", "FechaHoraFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_NegocioId_Estado",
                table: "Citas",
                columns: new[] { "NegocioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_NegocioId_FechaHoraInicio",
                table: "Citas",
                columns: new[] { "NegocioId", "FechaHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_Recordatorio",
                table: "Citas",
                columns: new[] { "RecordatorioEnviado", "Estado", "FechaHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ServicioId",
                table: "Citas",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_NegocioId",
                table: "Empleados",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosEmpleado_EmpleadoId_Dia",
                table: "HorariosEmpleado",
                columns: new[] { "EmpleadoId", "DiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosExcepcion_EmpleadoId_Fecha",
                table: "HorariosExcepcion",
                columns: new[] { "EmpleadoId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosCita_CitaId_Unique",
                table: "PagosCita",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosNegocio_NegocioId",
                table: "ServiciosNegocio",
                column: "NegocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosEmpleado");

            migrationBuilder.DropTable(
                name: "HorariosExcepcion");

            migrationBuilder.DropTable(
                name: "PagosCita");

            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "ServiciosNegocio");

            migrationBuilder.DropColumn(
                name: "TipoNegocio",
                table: "Negocios");
        }
    }
}
