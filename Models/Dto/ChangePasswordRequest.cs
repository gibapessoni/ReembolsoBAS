using System.ComponentModel.DataAnnotations;

namespace ReembolsoBAS.Models.Dto
{
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(6, ErrorMessage = "A nova senha deve ter ao menos 6 caracteres.")]
        public string NewPassword { get; set; } = "";
    }
}
