using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PlantillaNotificacionConfiguration : EntityConfigurationBase<PlantillaNotificacion>, IEntityTypeConfiguration<PlantillaNotificacion>
{
    public void Configure(EntityTypeBuilder<PlantillaNotificacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPlantilla);
    }
}
