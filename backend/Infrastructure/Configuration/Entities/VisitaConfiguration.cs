using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class VisitaConfiguration : EntityConfigurationBase<Visita>, IEntityTypeConfiguration<Visita>
{
    public void Configure(EntityTypeBuilder<Visita> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdVisita);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVisita }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdArea });
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
    }
}
