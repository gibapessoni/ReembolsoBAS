using ReembolsoBAS.Models.Enums;

public record ReembolsoDto(
    int Id,
    string NumeroRegistro,
    string Solicitante,
    DateTime DataNascimento,
    DateTime Periodo,
    TipoSolicitacaoEnum TipoSolicitacaoLancamento,
    string Status,
    decimal ValorSolicitado,
    decimal ValorReembolsado);

