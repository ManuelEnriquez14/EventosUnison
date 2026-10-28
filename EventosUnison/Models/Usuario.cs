namespace EventosUnison.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string CorreoInstitucional { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = Roles.Alumno;
    public string Departamento { get; set; } = "";
    public string? Carrera { get; set; }   // null para profesores y administradores
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}