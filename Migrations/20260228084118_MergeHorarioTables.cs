using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class MergeHorarioTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the new unified Horarios table
            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    HorarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoHorario = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoraInicio = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    HoraFin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    EsDiaLibre = table.Column<bool>(type: "bit", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.HorarioId);
                    table.ForeignKey(
                        name: "FK_Horarios_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_EmpleadoId_DiaSemana",
                table: "Horarios",
                columns: new[] { "EmpleadoId", "DiaSemana" },
                unique: true,
                filter: "[TipoHorario] = 'regular' AND [DiaSemana] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Horarios_EmpleadoId_Fecha",
                table: "Horarios",
                columns: new[] { "EmpleadoId", "Fecha" },
                unique: true,
                filter: "[TipoHorario] = 'excepcion' AND [Fecha] IS NOT NULL");

            // Step 2: Copy data from old HorariosEmpleado to Horarios (TipoHorario='regular')
            migrationBuilder.Sql(@"
                INSERT INTO [Horarios] ([HorarioId], [EmpleadoId], [NegocioId], [TipoHorario], [DiaSemana], [Fecha], [HoraInicio], [HoraFin], [Activo], [EsDiaLibre], [Motivo])
                SELECT [HorarioId], [EmpleadoId], [NegocioId], 'regular', [DiaSemana], NULL, [HoraInicio], [HoraFin], [Activo], 0, NULL
                FROM [HorariosEmpleado]
            ");

            // Step 3: Copy data from old HorariosExcepcion to Horarios (TipoHorario='excepcion')
            migrationBuilder.Sql(@"
                INSERT INTO [Horarios] ([HorarioId], [EmpleadoId], [NegocioId], [TipoHorario], [DiaSemana], [Fecha], [HoraInicio], [HoraFin], [Activo], [EsDiaLibre], [Motivo])
                SELECT [ExcepcionId], [EmpleadoId], [NegocioId], 'excepcion', NULL, [Fecha], [HoraInicio], [HoraFin], 1, [EsDiaLibre], [Motivo]
                FROM [HorariosExcepcion]
            ");

            // Step 4: Drop old tables (data has been migrated)
            migrationBuilder.DropTable(
                name: "HorariosExcepcion");

            migrationBuilder.DropTable(
                name: "HorariosEmpleado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate old tables
            migrationBuilder.CreateTable(
                name: "HorariosEmpleado",
                columns: table => new
                {
                    HorarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    HoraFin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    HoraInicio = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    EsDiaLibre = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoraFin = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    HoraInicio = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NegocioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            // Copy data back from Horarios to old tables
            migrationBuilder.Sql(@"
                INSERT INTO [HorariosEmpleado] ([HorarioId], [EmpleadoId], [NegocioId], [DiaSemana], [HoraInicio], [HoraFin], [Activo])
                SELECT [HorarioId], [EmpleadoId], [NegocioId], [DiaSemana], [HoraInicio], [HoraFin], [Activo]
                FROM [Horarios]
                WHERE [TipoHorario] = 'regular'
            ");

            migrationBuilder.Sql(@"
                INSERT INTO [HorariosExcepcion] ([ExcepcionId], [EmpleadoId], [NegocioId], [Fecha], [EsDiaLibre], [HoraInicio], [HoraFin], [Motivo])
                SELECT [HorarioId], [EmpleadoId], [NegocioId], [Fecha], [EsDiaLibre], [HoraInicio], [HoraFin], [Motivo]
                FROM [Horarios]
                WHERE [TipoHorario] = 'excepcion'
            ");

            // Recreate old indexes
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

            // Drop the unified table
            migrationBuilder.DropTable(
                name: "Horarios");
        }
    }
}
