namespace EventosUnison.Models;

public class RegistroEvento
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public bool? Asistio { get; set; }
}