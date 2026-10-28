using System.Security.Claims;
using EventosUnison.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventosUnison.ViewComponents;

public class NotificacionesViewComponent : ViewComponent
{
    private readonly AppDbContext _db;

    public NotificacionesViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
            return Content("");

        var id = int.Parse(UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var noLeidas = await _db.Notificaciones.CountAsync(n => n.UsuarioId == id && !n.Leida);
        return View(noLeidas);
    }
}