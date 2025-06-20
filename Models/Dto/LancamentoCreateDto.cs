// Models/Dto/LancamentoCreateDto.cs
using ReembolsoBAS.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ReembolsoBAS.Models.Dto;

public class LancamentoCreateDto          // <- igual ao de edição, sem Id
{
    [Required] public string Beneficiario { get; set; } = "";
    [Required] public GrauParentescoEnum GrauParentesco { get; set; }
    [Required] public DateTime DataNascimento { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal ValorPago { get; set; }
    [Required] public TipoSolicitacaoEnum TipoSolicitacao { get; set; }

    // Pelo menos 1 arquivo por lançamento
    [Required, MinLength(1)]
    public IFormFileCollection Arquivos { get; set; } = null!;
}

