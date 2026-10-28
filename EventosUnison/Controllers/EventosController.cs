using System.Security.Claims;
using EventosUnison.Data;
using EventosUnison.Models;
using EventosUnison.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventosUnison.Models;
using EventosUnison.Models.ViewModels;

namespace EventosUnison.Controllers;

[Authorize]
public class EventosController : Controller
{
    private readonly AppDbContext _db;

    public EventosController(AppDbContext db) => _db = db;

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task NotificarOrganizador(Evento evento, string tipo, string? detalle)
    {
        if (evento.OrganizadorId == UsuarioId) return;   // no se notifica a sí mismo

        Notificacion? existente = null;
        if (tipo == TiposNotificacion.Interes)
        {
            existente = await _db.Notificaciones.FirstOrDefaultAsync(n =>
                n.UsuarioId == evento.OrganizadorId &&
                n.ActorId == UsuarioId &&
                n.EventoId == evento.Id &&
                n.Tipo == TiposNotificacion.Interes);
        }

        if (existente != null)
        {
            existente.Detalle = detalle;
            existente.Leida = false;
            existente.Fecha = DateTime.Now;
        }
        else
        {
            _db.Notificaciones.Add(new Notificacion
            {
                UsuarioId = evento.OrganizadorId,
                ActorId = UsuarioId,
                EventoId = evento.Id,
                Tipo = tipo,
                Detalle = detalle
            });
        }
    }

