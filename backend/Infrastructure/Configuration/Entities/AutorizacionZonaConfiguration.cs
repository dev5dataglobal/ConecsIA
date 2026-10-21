using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionZonaConfiguration : EntityConfigurationBase<AutorizacionZona>, IEntityTypeConfiguration<AutorizacionZona>
{
    public void Configure(EntityTypeBuilder<AutorizacionZona> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdAutorizacion, x.IdZona });
    }
}
