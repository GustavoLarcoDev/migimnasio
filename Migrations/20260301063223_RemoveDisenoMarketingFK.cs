using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gimnasio.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDisenoMarketingFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisenosMarketing_Negocios_NegocioId",
                table: "DisenosMarketing");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_DisenosMarketing_Negocios_NegocioId",
                table: "DisenosMarketing",
                column: "NegocioId",
                principalTable: "Negocios",
                principalColumn: "NegocioId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
