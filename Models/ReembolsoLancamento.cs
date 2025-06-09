using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ReembolsoBAS.Models
{
    public class ReembolsoLancamento
    {
        public int Id { get; set; }
        public int ReembolsoId { get; set; }

        [JsonIgnore]
        public Reembolso Reembolso { get; set; } = null!;

        public string Beneficiario { get; set; } = string.Empty;
        public string GrauParentesco { get; set; } = string.Empty;
        public DateTime DataPagamento { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorPago { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRestituir { get; set; }
    }
}
