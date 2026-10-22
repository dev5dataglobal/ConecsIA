using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ModuloConfiguration : EntityConfigurationBase<Modulo>, IEntityTypeConfiguration<Modulo>
{
    public void Configure(EntityTypeBuilder<Modulo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdModulo);
    }
}
