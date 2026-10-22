using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class RestriccionAccesoConfiguration : EntityConfigurationBase<RestriccionAcceso>, IEntityTypeConfiguration<RestriccionAcceso>
{
    public void Configure(EntityTypeBuilder<RestriccionAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdRestriccion);
    }
}
