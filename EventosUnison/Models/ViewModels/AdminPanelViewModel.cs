namespace EventosUnison.Models.ViewModels;

public class AdminPanelViewModel
{
    public List<Evento> Pendientes { get; set; } = new();
    public List<Evento> Procesados { get; set; } = new();
}