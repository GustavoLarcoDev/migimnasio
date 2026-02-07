using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteFieldsToLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Logs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreCliente",
                table: "Logs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "NombreCliente",
                table: "Logs");
        }
    }
}
