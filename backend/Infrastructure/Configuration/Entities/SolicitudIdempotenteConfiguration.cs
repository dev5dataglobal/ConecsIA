using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class SolicitudIdempotenteConfiguration : EntityConfigurationBase<SolicitudIdempotente>, IEntityTypeConfiguration<SolicitudIdempotente>
{
    public void Configure(EntityTypeBuilder<SolicitudIdempotente> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.ClaveIdempotencia });
    }
}
