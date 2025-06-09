using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

public class ReembolsoRequest
{
    [Required]
    public string Matricula { get; set; } = "";

    [Required]
    public DateTime Periodo { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal ValorSolicitado { get; set; }

    [Required]
    public IFormFileCollection Documentos { get; set; } = null!;

    [Required, MinLength(1)]
    public string[] Beneficiario { get; set; } = Array.Empty<string>();

    [Required, MinLength(1)]
    public string[] GrauParentesco { get; set; } = Array.Empty<string>();

    [Required, MinLength(1)]
    public DateTime[] DataPagamento { get; set; } = Array.Empty<DateTime>();

    [Required, MinLength(1)]
    public decimal[] ValorPago { get; set; } = Array.Empty<decimal>();
}
