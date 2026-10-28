namespace EventosUnison.Models;

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }          // destinatario
    public Usuario? Usuario { get; set; }
    public int ActorId { get; set; }            // quien hizo la acción
    public Usuario? Actor { get; set; }
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public string Tipo { get; set; } = "";
    public string? Detalle { get; set; }
    public bool Leida { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}