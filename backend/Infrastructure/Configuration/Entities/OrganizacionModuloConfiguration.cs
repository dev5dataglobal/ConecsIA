using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class OrganizacionModuloConfiguration : EntityConfigurationBase<OrganizacionModulo>, IEntityTypeConfiguration<OrganizacionModulo>
{
    public void Configure(EntityTypeBuilder<OrganizacionModulo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdModulo });
    }
}
