using System.ComponentModel.DataAnnotations.Schema;

namespace ReembolsoBAS.Models
{
    public class Empregado
    {
        public int Id { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Diretoria { get; set; } = string.Empty;
        public string Superintendencia { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorMaximoMensal { get; set; }

        // (Opcional) navegação inversa se você quiser
        // public Usuario? Usuario { get; set; }
    }
}
