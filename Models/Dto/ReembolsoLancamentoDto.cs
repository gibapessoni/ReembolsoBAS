namespace ReembolsoBAS.Models.Dto
{
    public class ReembolsoLancamentoDto
    {
        public required string Beneficiario { get; set; }
        public required string GrauParentesco { get; set; }
        public required DateTime DataPagamento { get; set; }
        public required decimal ValorPago { get; set; }
    }
}
