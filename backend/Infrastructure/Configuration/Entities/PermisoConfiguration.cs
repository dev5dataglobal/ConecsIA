using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PermisoConfiguration : EntityConfigurationBase<Permiso>, IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPermiso);
        builder.HasIndex(x => x.Codigo).IsUnique();
    }
}
