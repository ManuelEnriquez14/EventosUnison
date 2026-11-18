namespace EventosUnison.Models;

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? Carrera { get; set; }
    public string Departamento { get; set; } = "";
    public DateTime FechaDePublicacion { get; set; } = DateTime.Now;
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string? ArchivoNombre { get; set; }
    public string? ArchivoRuta { get; set; }
    public long? ArchivoTamano { get; set; }
    public int Orden { get; set; }
}