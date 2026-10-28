using System.Security.Claims;
using EventosUnison.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventosUnison.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly AppDbContext _db;

    public NotificacionesController(AppDbContext db) => _db = db;

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        // AsNoTracking: así conservamos cuáles estaban sin leer para resaltarlas en la vista
        var lista = await _db.Notificaciones
            .AsNoTracking()
            .Include(n => n.Actor)
            .Include(n => n.Evento)
            .Where(n => n.UsuarioId == UsuarioId)
            .OrderByDescending(n => n.Fecha)
            .Take(100)
            .ToListAsync();

        // Al abrir la bandeja, todas pasan a leídas
        await _db.Notificaciones
            .Where(n => n.UsuarioId == UsuarioId && !n.Leida)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));

        return View(lista);
    }

    // Contenido de la ventanita de la campana
    public async Task<IActionResult> Resumen()
    {
        var recientes = await _db.Notificaciones
            .AsNoTracking()
            .Include(n => n.Actor)
            .Include(n => n.Evento)
            .Where(n => n.UsuarioId == UsuarioId)
            .OrderByDescending(n => n.Fecha)
            .Take(6)
            .ToListAsync();

        // Se marcan como leídas solo las que se están mostrando
        var idsNuevas = recientes.Where(n => !n.Leida).Select(n => n.Id).ToList();
        if (idsNuevas.Count > 0)
        {
            await _db.Notificaciones
                .Where(n => idsNuevas.Contains(n.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
        }

        var pendientes = await _db.Notificaciones.CountAsync(n => n.UsuarioId == UsuarioId && !n.Leida);
        Response.Headers["X-Pendientes"] = pendientes.ToString();
        Response.Headers["Cache-Control"] = "no-store";

        return PartialView("_Resumen", recientes);
    }
}