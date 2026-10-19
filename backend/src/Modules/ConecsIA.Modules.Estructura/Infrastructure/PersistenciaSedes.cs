using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Estructura.Application;
using ConecsIA.Modules.Estructura.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Estructura.Infrastructure;

internal sealed class ConfiguracionSede : IEntityTypeConfiguration<Sede>
{
    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        builder.ToTable("sede");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Nombre).HasMaxLength(Sede.LargoNombre).IsRequired();
        builder.Property(s => s.NombreNormalizado).HasMaxLength(Sede.LargoNombre).IsRequired();
        builder.Property(s => s.Direccion).HasMaxLength(Sede.LargoDireccion);
        builder.Property(s => s.Ciudad).HasMaxLength(Sede.LargoCiudadPais);
        builder.Property(s => s.Pais).HasMaxLength(Sede.LargoCiudadPais);
        builder.Property(s => s.ZonaHoraria).HasMaxLength(64).IsRequired();
        builder.Property(s => s.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Version).IsRowVersion();

        // ADR-004: unicidad por organización y clave (organizacion_id, id) para FKs compuestas futuras
        // (áreas, zonas, autorizaciones). La FK a organizacion se crea en la migración (otro módulo).
        builder.HasIndex(s => new { s.OrganizacionId, s.NombreNormalizado }).IsUnique();
        builder.HasAlternateKey(s => new { s.OrganizacionId, s.Id });
    }
}

internal sealed class RepositorioSedes(ConecsIADbContext contexto) : IRepositorioSedes
{
    private DbSet<Sede> Sedes => contexto.Set<Sede>();

    public Task<Sede?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        Sedes.SingleOrDefaultAsync(s => s.Id == id, cancelacion);

    public Task<bool> ExisteNombreAsync(string nombreNormalizado, Guid? excluir, CancellationToken cancelacion) =>
        Sedes.AnyAsync(s => s.NombreNormalizado == nombreNormalizado && s.Id != excluir, cancelacion);

    public async Task<(IReadOnlyList<Sede> Elementos, int Total)> ListarAsync(
        EstadoSede? estado,
        IReadOnlySet<Guid>? soloSedes,
        int pagina,
        int tamano,
        CancellationToken cancelacion)
    {
        var consulta = Sedes.AsNoTracking();
        if (estado is not null) consulta = consulta.Where(s => s.Estado == estado);
        if (soloSedes is not null)
        {
            var ids = soloSedes.ToList();
            consulta = consulta.Where(s => ids.Contains(s.Id));
        }

        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .OrderBy(s => s.NombreNormalizado)
            .ThenBy(s => s.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);
        return (elementos, total);
    }

    public void Agregar(Sede sede) => Sedes.Add(sede);

    public void EsperarVersion(Sede sede, uint version) =>
        contexto.Entry(sede).Property(s => s.Version).OriginalValue = version;
}
