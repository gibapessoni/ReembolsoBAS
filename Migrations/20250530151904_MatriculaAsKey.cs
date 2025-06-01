using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    /// <inheritdoc />
    public partial class MatriculaAsKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmpregadoMatricula",
                table: "Reembolsos");

            migrationBuilder.AddColumn<string>(
                name: "Matricula",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "MotivoReprovacao",
                table: "Reembolsos",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ReembolsoLancamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReembolsoId = table.Column<int>(type: "int", nullable: false),
                    Beneficiario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrauParentesco = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorRestituir = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReembolsoLancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReembolsoLancamentos_Reembolsos_ReembolsoId",
                        column: x => x.ReembolsoId,
                        principalTable: "Reembolsos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReembolsoLancamentos_ReembolsoId",
                table: "ReembolsoLancamentos",
                column: "ReembolsoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReembolsoLancamentos");

            migrationBuilder.DropColumn(
                name: "Matricula",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "MotivoReprovacao",
                table: "Reembolsos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpregadoMatricula",
                table: "Reembolsos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