    // ---------- PROPONER ----------
    [HttpGet]
    public async Task<IActionResult> Proponer()
    {
        var usuario = await _db.Usuarios.FindAsync(UsuarioId);

        var modelo = new ProponerEventoViewModel
        {
            Organizador = usuario?.NombreCompleto ?? "",
            Departamento = usuario?.Departamento ?? "",
            FechaHora = DateTime.Today.AddDays(7).AddHours(10)

        };
        return View(modelo);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Proponer(ProponerEventoViewModel modelo)
    {
        if (modelo.FechaHora <= DateTime.Now)
            ModelState.AddModelError(nameof(modelo.FechaHora), "La fecha debe ser posterior a hoy");

        if (!ModelState.IsValid) return View(modelo);

        var evento = new Evento
        {
            Nombre = modelo.Nombre.Trim(),
            Descripcion = modelo.Descripcion.Trim(),
            Departamento = modelo.Departamento.Trim(),
            FechaHora = modelo.FechaHora,
            Lugar = modelo.Lugar.Trim(),
            Organizador = modelo.Organizador.Trim(),
            OrganizadorId = UsuarioId,
            Estado = EstadosEvento.Propuesto,
            PuntosCulturest = Culturest.PuntosPorEvento
        };

        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync();

        TempData["Exito"] = "Tu propuesta se envió. Un administrador la revisará.";
        return RedirectToAction(nameof(MisPropuestas));
    }

    // ---------- MIS PROPUESTAS ----------
    public async Task<IActionResult> MisPropuestas()
    {
        var propuestas = await _db.Eventos
            .Where(e => e.OrganizadorId == UsuarioId)
            .OrderByDescending(e => e.FechaHora)
            .ToListAsync();

        return View(propuestas);
    }

    // ---------- LISTA DE EVENTOS CONFIRMADOS ----------
    public async Task<IActionResult> Index()
    {
        var ahora = DateTime.Now;


        var eventos = await _db.Eventos
            .Where(e => e.Estado == EstadosEvento.Confirmado && e.FechaHora >= ahora)
            .OrderBy(e => e.FechaHora)
            .ToListAsync();

        var ids = eventos.Select(e => e.Id).ToList();

        var respuestas = await _db.InteresEventos
            .Where(i => i.UsuarioId == UsuarioId && ids.Contains(i.EventoId))
            .ToDictionaryAsync(i => i.EventoId, i => i.Respuesta);

        var registrados = await _db.RegistroEventos
            .Where(r => r.UsuarioId == UsuarioId && ids.Contains(r.EventoId))
            .Select(r => r.EventoId)
            .ToListAsync();

        var modelo = eventos.Select(e => new EventoItemViewModel
        {
            Evento = e,
            MiRespuesta = respuestas.GetValueOrDefault(e.Id),
            Registrado = registrados.Contains(e.Id),
            EsOrganizador = e.OrganizadorId == UsuarioId
        }).ToList();



        return View(modelo);
    }

    // ---------- RESPONDER INTERÉS ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResponderInteres(int id, string respuesta)
    {
        var validas = new[] { RespuestasInteres.Si, RespuestasInteres.TalVez, RespuestasInteres.No };
        if (!validas.Contains(respuesta)) return BadRequest();

        var evento = await _db.Eventos.FindAsync(id);
        if (evento == null || evento.Estado != EstadosEvento.Confirmado) return NotFound();

        if (evento.OrganizadorId == UsuarioId)
        {
            TempData["Error"] = "No puedes responder interés en un evento que tú propusiste.";
            return RedirectToAction(nameof(Index));
        }

        var interes = await _db.InteresEventos
            .FirstOrDefaultAsync(i => i.UsuarioId == UsuarioId && i.EventoId == id);

        if (interes == null)
        {
            _db.InteresEventos.Add(new InteresEvento
            {
                UsuarioId = UsuarioId,
                EventoId = id,
                Respuesta = respuesta,
                FechaRespuesta = DateTime.Now
            });
        }
        else
        {
            interes.Respuesta = respuesta;
            interes.FechaRespuesta = DateTime.Now;
        }
        await NotificarOrganizador(evento, TiposNotificacion.Interes, respuesta);
        await _db.SaveChangesAsync();
        await _db.SaveChangesAsync();
        TempData["Exito"] = $"Guardamos tu respuesta para \"{evento.Nombre}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------- REGISTRARSE A UN EVENTO ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrarme(int id)
    {
        var evento = await _db.Eventos.FindAsync(id);
        if (evento == null || evento.Estado != EstadosEvento.Confirmado) return NotFound();

        if (evento.OrganizadorId == UsuarioId)
        {
            TempData["Error"] = "No puedes registrarte en un evento que tú propusiste.";
            return RedirectToAction(nameof(Index));
        }

        if (evento.FechaHora < DateTime.Now)
        {
            TempData["Error"] = "Ese evento ya pasó.";
            return RedirectToAction(nameof(Index));
        }

        var yaRegistrado = await _db.RegistroEventos
            .AnyAsync(r => r.UsuarioId == UsuarioId && r.EventoId == id);

        if (yaRegistrado)
        {
            TempData["Error"] = "Ya estás registrado en este evento.";
            return RedirectToAction(nameof(Index));
        }

        _db.RegistroEventos.Add(new RegistroEvento
        {
            UsuarioId = UsuarioId,
            EventoId = id,
            FechaRegistro = DateTime.Now
        });

        await NotificarOrganizador(evento, TiposNotificacion.Registro, null);
        await _db.SaveChangesAsync();
        await _db.SaveChangesAsync();

        TempData["Exito"] = $"Quedaste registrado en \"{evento.Nombre}\".";
        return RedirectToAction(nameof(MisRegistros));
    }

    // ---------- MIS REGISTROS ----------
    public async Task<IActionResult> MisRegistros()
    {
        var registros = await _db.RegistroEventos
            .Include(r => r.Evento)
            .Where(r => r.UsuarioId == UsuarioId)
            .OrderBy(r => r.Evento!.FechaHora)
            .ToListAsync();

        return View(registros);
    }

    // ---------- INTERESADOS (organizador y administrador) ----------
    public async Task<IActionResult> Interesados(int id, string? filtro = null)
    {
        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento == null) return NotFound();

        var esAdmin = User.IsInRole(Roles.Administrador);
        if (!esAdmin && evento.OrganizadorId != UsuarioId)
            return Forbid();   // redirige a /Cuenta/AccesoDenegado

        var todas = await _db.InteresEventos
            .Include(i => i.Usuario)
            .Where(i => i.EventoId == id)
            .ToListAsync();

        // Orden: Sí, Tal vez, No; y dentro de cada grupo, por nombre
        var orden = new Dictionary<string, int>
        {
            [RespuestasInteres.Si] = 0,
            [RespuestasInteres.TalVez] = 1,
            [RespuestasInteres.No] = 2
        };

        var filtradas = todas
            .Where(i => filtro == null || i.Respuesta == filtro)
            .OrderBy(i => orden[i.Respuesta])
            .ThenBy(i => i.Usuario!.NombreCompleto)
            .ToList();

        var si = todas.Where(i => i.Respuesta == RespuestasInteres.Si).ToList();

        var modelo = new InteresadosViewModel
        {
            Evento = evento,
            Respuestas = filtradas,
            Filtro = filtro,
            TotalSi = si.Count,
            TotalTalVez = todas.Count(i => i.Respuesta == RespuestasInteres.TalVez),
            TotalNo = todas.Count(i => i.Respuesta == RespuestasInteres.No),
            ProfesoresInteresados = si.Count(i => i.Usuario!.Rol == Roles.Profesor),
            AlumnosInteresados = si.Count(i => i.Usuario!.Rol == Roles.Alumno),
            TotalRegistrados = await _db.RegistroEventos.CountAsync(r => r.EventoId == id)
        };

        return View(modelo);
    }
}