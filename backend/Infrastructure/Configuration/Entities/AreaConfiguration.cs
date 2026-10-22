using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AreaConfiguration : EntityConfigurationBase<Area>, IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdArea);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdArea }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede }).IsUnique();
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
    }
}
