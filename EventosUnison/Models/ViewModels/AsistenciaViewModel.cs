namespace EventosUnison.Models.ViewModels;

public class AsistenciaViewModel
{
    public Evento? Evento { get; set; }
    public List<RegistroEvento> Registrados { get; set; } = new();
    public List<InteresEvento> Interesados { get; set; } = new();
}