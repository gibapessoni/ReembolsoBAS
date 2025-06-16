using ReembolsoBAS.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ReembolsoBAS.Models
{
    public class ReembolsoLancamento
    {
        public int Id { get; set; }

        /* FK */
        public int ReembolsoId { get; set; }
        [JsonIgnore] public Reembolso Reembolso { get; set; } = null!;

        /* Dados do lançamento */
        public string Beneficiario { get; set; } = string.Empty;
        public GrauParentescoEnum GrauParentesco { get; set; }
        public DateTime DataNascimento { get; set; }
        public DateTime DataPagamento { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorPago { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRestituir { get; set; }

        /* NOVO CAMPO  ↓    */
        public TipoSolicitacaoEnum TipoSolicitacao { get; set; }
    }
}
