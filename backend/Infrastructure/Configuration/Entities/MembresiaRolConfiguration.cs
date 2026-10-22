using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MembresiaRolConfiguration : EntityConfigurationBase<MembresiaRol>, IEntityTypeConfiguration<MembresiaRol>
{
    public void Configure(EntityTypeBuilder<MembresiaRol> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdMembresia, x.IdRol });
    }
}
