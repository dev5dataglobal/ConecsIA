using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MovimientoDispositivoConfiguration : EntityConfigurationBase<MovimientoDispositivo>, IEntityTypeConfiguration<MovimientoDispositivo>
{
    public void Configure(EntityTypeBuilder<MovimientoDispositivo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMovimientoDispositivo);
    }
}
