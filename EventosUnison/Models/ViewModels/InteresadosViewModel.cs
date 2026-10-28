namespace EventosUnison.Models.ViewModels;

public class InteresadosViewModel
{
    public Evento Evento { get; set; } = null!;
    public List<InteresEvento> Respuestas { get; set; } = new();   // ya filtradas para mostrar

    public int TotalSi { get; set; }
    public int TotalTalVez { get; set; }
    public int TotalNo { get; set; }

    public int ProfesoresInteresados { get; set; }   // respondieron "Sí"
    public int AlumnosInteresados { get; set; }      // respondieron "Sí"
    public int TotalRegistrados { get; set; }

    public string? Filtro { get; set; }
}