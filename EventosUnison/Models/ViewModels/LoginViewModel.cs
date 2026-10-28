using System.ComponentModel.DataAnnotations;

namespace EventosUnison.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo institucional")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [Display(Name = "Correo institucional")]
    public string CorreoInstitucional { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contraseña")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
}