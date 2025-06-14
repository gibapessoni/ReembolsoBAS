﻿using ReembolsoBAS.Models;
using ReembolsoBAS.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

public class Reembolso
{
    public int Id { get; set; }
    public string NumeroRegistro { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

    public string MatriculaEmpregado { get; set; } = null!;
    public TipoSolicitacaoEnum TipoSolicitacao { get; set; }

    public DateTime Periodo { get; set; }
    public string Status { get; set; } = StatusReembolso.Pendente;
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    public string? MotivoReprovacao { get; set; } 
    public string CaminhoDocumentos { get; set; } = string.Empty;
    public Empregado Empregado { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorSolicitado { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ValorReembolsado { get; set; } = 0;
    public ICollection<ReembolsoLancamento> Lancamentos { get; set; } = new List<ReembolsoLancamento>();

}