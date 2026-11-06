using EventosUnison.Data;
using EventosUnison.Models;
using EventosUnison.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventosUnison.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var eventos = await _db.Eventos
            .Include(e => e.OrganizadorUsuario)
            .OrderBy(e => e.FechaHora)
            .ToListAsync();

        var modelo = new AdminPanelViewModel
        {
            Pendientes = eventos.Where(e => e.Estado == EstadosEvento.Propuesto).ToList(),
            Procesados = eventos.Where(e => e.Estado != EstadosEvento.Propuesto)
                                .OrderByDescending(e => e.FechaHora).ToList()
        };
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, string estado)
    {
        if (estado != EstadosEvento.Confirmado && estado != EstadosEvento.Rechazado)
            return BadRequest();

        var evento = await _db.Eventos.FindAsync(id);
        if (evento == null) return NotFound();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        evento.Estado = estado;

        // Avisar al organizador (si quien propuso el evento no es el mismo admin)
        if (evento.OrganizadorId != adminId)
        {
            _db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = evento.OrganizadorId,
                ActorId = adminId,
                EventoId = evento.Id,
                Tipo = TiposNotificacion.Estado,
                Detalle = estado
            });
        }

        await _db.SaveChangesAsync();

        TempData["Exito"] = estado == EstadosEvento.Confirmado
            ? $"El evento \"{evento.Nombre}\" fue confirmado."
            : $"El evento \"{evento.Nombre}\" fue rechazado.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Asistencia(int id)
    {
        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento == null) return NotFound();

        // 1. Obtener registrados
        var registrados = await _db.RegistroEventos
            .Include(r => r.Usuario)
            .Where(r => r.EventoId == id)
            .OrderBy(r => r.Usuario!.NombreCompleto)
            .ToListAsync();

        var registradosIds = registrados.Select(r => r.UsuarioId).ToList();

        // 2. Obtener interesados (Solo Sí y Tal Vez, omitiendo a los ya registrados)
        var interesados = await _db.InteresEventos
            .Include(i => i.Usuario)
            .Where(i => i.EventoId == id
                        && (i.Respuesta == "Si" || i.Respuesta == "TalVez")
                        && !registradosIds.Contains(i.UsuarioId))
            .OrderBy(i => i.Respuesta) // Saldrán primero los Sí, luego Tal Vez
            .ThenBy(i => i.Usuario!.NombreCompleto)
            .ToListAsync();

        var modelo = new AsistenciaViewModel
        {
            Evento = evento,
            Registrados = registrados,
            Interesados = interesados
        };

        return View(modelo);
    }

    // ---------- MARCAR ASISTENCIA ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarAsistencia(int registroId, int eventoId, bool asistio)
    {
        var registro = await _db.RegistroEventos.FindAsync(registroId);
        if (registro == null) return NotFound();

        registro.Asistio = asistio;
        await _db.SaveChangesAsync();

        TempData["Exito"] = asistio ? "Se marcó la asistencia correctamente." : "Se marcó la inasistencia.";

        return RedirectToAction(nameof(Asistencia), new { id = eventoId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarAsistenciaInteresado(int interesId, int eventoId, bool asistio)
    {
        var interes = await _db.InteresEventos.FindAsync(interesId);
        if (interes == null) return NotFound();

        interes.Asistio = asistio;
        await _db.SaveChangesAsync();

        TempData["Exito"] = asistio ? "Se marcó la asistencia del interesado." : "Se marcó la inasistencia del interesado.";

        return RedirectToAction(nameof(Asistencia), new { id = eventoId });
    }
}