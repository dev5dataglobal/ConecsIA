using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class SedeConfiguration : EntityConfigurationBase<Sede>, IEntityTypeConfiguration<Sede>
{
    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdSede);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede }).IsUnique();
        builder.HasOne<Ciudad>().WithMany().HasForeignKey(x => x.IdCiudad).OnDelete(DeleteBehavior.Restrict);
    }
}
