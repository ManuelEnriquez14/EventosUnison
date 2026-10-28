using System.ComponentModel.DataAnnotations;

namespace EventosUnison.Models.ViewModels;

public class ProponerEventoViewModel
{
    [Required(ErrorMessage = "Ingresa el nombre del evento")]
    [StringLength(150)]
    [Display(Name = "Nombre del evento")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresa una descripción")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el departamento")]
    [StringLength(150)]
    public string Departamento { get; set; } = "";

    [Required(ErrorMessage = "Indica la fecha y hora")]
    [Display(Name = "Fecha y hora")]
    public DateTime FechaHora { get; set; }

    [Required(ErrorMessage = "Ingresa el lugar")]
    [StringLength(150)]
    public string Lugar { get; set; } = "";

    [Required(ErrorMessage = "Indica quién organiza")]
    [StringLength(150)]
    [Display(Name = "Organizador (persona u organización)")]
    public string Organizador { get; set; } = "";
}