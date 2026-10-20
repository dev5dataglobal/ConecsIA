using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.Modules.Organizaciones.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class ConfiguracionElementoCatalogo : IEntityTypeConfiguration<ElementoCatalogo>
{
    public void Configure(EntityTypeBuilder<ElementoCatalogo> builder)
    {
        builder.ToTable("catalogo_elemento");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Catalogo).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Clave).HasMaxLength(ElementoCatalogo.LargoClave).IsRequired();
        builder.Property(e => e.Nombre).HasMaxLength(ElementoCatalogo.LargoNombre).IsRequired();
        builder.Property(e => e.Origen).HasMaxLength(80).IsRequired();
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(e => e.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        // Clave natural: impide duplicados también ante aplicaciones simultáneas de la plantilla.
        builder.HasIndex(e => new { e.OrganizacionId, e.Catalogo, e.Clave }).IsUnique();
    }
}

internal sealed class RepositorioCatalogos(ConecsIADbContext contexto) : IRepositorioCatalogos
{
    public async Task<IReadOnlyList<ElementoCatalogo>> DeCatalogoAsync(string catalogo, CancellationToken cancelacion) =>
        await contexto.Set<ElementoCatalogo>().Where(e => e.Catalogo == catalogo).ToListAsync(cancelacion);

    public Task<ElementoCatalogo?> ObtenerAsync(Guid id, CancellationToken cancelacion) =>
        contexto.Set<ElementoCatalogo>().SingleOrDefaultAsync(e => e.Id == id, cancelacion);

    public void Agregar(ElementoCatalogo elemento) => contexto.Add(elemento);
}
