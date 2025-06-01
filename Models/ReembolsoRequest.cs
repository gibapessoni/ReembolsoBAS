using System.ComponentModel.DataAnnotations;

public class ReembolsoRequest
{
    [Required]
    public required string Matricula { get; set; } 

    [Required]
    public DateTime Periodo { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal ValorSolicitado { get; set; }

    [Required]
    public required IFormFileCollection Documentos { get; set; } 
}