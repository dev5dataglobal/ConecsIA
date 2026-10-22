using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionTransicionConfiguration : EntityConfigurationBase<AutorizacionTransicion>, IEntityTypeConfiguration<AutorizacionTransicion>
{
    public void Configure(EntityTypeBuilder<AutorizacionTransicion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTransicion);
    }
}
