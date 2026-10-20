using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Estructura.Application;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.SharedKernel.Estructura;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Estructura.Infrastructure;

internal sealed class ConfiguracionZona : IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> builder)
    {
        builder.ToTable("zona", t => t.HasCheckConstraint(
            "ck_zona_modo_solo_en_punto_de_acceso", "es_punto_acceso = (modo IS NOT NULL)"));
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).ValueGeneratedNever();
        builder.Property(z => z.Nombre).HasMaxLength(Zona.LargoNombre).IsRequired();
        builder.Property(z => z.NombreNormalizado).HasMaxLength(Zona.LargoNombre).IsRequired();
        builder.Property(z => z.Descripcion).HasMaxLength(Zona.LargoDescripcion);
        builder.Property(z => z.Modo).HasConversion<string>().HasMaxLength(20);
        builder.Property(z => z.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(z => z.Version).IsRowVersion();

        // ADR-004: FK compuesta → la sede es de la misma organización. (organizacion_id, id) para EP-10.
        builder.HasOne<Sede>().WithMany()
            .HasForeignKey(z => new { z.OrganizacionId, z.SedeId })
            .HasPrincipalKey(s => new { s.OrganizacionId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(z => new { z.OrganizacionId, z.Id });
        builder.HasIndex(z => new { z.OrganizacionId, z.SedeId, z.NombreNormalizado }).IsUnique();
    }
}

internal sealed class RepositorioZonas(ConecsIADbContext contexto) : IRepositorioZonas
{
    private DbSet<Zona> Zonas => contexto.Set<Zona>();

    public Task<Zona?> ObtenerAsync(Guid id, CancellationToken cancelacion) => Zonas.SingleOrDefaultAsync(z => z.Id == id, cancelacion);

    public async Task<IReadOnlyList<Zona>> DeSedeAsync(Guid sedeId, CancellationToken cancelacion) =>
        await Zonas.AsNoTracking().Where(z => z.SedeId == sedeId).OrderBy(z => z.NombreNormalizado).ToListAsync(cancelacion);

    public async Task<IReadOnlyList<Zona>> VariasAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion)
    {
        var lista = ids.Distinct().ToList();
        return await Zonas.AsNoTracking().Where(z => lista.Contains(z.Id)).ToListAsync(cancelacion);
    }

    public Task<bool> ExisteNombreAsync(Guid sedeId, string nombreNormalizado, Guid? excluir, CancellationToken cancelacion) =>
        Zonas.AnyAsync(z => z.SedeId == sedeId && z.NombreNormalizado == nombreNormalizado && z.Id != excluir, cancelacion);

    public void Agregar(Zona zona) => Zonas.Add(zona);

    public void EsperarVersion(Zona zona, uint version) =>
        contexto.Entry(zona).Property(z => z.Version).OriginalValue = version;
}

/// <summary>Contrato <see cref="IConsultaPuntosAcceso"/>: zonas de la organización del contexto (filtro global).</summary>
internal sealed class ConsultaPuntosAcceso(IRepositorioZonas zonas) : IConsultaPuntosAcceso
{
    public async Task<IReadOnlyDictionary<Guid, PuntoAccesoResumen>> ObtenerAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion) =>
        (await zonas.VariasAsync(ids, cancelacion))
            .ToDictionary(z => z.Id, z => new PuntoAccesoResumen(z.Id, z.SedeId, z.EsPuntoAcceso, z.Estado == EstadoSede.Activa));
}
