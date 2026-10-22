using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PlantillaRolConfiguration : EntityConfigurationBase<PlantillaRol>, IEntityTypeConfiguration<PlantillaRol>
{
    public void Configure(EntityTypeBuilder<PlantillaRol> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPlantillaRol);
    }
}
