using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class TipoDocumentoConfiguration : EntityConfigurationBase<TipoDocumento>, IEntityTypeConfiguration<TipoDocumento>
{
    public void Configure(EntityTypeBuilder<TipoDocumento> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTipoDocumento);
    }
}
