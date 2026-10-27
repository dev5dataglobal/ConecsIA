using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class TipoPersonaConfiguration : EntityConfigurationBase<TipoPersona>, IEntityTypeConfiguration<TipoPersona>
{
    public void Configure(EntityTypeBuilder<TipoPersona> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTipoPersona);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdTipoPersona }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.Nombre }).IsUnique();
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
    }
}
