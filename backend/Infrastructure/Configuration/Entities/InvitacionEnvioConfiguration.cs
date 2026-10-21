using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class InvitacionEnvioConfiguration : EntityConfigurationBase<InvitacionEnvio>, IEntityTypeConfiguration<InvitacionEnvio>
{
    public void Configure(EntityTypeBuilder<InvitacionEnvio> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdEnvio);
    }
}
