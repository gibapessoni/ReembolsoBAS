﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ReembolsoBAS.Models.Enums;

public class ReembolsoRequest
{
    [Required] public string Matricula { get; set; } = "";

    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public string Periodo { get; set; } = "";

    [Range(0.01, double.MaxValue)]
    public decimal ValorSolicitado { get; set; }

    /* arrays pareados (mesma quantidade) */
    [MinLength(1)] public string[] Beneficiario { get; set; } = Array.Empty<string>();
    [MinLength(1)] public GrauParentescoEnum[] GrauParentesco { get; set; } = Array.Empty<GrauParentescoEnum>();
    [MinLength(1)] public DateTime[] DataNascimento { get; set; } = Array.Empty<DateTime>();
    [MinLength(1)] public decimal[] ValorPago { get; set; } = Array.Empty<decimal>();
    [MinLength(1)] public TipoSolicitacaoEnum[] TipoSolicitacaoLancamento { get; set; } = Array.Empty<TipoSolicitacaoEnum>();

    /* um arquivo por lançamento (índices casados) */
    public IFormFile[] Documentos { get; set; } = Array.Empty<IFormFile>();

    public bool RemoverDocumento { get; set; } = false;
}
