using EventosUnison.Models;
using Microsoft.EntityFrameworkCore;

namespace EventosUnison.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<RegistroEvento> RegistroEventos => Set<RegistroEvento>();
    public DbSet<InteresEvento> InteresEventos => Set<InteresEvento>();

    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // La FK OrganizadorId no sigue la convención por el nombre de la navegación
        modelBuilder.Entity<Evento>()
            .HasOne(e => e.OrganizadorUsuario)
            .WithMany()
            .HasForeignKey(e => e.OrganizadorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notificacion>()
        .HasOne(n => n.Usuario).WithMany()
        .HasForeignKey(n => n.UsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notificacion>()
            .HasOne(n => n.Actor).WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }


}