using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AreaResponsableConfiguration : EntityConfigurationBase<AreaResponsable>, IEntityTypeConfiguration<AreaResponsable>
{
    public void Configure(EntityTypeBuilder<AreaResponsable> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdArea, x.IdMembresia });
    }
}
