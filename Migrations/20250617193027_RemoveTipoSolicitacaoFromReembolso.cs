using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace ReembolsoBAS.Migrations
{
    public partial class RemoveTipoSolicitacaoFromReembolso : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ↓ única operação necessária
            migrationBuilder.DropColumn(
                name: "TipoSolicitacao",
                table: "Reembolsos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // recria a coluna se fizer rollback
            migrationBuilder.AddColumn<short>(
                name: "TipoSolicitacao",
                table: "Reembolsos",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }
    }
}
