using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class CiudadConfiguration : EntityConfigurationBase<Ciudad>, IEntityTypeConfiguration<Ciudad>
{
    public void Configure(EntityTypeBuilder<Ciudad> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdCiudad);
    }
}
