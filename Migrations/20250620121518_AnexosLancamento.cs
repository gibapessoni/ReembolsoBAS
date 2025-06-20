using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    /// <inheritdoc />
    public partial class AnexosLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaminhoDocumentos",
                table: "ReembolsoLancamentos");

            migrationBuilder.CreateTable(
                name: "ReembolsoDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReembolsoLancamentoId = table.Column<int>(type: "int", nullable: false),
                    NomeFisico = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomeOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataUpload = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReembolsoDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReembolsoDocumentos_ReembolsoLancamentos_ReembolsoLancamentoId",
                        column: x => x.ReembolsoLancamentoId,
                        principalTable: "ReembolsoLancamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReembolsoDocumentos_ReembolsoLancamentoId",
                table: "ReembolsoDocumentos",
                column: "ReembolsoLancamentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReembolsoDocumentos");

            migrationBuilder.AddColumn<string>(
                name: "CaminhoDocumentos",
                table: "ReembolsoLancamentos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
