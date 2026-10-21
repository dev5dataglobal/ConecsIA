using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PermanenciaConfiguration : EntityConfigurationBase<Permanencia>, IEntityTypeConfiguration<Permanencia>
{
    public void Configure(EntityTypeBuilder<Permanencia> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPermanencia);
    }
}
