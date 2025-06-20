using ReembolsoBAS.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace ReembolsoBAS.Models.Dto
{
    public class LancamentoEditDto
    {
        public int? Id { get; set; }      // null  → novo
        public string Beneficiario { get; set; } = "";
        public GrauParentescoEnum GrauParentesco { get; set; }
        public DateTime DataNascimento { get; set; }
        public decimal ValorPago { get; set; }
        public TipoSolicitacaoEnum TipoSolicitacao { get; set; }

        /* anexos novos (opcional) */
        public IFormFileCollection? NovosArquivos { get; set; }
    }

    public class ReembolsoEditRequest
    {
        public string Periodo { get; set; } = "";   // YYYY-MM
        public decimal ValorSolicitado { get; set; }

        public List<LancamentoEditDto> Lancamentos { get; set; } = [];
    }

}
