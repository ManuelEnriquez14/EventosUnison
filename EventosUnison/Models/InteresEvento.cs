namespace EventosUnison.Models;

public class InteresEvento
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public string Respuesta { get; set; } = RespuestasInteres.Si;
    public DateTime FechaRespuesta { get; set; } = DateTime.Now;
}