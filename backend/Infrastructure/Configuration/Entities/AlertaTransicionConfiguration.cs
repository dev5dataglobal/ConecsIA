using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AlertaTransicionConfiguration : EntityConfigurationBase<AlertaTransicion>, IEntityTypeConfiguration<AlertaTransicion>
{
    public void Configure(EntityTypeBuilder<AlertaTransicion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTransicion);
    }
}
