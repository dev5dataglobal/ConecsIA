using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ZonaEmpresarialConfiguration : EntityConfigurationBase<ZonaEmpresarial>, IEntityTypeConfiguration<ZonaEmpresarial>
{
    public void Configure(EntityTypeBuilder<ZonaEmpresarial> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdZonaEmpresarial);
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.Property(x => x.Nombre).IsRequired();
    }
}
