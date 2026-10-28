namespace EventosUnison.Models.ViewModels;

public class EventoItemViewModel
{
    public Evento Evento { get; set; } = null!;
    public string? MiRespuesta { get; set; }   // "Si", "TalVez", "No" o null si aún no responde
    public bool Registrado { get; set; }

    public bool EsOrganizador { get; set; }   // true si el usuario actual propuso este evento
}