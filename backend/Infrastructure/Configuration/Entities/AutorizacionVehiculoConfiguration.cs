using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionVehiculoConfiguration : EntityConfigurationBase<AutorizacionVehiculo>, IEntityTypeConfiguration<AutorizacionVehiculo>
{
    public void Configure(EntityTypeBuilder<AutorizacionVehiculo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdAutorizacion, x.IdVehiculo });
    }
}
