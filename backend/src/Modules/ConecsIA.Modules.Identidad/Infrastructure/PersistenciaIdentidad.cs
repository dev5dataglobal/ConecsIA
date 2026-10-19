using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Organizaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Identidad.Infrastructure;

internal sealed class ConfiguracionUsuario : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Correo).HasMaxLength(254).IsRequired();
        builder.Property(u => u.Nombre).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Correo).IsUnique();
        builder.Property(u => u.HashContrasena).HasMaxLength(512);
        builder.Property(u => u.SelloSeguridad).HasMaxLength(64).IsRequired();
    }
}

internal sealed class ConfiguracionMembresia : IEntityTypeConfiguration<Membresia>
{
    public void Configure(EntityTypeBuilder<Membresia> builder)
    {
        builder.ToTable("membresia");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Permisos).HasColumnType("text[]");
        builder.Property(m => m.SedesAsignadas).HasColumnType("uuid[]");
        builder.Property(m => m.Roles).HasColumnType("uuid[]");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        // La FK a organizacion se crea en la migración: la entidad vive en otro módulo (ADR-001).
        builder.HasIndex(m => new { m.UsuarioId, m.OrganizacionId }).IsUnique();
        builder.HasIndex(m => m.OrganizacionId);
    }
}

internal sealed class ConfiguracionSesionUsuario : IEntityTypeConfiguration<SesionUsuario>
{
    public void Configure(EntityTypeBuilder<SesionUsuario> builder)
    {
        builder.ToTable("sesion_usuario");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.UsuarioId, s.RevocadaEn });
    }
}

internal sealed class RepositorioIdentidad(ConecsIADbContext contexto) : IRepositorioIdentidad
{
    public Task<Usuario?> ObtenerUsuarioAsync(Guid id, CancellationToken cancelacion) =>
        contexto.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancelacion);

    public async Task<IReadOnlyList<Membresia>> MembresiasActivasAsync(Guid usuarioId, CancellationToken cancelacion) =>
        await contexto.Set<Membresia>().AsNoTracking()
            .Where(m => m.UsuarioId == usuarioId && m.Activa)
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Rol>> RolesActivosAsync(
        Guid organizacionId, IReadOnlyCollection<Guid> ids, CancellationToken cancelacion)
    {
        var lista = ids.ToList();
        // Excepción revisada al filtro global (HU-005): el contexto aún no está resuelto, así que la
        // organización se fija explícitamente con la de la membresía verificada.
        return await contexto.Set<Rol>().IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.OrganizacionId == organizacionId && r.Activo && lista.Contains(r.Id))
            .ToListAsync(cancelacion);
    }

    public async Task<IReadOnlyList<Usuario>> UsuariosActivosAsync(CancellationToken cancelacion) =>
        await contexto.Set<Usuario>().AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.Nombre)
            .ToListAsync(cancelacion);

    public void AgregarSesion(SesionUsuario sesion) => contexto.Add(sesion);

    public Task<SesionUsuario?> ObtenerSesionAsync(Guid sesionId, CancellationToken cancelacion) =>
        contexto.Set<SesionUsuario>().AsNoTracking().SingleOrDefaultAsync(s => s.Id == sesionId, cancelacion);

    public Task<SesionUsuario?> ObtenerSesionParaActualizarAsync(Guid sesionId, CancellationToken cancelacion) =>
        contexto.Set<SesionUsuario>().SingleOrDefaultAsync(s => s.Id == sesionId, cancelacion);

    public async Task<IReadOnlyList<SesionUsuario>> SesionesVigentesAsync(Guid usuarioId, DateTimeOffset ahora, CancellationToken cancelacion) =>
        await contexto.Set<SesionUsuario>().AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId && s.RevocadaEn == null && s.ExpiraEn > ahora)
            .OrderByDescending(s => s.IniciadaEn)
            .ToListAsync(cancelacion);

    public Task<Usuario?> ObtenerPorCorreoParaActualizarAsync(string correoNormalizado, CancellationToken cancelacion) =>
        contexto.Set<Usuario>().SingleOrDefaultAsync(u => u.Correo == correoNormalizado, cancelacion);
}

/// <summary>Solo para el listado de usuarios sintéticos del modo desarrollo.</summary>
public sealed class ConsultaUsuariosDesarrollo(ConecsIADbContext contexto, IConsultaOrganizaciones organizaciones)
{
    public async Task<IReadOnlyDictionary<Guid, List<string>>> OrganizacionesPorUsuarioAsync(CancellationToken cancelacion)
    {
        var membresias = await contexto.Set<Membresia>().AsNoTracking()
            .Where(m => m.Activa)
            .Select(m => new { m.UsuarioId, m.OrganizacionId })
            .ToListAsync(cancelacion);

        var nombres = new Dictionary<Guid, string>();
        foreach (var id in membresias.Select(m => m.OrganizacionId).Distinct())
        {
            nombres[id] = (await organizaciones.ObtenerAsync(id, cancelacion))?.Nombre ?? "?";
        }

        return membresias
            .GroupBy(m => m.UsuarioId)
            .ToDictionary(g => g.Key, g => g.Select(m => nombres[m.OrganizacionId]).Order(StringComparer.Ordinal).ToList());
    }
}
