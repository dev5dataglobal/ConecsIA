using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ParametroOrganizacionConfiguration : EntityConfigurationBase<ParametroOrganizacion>, IEntityTypeConfiguration<ParametroOrganizacion>
{
    public void Configure(EntityTypeBuilder<ParametroOrganizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.Clave });
    }
}
