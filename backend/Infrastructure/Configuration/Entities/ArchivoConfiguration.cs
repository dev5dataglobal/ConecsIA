using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ArchivoConfiguration : EntityConfigurationBase<Archivo>, IEntityTypeConfiguration<Archivo>
{
    public void Configure(EntityTypeBuilder<Archivo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdArchivo);
    }
}
