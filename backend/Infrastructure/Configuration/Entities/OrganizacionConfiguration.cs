using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class OrganizacionConfiguration : EntityConfigurationBase<Organizacion>, IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdOrganizacion);
        builder.HasOne<TipoOrganizacion>().WithMany().HasForeignKey(x => x.IdTipoOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Plan>().WithMany().HasForeignKey(x => x.IdPlan).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Archivo>().WithMany().HasForeignKey(x => x.IdArchivoLogo).OnDelete(DeleteBehavior.Restrict);
    }
}
