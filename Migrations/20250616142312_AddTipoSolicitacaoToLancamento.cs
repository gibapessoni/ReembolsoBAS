using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoSolicitacaoToLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "TipoSolicitacao",
                table: "ReembolsoLancamentos",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoSolicitacao",
                table: "ReembolsoLancamentos");
        }
    }
}
