using Microsoft.EntityFrameworkCore;
namespace ReembolsoBAS.Models
{
    public static class StatusReembolso
    {
        public const string Pendente = "Pendente";
        public const string ValidadoRH = "Validado pelo RH";
        public const string Aprovado = "Aprovado";
        public const string Reprovado = "Reprovado";
        public const string DevolvidoRH = "Devolvido pelo RH";
        public const string RecusadoPrazo = "Recusado por Prazo";
    }
}