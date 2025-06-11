using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReembolsoBAS.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }

        // -- vínculo numérico (FK) para Empregado.Id
        [Required]
        public int EmpregadoId { get; set; }

        [ForeignKey(nameof(EmpregadoId))]
        public Empregado Empregado { get; set; } = null!;

        // -- mantém também Matrícula para lookup legível
        [Required, StringLength(50)]
        public string Matricula { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Perfil { get; set; } = string.Empty;
    }
}
