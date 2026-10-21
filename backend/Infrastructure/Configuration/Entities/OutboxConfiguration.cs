using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class OutboxConfiguration : EntityConfigurationBase<Outbox>, IEntityTypeConfiguration<Outbox>
{
    public void Configure(EntityTypeBuilder<Outbox> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMensaje);
    }
}
