using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    /// <inheritdoc />
    public partial class AddReembolsoLancamentosFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Matricula",
                table: "Usuarios",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Matricula",
                table: "Usuarios",
                column: "Matricula",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Empregados_Matricula",
                table: "Usuarios",
                column: "Matricula",
                principalTable: "Empregados",
                principalColumn: "Matricula",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Empregados_Matricula",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Matricula",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Matricula",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
