using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Identidad.Infrastructure;

internal sealed class ConfiguracionRol : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Nombre).HasMaxLength(Rol.LargoNombre).IsRequired();
        builder.Property(r => r.NombreNormalizado).HasMaxLength(Rol.LargoNombre).IsRequired();
        builder.Property(r => r.Descripcion).HasMaxLength(Rol.LargoDescripcion);
        builder.Property(r => r.Permisos).HasColumnType("text[]");
        builder.Property(r => r.Plantilla).HasMaxLength(64);
        builder.Property(r => r.Version).IsRowVersion();

        // ADR-004: unicidad por organización y clave (organizacion_id, id) para FKs compuestas.
        // La FK a organizacion se crea en la migración (otro módulo).
        builder.HasIndex(r => new { r.OrganizacionId, r.NombreNormalizado }).IsUnique();
        builder.HasAlternateKey(r => new { r.OrganizacionId, r.Id });
    }
}

internal sealed class RepositorioRoles(ConecsIADbContext contexto) : IRepositorioRoles
{
    private DbSet<Rol> Roles => contexto.Set<Rol>();

    public Task<Rol?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        Roles.SingleOrDefaultAsync(r => r.Id == id, cancelacion);

    public async Task<IReadOnlyList<Rol>> ListarAsync(bool? activos, CancellationToken cancelacion)
    {
        var consulta = Roles.AsNoTracking();
        if (activos is { } a) consulta = consulta.Where(r => r.Activo == a);
        return await consulta.OrderBy(r => r.NombreNormalizado).ThenBy(r => r.Id).ToListAsync(cancelacion);
    }

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluir, CancellationToken cancelacion) =>
        Roles.AnyAsync(r => r.NombreNormalizado == nombreNormalizado && r.Id != excluir, cancelacion);

    public Task<bool> OtroRolActivoConcedeAsync(string permiso, Guid excluir, CancellationToken cancelacion) =>
        Roles.AnyAsync(r => r.Id != excluir && r.Activo && r.Permisos.Contains(permiso), cancelacion);

    public void Agregar(Rol rol) => Roles.Add(rol);

    public void EsperarVersion(Rol rol, uint version) =>
        contexto.Entry(rol).Property(r => r.Version).OriginalValue = version;
}
