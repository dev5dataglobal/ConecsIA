using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class DispositivoConfiguration : EntityConfigurationBase<Dispositivo>, IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdDispositivo);
    }
}
