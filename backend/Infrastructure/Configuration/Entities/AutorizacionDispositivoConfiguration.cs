using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionDispositivoConfiguration : EntityConfigurationBase<AutorizacionDispositivo>, IEntityTypeConfiguration<AutorizacionDispositivo>
{
    public void Configure(EntityTypeBuilder<AutorizacionDispositivo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdAutorizacion, x.IdDispositivo });
    }
}
