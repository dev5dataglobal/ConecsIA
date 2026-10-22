using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class TipoDispositivoConfiguration : EntityConfigurationBase<TipoDispositivo>, IEntityTypeConfiguration<TipoDispositivo>
{
    public void Configure(EntityTypeBuilder<TipoDispositivo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTipoDispositivo);
    }
}
