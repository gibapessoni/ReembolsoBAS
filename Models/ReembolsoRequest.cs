﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ReembolsoBAS.Models.Enums;
using System.Globalization;


public class ReembolsoRequest
{
    [Required] public string Matricula { get; set; } = "";
    [Required]
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$",
       ErrorMessage = "Período deve ser no formato YYYY-MM.")]
    public string Periodo { get; set; } = "";

    [Range(0.01, double.MaxValue)]
    public decimal ValorSolicitado { get; set; }

    public IFormFileCollection? Documentos { get; set; }

    [Required, MinLength(1)]
    public string[] Beneficiario { get; set; } = Array.Empty<string>();

    [Required, MinLength(1)]
    public DateTime[] DataPagamento { get; set; } = Array.Empty<DateTime>();

    [Required, MinLength(1)]
    public decimal[] ValorPago { get; set; } = Array.Empty<decimal>();

    [Required] public TipoSolicitacaoEnum TipoSolicitacao { get; set; }

    [Required, MinLength(1)]
    public GrauParentescoEnum[] GrauParentesco { get; set; }
    public bool RemoverDocumento { get; set; }
}
