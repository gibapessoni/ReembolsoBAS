namespace ReembolsoBAS.Models
{
    public class PoliticaBAS
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; 
        public string Revisao { get; set; } = string.Empty;
        public DateTime DataPublicacao { get; set; }
        public string CaminhoArquivo { get; set; } = string.Empty;
        public bool Vigente { get; set; }
    }
}
