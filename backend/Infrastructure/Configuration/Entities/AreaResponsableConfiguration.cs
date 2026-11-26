using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Designación de responsables y autorizadores por destino. FK compuestas: área y membresía de la misma organización.</summary>
public sealed class AreaResponsableConfiguration : EntityConfigurationBase<AreaResponsable>, IEntityTypeConfiguration<AreaResponsable>
{
    public void Configure(EntityTypeBuilder<AreaResponsable> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdArea, x.IdMembresia });
        builder.Property(x => x.AsignadoEn).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdMembresia });
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MembresiaOrganizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdMembresia }).HasForeignKey(x => new { x.IdOrganizacion, x.IdMembresia }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioAsigna).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioActualiza).OnDelete(DeleteBehavior.Restrict);
    }
}
