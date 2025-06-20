using ReembolsoBAS.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ReembolsoBAS.Models
{
    public class ReembolsoLancamento
    {
        public int Id { get; set; }
        public int ReembolsoId { get; set; }
        public Reembolso Reembolso { get; set; } = null!;

        /* ---- dados do lançamento ---- */
        public string Beneficiario { get; set; } = "";
        public GrauParentescoEnum GrauParentesco { get; set; }
        public DateTime DataNascimento { get; set; }
        public decimal ValorPago { get; set; }
        public decimal ValorRestituir { get; set; }
        public TipoSolicitacaoEnum TipoSolicitacao { get; set; }

        /* ---- NOVO: anexos ---- */
        public ICollection<ReembolsoDocumento> Documentos { get; set; } = new List<ReembolsoDocumento>();
    }
}
