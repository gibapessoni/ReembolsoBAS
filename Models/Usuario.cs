using Microsoft.EntityFrameworkCore;

namespace ReembolsoBAS.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string SenhaHash { get; set; } = string.Empty;

        public string Perfil { get; set; } = string.Empty;

        // A Matrícula que vinculada ao Empregado
        public string Matricula { get; set; } = string.Empty;

        // Navegação para o Empregado correspondente
        public Empregado? Empregado { get; set; }
    }
}
