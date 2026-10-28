namespace EventosUnison.Models;

public class Evento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Departamento { get; set; } = "";
    public DateTime FechaHora { get; set; }
    public string Lugar { get; set; } = "";
    public string Organizador { get; set; } = "";      // texto: "Difusión Cultural", "Prof. Álvaro"...
    public int OrganizadorId { get; set; }             // usuario que propuso el evento
    public Usuario? OrganizadorUsuario { get; set; }
    public string Estado { get; set; } = EstadosEvento.Propuesto;

    public double PuntosCulturest { get; set; } = Culturest.PuntosPorEvento;
}