using ReembolsoBAS.Models.Enums;

namespace ReembolsoBAS.Models.Dto
{
    public record ReembolsoDto(
    int Id,
    string NumeroRegistro,
    string Solicitante,
    DateTime DataNascimento,
    DateTime Periodo,
    TipoSolicitacaoEnum TipoSolicitacao,
    string Status,
    decimal ValorSolicitado,
    decimal ValorReembolsado);
}
