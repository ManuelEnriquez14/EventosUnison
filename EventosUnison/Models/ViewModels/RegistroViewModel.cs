using System.ComponentModel.DataAnnotations;

namespace EventosUnison.Models.ViewModels;

public class RegistroViewModel
{
    [Required(ErrorMessage = "Ingresa tu nombre completo")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu correo institucional")]
    [EmailAddress(ErrorMessage = "Correo no válido")]
    [Display(Name = "Correo institucional")]
    public string CorreoInstitucional { get; set; } = "";

    [Required(ErrorMessage = "Ingresa una contraseña")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirma tu contraseña")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarPassword { get; set; } = "";

    [Required(ErrorMessage = "Selecciona tu rol")]
    public string Rol { get; set; } = Roles.Alumno;

    [Required(ErrorMessage = "Ingresa tu departamento")]
    [StringLength(150)]
    public string Departamento { get; set; } = "";

    [StringLength(150)]
    public string? Carrera { get; set; }   // solo obligatoria para alumnos
}