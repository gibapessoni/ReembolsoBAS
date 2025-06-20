namespace ReembolsoBAS.Models;

public class ReembolsoDocumento
{
    public int Id { get; set; }

    public int ReembolsoLancamentoId { get; set; }
    public ReembolsoLancamento Lancamento { get; set; } = null!;

    public string NomeFisico { get; set; } = "";
    public string NomeOriginal { get; set; } = "";
    public string ContentType { get; set; } = "";
    public DateTime DataUpload { get; set; } = DateTime.UtcNow;
}
