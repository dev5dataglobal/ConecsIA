using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class VehiculoConfiguration : EntityConfigurationBase<Vehiculo>, IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdVehiculo);
        builder.HasIndex(x => new { x.IdOrganizacion, x.PlacaNormalizada }).IsUnique();
    }
}
