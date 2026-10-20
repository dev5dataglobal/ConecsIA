using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Estructura.Application;
using ConecsIA.Modules.Estructura.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Estructura.Infrastructure;

internal sealed class ConfiguracionArea : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("area");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Nombre).HasMaxLength(Area.LargoNombre).IsRequired();
        builder.Property(a => a.NombreNormalizado).HasMaxLength(Area.LargoNombre).IsRequired();
        builder.Property(a => a.Tipo).HasMaxLength(Area.LargoTipo);
        builder.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Version).IsRowVersion();

        // ADR-004: FK compuesta (organizacion_id, sede_id) → la sede es de la misma organización.
        builder.HasOne<Sede>().WithMany()
            .HasForeignKey(a => new { a.OrganizacionId, a.SedeId })
            .HasPrincipalKey(s => new { s.OrganizacionId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(a => new { a.OrganizacionId, a.Id });
        builder.HasIndex(a => new { a.OrganizacionId, a.SedeId, a.NombreNormalizado }).IsUnique();

        builder.HasMany(a => a.Asignaciones).WithOne()
            .HasForeignKey(x => new { x.OrganizacionId, x.AreaId })
            .HasPrincipalKey(a => new { a.OrganizacionId, a.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(a => a.Asignaciones).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ConfiguracionAsignacionArea : IEntityTypeConfiguration<AsignacionArea>
{
    public void Configure(EntityTypeBuilder<AsignacionArea> builder)
    {
        builder.ToTable("area_asignacion");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Funcion).HasConversion<string>().HasMaxLength(20).IsRequired();
        // La FK a usuario se crea en la migración (otro módulo).
        builder.HasIndex(a => new { a.OrganizacionId, a.UsuarioId });
    }
}

internal sealed class RepositorioAreas(ConecsIADbContext contexto) : IRepositorioAreas
{
    private DbSet<Area> Areas => contexto.Set<Area>();

    public Task<Area?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        Areas.Include(a => a.Asignaciones).SingleOrDefaultAsync(a => a.Id == id, cancelacion);

    public async Task<IReadOnlyList<Area>> DeSedeAsync(Guid sedeId, CancellationToken cancelacion) =>
        await Areas.AsNoTracking().Include(a => a.Asignaciones)
            .Where(a => a.SedeId == sedeId).OrderBy(a => a.NombreNormalizado).ToListAsync(cancelacion);

    public Task<bool> ExisteNombreAsync(Guid sedeId, string nombreNormalizado, Guid? excluir, CancellationToken cancelacion) =>
        Areas.AnyAsync(a => a.SedeId == sedeId && a.NombreNormalizado == nombreNormalizado && a.Id != excluir, cancelacion);

    public void Agregar(Area area) => Areas.Add(area);

    public void EsperarVersion(Area area, uint version) =>
        contexto.Entry(area).Property(a => a.Version).OriginalValue = version;
}
