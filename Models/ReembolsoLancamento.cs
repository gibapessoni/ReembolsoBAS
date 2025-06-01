using System.ComponentModel.DataAnnotations.Schema;

namespace ReembolsoBAS.Models
{
    public class ReembolsoLancamento
    {
        public int Id { get; set; }
        public int ReembolsoId { get; set; }
        public Reembolso Reembolso { get; set; } = null!;

        public string Beneficiario { get; set; } = string.Empty;    // “Filho”, “Titular” …
        public string GrauParentesco { get; set; } = string.Empty;  // “Cônjuge”, “Filho” …

        public DateTime DataPagamento { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorPago { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRestituir { get; set; }                 // 50 % de ValorPago
    }
}
