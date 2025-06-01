
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
        public string Matricula { get; set; } = string.Empty;
    }
}