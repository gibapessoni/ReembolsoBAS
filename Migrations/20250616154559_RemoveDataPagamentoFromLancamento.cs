using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDataPagamentoFromLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empregados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Matricula = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Diretoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Superintendencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    ValorMaximoMensal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empregados", x => x.Id);
                    table.UniqueConstraint("AK_Empregados_Matricula", x => x.Matricula);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasBAS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Revisao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataPublicacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vigente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasBAS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reembolsos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroRegistro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatriculaEmpregado = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TipoSolicitacao = table.Column<short>(type: "smallint", nullable: false),
                    Periodo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MotivoReprovacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaminhoDocumentos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValorSolicitado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorReembolsado = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reembolsos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reembolsos_Empregados_MatriculaEmpregado",
                        column: x => x.MatriculaEmpregado,
                        principalTable: "Empregados",
                        principalColumn: "Matricula",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpregadoId = table.Column<int>(type: "int", nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Perfil = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Empregados_EmpregadoId",
                        column: x => x.EmpregadoId,
                        principalTable: "Empregados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReembolsoLancamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReembolsoId = table.Column<int>(type: "int", nullable: false),
                    Beneficiario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrauParentesco = table.Column<short>(type: "smallint", nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorRestituir = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoSolicitacao = table.Column<short>(type: "smallint", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Reembolsos_MatriculaEmpregado",
                table: "Reembolsos",
                column: "MatriculaEmpregado");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpregadoId",
                table: "Usuarios",
                column: "EmpregadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Matricula",
                table: "Usuarios",
                column: "Matricula",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoliticasBAS");

            migrationBuilder.DropTable(
                name: "ReembolsoLancamentos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Reembolsos");

            migrationBuilder.DropTable(
                name: "Empregados");
        }
    }
}
