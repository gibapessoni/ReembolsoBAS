using System.ComponentModel.DataAnnotations;

public class EmpregadoCreateDto
{
    [Required] public string Matricula { get; set; } = "";
    [Required] public string Nome { get; set; } = "";
    [Required] public string Diretoria { get; set; } = "";
    [Required] public string Superintendencia { get; set; } = "";
    [Required] public string Cargo { get; set; } = "";
    public bool Ativo { get; set; } = true;

    // colaborador | rh | gerente-rh | admin
    [Required, RegularExpression("^(colaborador|rh|gerente-rh|admin)$")]
    public string Perfil { get; set; } = "colaborador";
}
