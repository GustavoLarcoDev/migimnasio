using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class RenameGimnasioToNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Gimnasios_GymGimnasioId",
                table: "Clientes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Gimnasios",
                table: "Gimnasios");

            migrationBuilder.RenameTable(
                name: "Gimnasios",
                newName: "Negocios");

            migrationBuilder.RenameColumn(
                name: "GimnasioId",
                table: "Notificaciones",
                newName: "NegocioId");

            migrationBuilder.RenameColumn(
                name: "GimnasioId",
                table: "Logs",
                newName: "NegocioId");

            migrationBuilder.RenameColumn(
                name: "GymGimnasioId",
                table: "Clientes",
                newName: "GymNegocioId");

            migrationBuilder.RenameColumn(
                name: "GimnasioId",
                table: "Clientes",
                newName: "NegocioId");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_GymGimnasioId",
                table: "Clientes",
                newName: "IX_Clientes_GymNegocioId");

            migrationBuilder.RenameColumn(
                name: "GimnasioNombre",
                table: "Negocios",
                newName: "NegocioNombre");

            migrationBuilder.RenameColumn(
                name: "DuenoGimnasio",
                table: "Negocios",
                newName: "DuenoNegocio");

            migrationBuilder.RenameColumn(
                name: "GimnasioId",
                table: "Negocios",
                newName: "NegocioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Negocios",
                table: "Negocios",
                column: "NegocioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Negocios_GymNegocioId",
                table: "Clientes",
                column: "GymNegocioId",
                principalTable: "Negocios",
                principalColumn: "NegocioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Negocios_GymNegocioId",
                table: "Clientes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Negocios",
                table: "Negocios");

            migrationBuilder.RenameTable(
                name: "Negocios",
                newName: "Gimnasios");

            migrationBuilder.RenameColumn(
                name: "NegocioId",
                table: "Notificaciones",
                newName: "GimnasioId");

            migrationBuilder.RenameColumn(
                name: "NegocioId",
                table: "Logs",
                newName: "GimnasioId");

            migrationBuilder.RenameColumn(
                name: "NegocioId",
                table: "Clientes",
                newName: "GimnasioId");

            migrationBuilder.RenameColumn(
                name: "GymNegocioId",
                table: "Clientes",
                newName: "GymGimnasioId");

            migrationBuilder.RenameIndex(
                name: "IX_Clientes_GymNegocioId",
                table: "Clientes",
                newName: "IX_Clientes_GymGimnasioId");

            migrationBuilder.RenameColumn(
                name: "NegocioNombre",
                table: "Gimnasios",
                newName: "GimnasioNombre");

            migrationBuilder.RenameColumn(
                name: "DuenoNegocio",
                table: "Gimnasios",
                newName: "DuenoGimnasio");

            migrationBuilder.RenameColumn(
                name: "NegocioId",
                table: "Gimnasios",
                newName: "GimnasioId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Gimnasios",
                table: "Gimnasios",
                column: "GimnasioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Gimnasios_GymGimnasioId",
                table: "Clientes",
                column: "GymGimnasioId",
                principalTable: "Gimnasios",
                principalColumn: "GimnasioId");
        }
    }
}
