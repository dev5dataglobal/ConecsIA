using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AuditoriaConfiguration : EntityConfigurationBase<Auditoria>, IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAuditoria);
    }
}
