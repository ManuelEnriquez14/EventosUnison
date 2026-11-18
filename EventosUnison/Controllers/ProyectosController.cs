using System.Security.Claims;
using EventosUnison.Data;
using EventosUnison.Models;
using EventosUnison.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

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



        var ordenada = mios
        ? consulta.OrderBy(p => p.Orden).ThenBy(p => p.Id)
        : consulta.OrderByDescending(p => p.FechaDePublicacion);

        var modelo = new ProyectosIndexViewModel
        {
            Proyectos = await ordenada.ToListAsync(),
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
            // Ya no calculamos Orden aquí; el SP se encarga
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
            var parametroId = new SqlParameter
            {
                ParameterName = "@Id",
                SqlDbType = SqlDbType.Int,
                Direction = ParameterDirection.Output
            };

            await _db.Database.ExecuteSqlRawAsync(
                "EXEC Proyectos_Insertar @Nombre, @Descripcion, @Departamento, @Carrera, @FechaDePublicacion, @UsuarioId, @ArchivoNombre, @ArchivoRuta, @ArchivoTamano, @Id OUTPUT",
                new SqlParameter("@Nombre", proyecto.Nombre),
                new SqlParameter("@Descripcion", proyecto.Descripcion),
                new SqlParameter("@Departamento", proyecto.Departamento),
                new SqlParameter("@Carrera", (object?)proyecto.Carrera ?? DBNull.Value),
                new SqlParameter("@FechaDePublicacion", proyecto.FechaDePublicacion),
                new SqlParameter("@UsuarioId", proyecto.UsuarioId),
                new SqlParameter("@ArchivoNombre", (object?)proyecto.ArchivoNombre ?? DBNull.Value),
                new SqlParameter("@ArchivoRuta", (object?)proyecto.ArchivoRuta ?? DBNull.Value),
                new SqlParameter("@ArchivoTamano", (object?)proyecto.ArchivoTamano ?? DBNull.Value),
                parametroId
            );

            proyecto.Id = (int)parametroId.Value;
        }
        catch
        {
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

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var proyecto = await _db.Proyectos.FindAsync(id);
        if (proyecto == null) return NotFound();                 // → página «recurso no encontrado»
        if (proyecto.UsuarioId != UsuarioId) return Forbid();    // → «Acceso denegado»

        CargarDatosVista(proyecto);
        return View(new EditarProyectoViewModel
        {
            Id = proyecto.Id,
            Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion,
            Departamento = proyecto.Departamento,
            Carrera = proyecto.Carrera,
            ArchivoNombre = proyecto.ArchivoNombre,
            ArchivoTamano = proyecto.ArchivoTamano
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Editar(EditarProyectoViewModel modelo)
    {
        // Se vuelve a buscar y a comprobar el dueño en el servidor: no se confía en lo que envía el formulario
        var proyecto = await _db.Proyectos.FindAsync(modelo.Id);
        if (proyecto == null) return NotFound();
        if (proyecto.UsuarioId != UsuarioId) return Forbid();

        string? extension = null;
        var hayArchivoNuevo = modelo.Archivo != null && modelo.Archivo.Length > 0;

        if (hayArchivoNuevo)
        {
            extension = Path.GetExtension(modelo.Archivo!.FileName).ToLowerInvariant();

            if (!ExtensionesPermitidas.Contains(extension))
                ModelState.AddModelError(nameof(modelo.Archivo),
                    "Tipo de archivo no permitido. Usa PDF, Word, PowerPoint, Excel, ZIP o imagen (PNG/JPG).");

            if (modelo.Archivo.Length > TamanoMaximo)
                ModelState.AddModelError(nameof(modelo.Archivo), "El archivo no puede pesar más de 10 MB.");
        }

        if (!ModelState.IsValid)
        {
            modelo.ArchivoNombre = proyecto.ArchivoNombre;
            modelo.ArchivoTamano = proyecto.ArchivoTamano;
            CargarDatosVista(proyecto);
            return View(modelo);
        }

        proyecto.Nombre = modelo.Nombre.Trim();
        proyecto.Descripcion = modelo.Descripcion.Trim();
        proyecto.Departamento = modelo.Departamento.Trim();
        proyecto.Carrera = string.IsNullOrWhiteSpace(modelo.Carrera) ? null : modelo.Carrera.Trim();

        var rutaAnterior = proyecto.ArchivoRuta;
        string? rutaNueva = null;

        if (hayArchivoNuevo)
        {
            rutaNueva = await GuardarArchivoAsync(modelo.Archivo!, extension!);

            var nombreOriginal = Path.GetFileName(modelo.Archivo!.FileName);
            if (nombreOriginal.Length > 255) nombreOriginal = nombreOriginal[^255..];

            proyecto.ArchivoNombre = nombreOriginal;
            proyecto.ArchivoRuta = rutaNueva;
            proyecto.ArchivoTamano = modelo.Archivo.Length;
        }
        else if (modelo.QuitarArchivo)
        {
            proyecto.ArchivoNombre = null;
            proyecto.ArchivoRuta = null;
            proyecto.ArchivoTamano = null;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            BorrarArchivo(rutaNueva);   // si falla la base de datos, no dejamos el archivo nuevo huérfano
            throw;
        }

        // El archivo anterior se borra solo cuando ya no se usa (fue reemplazado o quitado)
        if (rutaAnterior != null && rutaAnterior != proyecto.ArchivoRuta)
            BorrarArchivo(rutaAnterior);

        TempData["Exito"] = $"Tu proyecto \"{proyecto.Nombre}\" fue actualizado.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- AUXILIARES ----------
    private void CargarDatosVista(Proyecto proyecto)
    {
        ViewBag.Responsable = User.Identity?.Name;
        ViewBag.Fecha = proyecto.FechaDePublicacion.ToString("dd/MM/yyyy");
    }

    private async Task<string> GuardarArchivoAsync(IFormFile archivo, string extension)
    {
        Directory.CreateDirectory(CarpetaArchivos);

        var nombreGuardado = $"{Guid.NewGuid():N}{extension}";
        using var stream = new FileStream(Path.Combine(CarpetaArchivos, nombreGuardado), FileMode.Create);
        await archivo.CopyToAsync(stream);

        return nombreGuardado;
    }

    private void BorrarArchivo(string? ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return;

        var completa = Path.Combine(CarpetaArchivos, ruta);
        if (System.IO.File.Exists(completa)) System.IO.File.Delete(completa);
    }

    // ---------- ORDENAR (arrastrar y soltar) ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ordenar([FromBody] int[] ids)
    {
        var misProyectos = await _db.Proyectos.Where(p => p.UsuarioId == UsuarioId).ToListAsync();
        var porId = misProyectos.ToDictionary(p => p.Id);

        // Igual que en tus documentos: si llega un id que no es del usuario, se rechaza
        if (ids.Any(id => !porId.ContainsKey(id)))
            return StatusCode(StatusCodes.Status403Forbidden);

        var orden = 1;
        foreach (var id in ids.Distinct())
            porId[id].Orden = orden++;

        // Si algún proyecto del usuario no llegó en la lista, queda al final, conservando su orden
        foreach (var p in misProyectos.Where(p => !ids.Contains(p.Id)).OrderBy(p => p.Orden))
            p.Orden = orden++;

        await _db.SaveChangesAsync();
        return Ok();
    }
}