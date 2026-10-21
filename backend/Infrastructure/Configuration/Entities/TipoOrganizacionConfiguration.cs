using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class TipoOrganizacionConfiguration : EntityConfigurationBase<TipoOrganizacion>, IEntityTypeConfiguration<TipoOrganizacion>
{
    public void Configure(EntityTypeBuilder<TipoOrganizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTipoOrganizacion);
    }
}
