using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReembolsoBAS.Migrations
{
    public partial class RemoveDataPagamentoFromLancamento : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataPagamento",
                table: "ReembolsoLancamentos");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataPagamento",
                table: "ReembolsoLancamentos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1900, 1, 1));
        }
    }
}
