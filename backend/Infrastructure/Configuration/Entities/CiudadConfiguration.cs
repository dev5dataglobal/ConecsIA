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
        builder.Property(x => x.ZonaHoraria).IsRequired().HasMaxLength(100);
        // Código de municipio DIVIPOLA (DANE), 5 dígitos; clave para cargas idempotentes del catálogo.
        builder.Property(x => x.CodigoDane).HasMaxLength(5).IsFixedLength();
        builder.HasIndex(x => x.CodigoDane).IsUnique().HasFilter("codigo_dane IS NOT NULL");
    }
}
