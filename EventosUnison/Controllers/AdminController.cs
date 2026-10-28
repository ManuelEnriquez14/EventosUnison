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
}