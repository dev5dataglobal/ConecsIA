using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PlantillaRolPermisoConfiguration : EntityConfigurationBase<PlantillaRolPermiso>, IEntityTypeConfiguration<PlantillaRolPermiso>
{
    public void Configure(EntityTypeBuilder<PlantillaRolPermiso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdPlantillaRol, x.IdPermiso });
    }
}
