using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EventosUnison.Models.ViewModels;

public class PublicarProyectoViewModel
{
    [Required(ErrorMessage = "Ingresa el nombre del proyecto")]
    [StringLength(150)]
    [Display(Name = "Nombre del proyecto")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresa una descripción")]
    [StringLength(1000, ErrorMessage = "Máximo 1000 caracteres")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el departamento")]
    [StringLength(150)]
    public string Departamento { get; set; } = "";

    [StringLength(150)]
    public string? Carrera { get; set; }

    [Display(Name = "Archivo adjunto (opcional)")]
    public IFormFile? Archivo { get; set; }
}