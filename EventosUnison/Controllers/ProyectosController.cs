using System.Security.Claims;
using EventosUnison.Data;
using EventosUnison.Models;
using EventosUnison.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;

namespace EventosUnison.Controllers;

[Authorize]
public class ProyectosController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] ExtensionesPermitidas =
        { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".zip", ".png", ".jpg", ".jpeg" };

    private const long TamanoMaximo = 10 * 1024 * 1024;   // 10 MB

    public ProyectosController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private string CarpetaArchivos => Path.Combine(_env.ContentRootPath, "Uploads", "Proyectos");

    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---------- CONSULTAR ----------
    public async Task<IActionResult> Index(string? q, bool mios = false)
    {
        var consulta = _db.Proyectos
            .Include(p => p.Usuario)
            .AsQueryable();

        if (mios)
            consulta = consulta.Where(p => p.UsuarioId == UsuarioId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            consulta = consulta.Where(p =>
                p.Nombre.Contains(q) ||
                p.Descripcion.Contains(q) ||
                p.Departamento.Contains(q) ||
                (p.Carrera != null && p.Carrera.Contains(q)) ||
                p.Usuario!.NombreCompleto.Contains(q));
        }

        var modelo = new ProyectosIndexViewModel
        {
            Proyectos = await consulta
                .OrderByDescending(p => p.FechaDePublicacion)
                .ToListAsync(),
            Q = q,
            Mios = mios
        };

        return View(modelo);
    }

    // ---------- PUBLICAR ----------
    [HttpGet]
    public async Task<IActionResult> Publicar()
    {
        var usuario = await _db.Usuarios.FindAsync(UsuarioId);

        ViewBag.Responsable = usuario?.NombreCompleto;
        return View(new PublicarProyectoViewModel
        {
            Departamento = usuario?.Departamento ?? "",
            Carrera = usuario?.Carrera
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Publicar(PublicarProyectoViewModel modelo)
    {
        string? extension = null;

        if (modelo.Archivo != null && modelo.Archivo.Length > 0)
        {
            extension = Path.GetExtension(modelo.Archivo.FileName).ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
                ModelState.AddModelError(nameof(modelo.Archivo),
                    "Tipo de archivo no permitido. Usa PDF, Word, PowerPoint, Excel, ZIP o imagen (PNG/JPG).");

            if (modelo.Archivo.Length > TamanoMaximo)
                ModelState.AddModelError(nameof(modelo.Archivo), "El archivo no puede pesar más de 10 MB.");
        }

        if (!ModelState.IsValid)
        {
            var usuario = await _db.Usuarios.FindAsync(UsuarioId);
            ViewBag.Responsable = usuario?.NombreCompleto;
            return View(modelo);
        }

        var proyecto = new Proyecto
        {
            Nombre = modelo.Nombre.Trim(),
            Descripcion = modelo.Descripcion.Trim(),
            Departamento = modelo.Departamento.Trim(),
            Carrera = string.IsNullOrWhiteSpace(modelo.Carrera) ? null : modelo.Carrera.Trim(),
            FechaDePublicacion = DateTime.Now,
            UsuarioId = UsuarioId
        };

        string? rutaCompleta = null;

        if (modelo.Archivo != null && modelo.Archivo.Length > 0)
        {
            Directory.CreateDirectory(CarpetaArchivos);

            var nombreGuardado = $"{Guid.NewGuid():N}{extension}";
            rutaCompleta = Path.Combine(CarpetaArchivos, nombreGuardado);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await modelo.Archivo.CopyToAsync(stream);
            }

            var nombreOriginal = Path.GetFileName(modelo.Archivo.FileName);
            if (nombreOriginal.Length > 255) nombreOriginal = nombreOriginal[^255..];

            proyecto.ArchivoNombre = nombreOriginal;
            proyecto.ArchivoRuta = nombreGuardado;
            proyecto.ArchivoTamano = modelo.Archivo.Length;
        }

        try
        {
            _db.Proyectos.Add(proyecto);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Si falla la base de datos, no dejamos el archivo huérfano en el disco
            if (rutaCompleta != null && System.IO.File.Exists(rutaCompleta))
                System.IO.File.Delete(rutaCompleta);
            throw;
        }

        TempData["Exito"] = $"Tu proyecto \"{proyecto.Nombre}\" fue publicado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Descargar(int id)
    {
        var proyecto = await _db.Proyectos.FindAsync(id);
        if (proyecto?.ArchivoRuta == null) return NotFound();

        var ruta = Path.Combine(CarpetaArchivos, proyecto.ArchivoRuta);
        if (!System.IO.File.Exists(ruta)) return NotFound();

        var proveedor = new FileExtensionContentTypeProvider();
        if (!proveedor.TryGetContentType(proyecto.ArchivoNombre ?? proyecto.ArchivoRuta, out var tipo))
            tipo = "application/octet-stream";

        return PhysicalFile(ruta, tipo, proyecto.ArchivoNombre);
    }
}