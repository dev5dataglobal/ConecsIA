using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ZonaEmpresarialOrganizacionConfiguration : EntityConfigurationBase<ZonaEmpresarialOrganizacion>, IEntityTypeConfiguration<ZonaEmpresarialOrganizacion>
{
    public void Configure(EntityTypeBuilder<ZonaEmpresarialOrganizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdZonaEmpresarial });
        builder.HasOne<ZonaEmpresarial>().WithMany().HasForeignKey(x => x.IdZonaEmpresarial).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdZonaEmpresarial, x.EsAdministradora }).IsUnique();
    }
}
