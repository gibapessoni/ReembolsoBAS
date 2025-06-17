using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCaminhoDocumentosFromReembolso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaminhoDocumentos",
                table: "Reembolsos");

            migrationBuilder.AddColumn<string>(
                name: "CaminhoDocumentos",
                table: "ReembolsoLancamentos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaminhoDocumentos",
                table: "ReembolsoLancamentos");

            migrationBuilder.AddColumn<string>(
                name: "CaminhoDocumentos",
                table: "Reembolsos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
