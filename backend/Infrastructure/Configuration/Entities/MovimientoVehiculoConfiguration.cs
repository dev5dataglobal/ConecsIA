using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MovimientoVehiculoConfiguration : EntityConfigurationBase<MovimientoVehiculo>, IEntityTypeConfiguration<MovimientoVehiculo>
{
    public void Configure(EntityTypeBuilder<MovimientoVehiculo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMovimientoVehiculo);
    }
}
