using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AlertaConfiguration : EntityConfigurationBase<Alerta>, IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAlerta);
        builder.HasIndex(x => new { x.IdOrganizacion, x.ClaveDeduplicacion }).IsUnique();
    }
}
