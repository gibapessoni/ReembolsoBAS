using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ReembolsoBAS.Models.Enums;

public class ReembolsoRequest
{
    [Required] public string Matricula { get; set; } = "";

    [Required]
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Período deve ser YYYY-MM.")]
    public string Periodo { get; set; } = "";

    [Range(0.01, double.MaxValue)]
    public decimal ValorSolicitado { get; set; }

    [Required, MinLength(1)]
    public DateTime[] DataNascimento { get; set; } = Array.Empty<DateTime>();

    /* ---------- arquivos ---------- */
    public IFormFileCollection? Documentos { get; set; }
    public bool RemoverDocumento { get; set; }

    /* ---------- arrays pareados ---------- */
    [Required, MinLength(1)] public string[] Beneficiario { get; set; } = Array.Empty<string>();
    [Required, MinLength(1)] public GrauParentescoEnum[] GrauParentesco { get; set; } = Array.Empty<GrauParentescoEnum>();
    [Required, MinLength(1)] public DateTime[] DataPagamento { get; set; } = Array.Empty<DateTime>();
    [Required, MinLength(1)] public decimal[] ValorPago { get; set; } = Array.Empty<decimal>();

    /* Tipo *principal* (mantido para compat.) */
    [Required] public TipoSolicitacaoEnum TipoSolicitacao { get; set; }

    /* NOVO – um tipo para cada lançamento */
    [Required, MinLength(1)]
    public TipoSolicitacaoEnum[] TipoSolicitacaoLancamento { get; set; } = Array.Empty<TipoSolicitacaoEnum>();
}
