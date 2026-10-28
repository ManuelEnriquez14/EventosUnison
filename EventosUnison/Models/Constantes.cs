namespace EventosUnison.Models;

public static class Roles
{
    public const string Alumno = "Alumno";
    public const string Profesor = "Profesor";
    public const string Administrador = "Administrador";
}

public static class EstadosEvento
{
    public const string Propuesto = "Propuesto";
    public const string Confirmado = "Confirmado";
    public const string Rechazado = "Rechazado";
}

public static class RespuestasInteres
{
    public const string Si = "Si";
    public const string TalVez = "TalVez";
    public const string No = "No";
}

public static class Culturest
{
    public const double PuntosPorEvento = 0.25;
}

public static class TiposNotificacion
{
    public const string Interes = "Interes";
    public const string Registro = "Registro";
    public const string Estado = "Estado";
}