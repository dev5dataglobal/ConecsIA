using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PersonaTelefonoConfiguration : EntityConfigurationBase<PersonaTelefono>, IEntityTypeConfiguration<PersonaTelefono>
{
    public void Configure(EntityTypeBuilder<PersonaTelefono> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTelefono);
    }
}
