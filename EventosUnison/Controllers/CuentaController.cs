using System.Security.Claims;
using EventosUnison.Data;
using EventosUnison.Models;
using EventosUnison.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventosUnison.Controllers;

public class CuentaController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public CuentaController(AppDbContext db) => _db = db;

    // ---------- LOGIN ----------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(modelo);

        var correo = modelo.CorreoInstitucional.Trim().ToLower();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.CorreoInstitucional == correo);

        var valido = usuario != null &&
            _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, modelo.Password)
                != PasswordVerificationResult.Failed;

        if (!valido)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos");
            return View(modelo);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.CorreoInstitucional),
            new(ClaimTypes.Role, usuario.Rol)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identidad));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ---------- REGISTRO ----------
    [HttpGet]
    public IActionResult Registro() => View(new RegistroViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroViewModel modelo)
    {
        // Solo se puede elegir Alumno o Profesor desde el formulario
        if (modelo.Rol != Roles.Alumno && modelo.Rol != Roles.Profesor)
            ModelState.AddModelError(nameof(modelo.Rol), "Rol no válido");

        if (modelo.Rol == Roles.Alumno && string.IsNullOrWhiteSpace(modelo.Carrera))
            ModelState.AddModelError(nameof(modelo.Carrera), "Los alumnos deben indicar su carrera");

        var correo = modelo.CorreoInstitucional.Trim().ToLower();
        if (await _db.Usuarios.AnyAsync(u => u.CorreoInstitucional == correo))
            ModelState.AddModelError(nameof(modelo.CorreoInstitucional), "Ese correo ya está registrado");

        if (!ModelState.IsValid) return View(modelo);

        var usuario = new Usuario
        {
            NombreCompleto = modelo.NombreCompleto.Trim(),
            CorreoInstitucional = correo,
            Rol = modelo.Rol,
            Departamento = modelo.Departamento.Trim(),
            Carrera = modelo.Rol == Roles.Alumno ? modelo.Carrera!.Trim() : null,
            FechaRegistro = DateTime.Now
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, modelo.Password);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        TempData["Exito"] = "Cuenta creada correctamente. Ya puedes iniciar sesión.";
        return RedirectToAction("Login");
    }

    // ---------- LOGOUT ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    [Authorize]
    public IActionResult AccesoDenegado() => View();
}