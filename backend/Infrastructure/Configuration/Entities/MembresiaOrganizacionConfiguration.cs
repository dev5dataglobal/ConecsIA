using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MembresiaOrganizacionConfiguration : EntityConfigurationBase<MembresiaOrganizacion>, IEntityTypeConfiguration<MembresiaOrganizacion>
{
    public void Configure(EntityTypeBuilder<MembresiaOrganizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMembresia);
    }
}
