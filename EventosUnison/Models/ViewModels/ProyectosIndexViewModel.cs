namespace EventosUnison.Models.ViewModels;

public class ProyectosIndexViewModel
{
    public List<Proyecto> Proyectos { get; set; } = new();
    public string? Q { get; set; }
    public bool Mios { get; set; }
}