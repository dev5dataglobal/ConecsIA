using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.Modules.Organizaciones.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class ConfiguracionModuloOrganizacion : IEntityTypeConfiguration<ModuloOrganizacion>
{
    public void Configure(EntityTypeBuilder<ModuloOrganizacion> builder)
    {
        builder.ToTable("organizacion_modulo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Modulo).HasMaxLength(50).IsRequired();
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(m => m.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => new { m.OrganizacionId, m.Modulo }).IsUnique();
    }
}

internal sealed class RepositorioModulos(ConecsIADbContext contexto) : IRepositorioModulos
{
    public async Task<IReadOnlyList<ModuloOrganizacion>> DeLaOrganizacionAsync(CancellationToken cancelacion) =>
        await contexto.Set<ModuloOrganizacion>().ToListAsync(cancelacion);

    public void Agregar(ModuloOrganizacion modulo) => contexto.Add(modulo);
}
