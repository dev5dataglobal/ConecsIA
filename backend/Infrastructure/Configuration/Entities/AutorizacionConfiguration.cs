using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionConfiguration : EntityConfigurationBase<Autorizacion>, IEntityTypeConfiguration<Autorizacion>
{
    public void Configure(EntityTypeBuilder<Autorizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAutorizacion);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion }).IsUnique();
        builder.Property(x => x.FechaInicio).HasColumnType("date");
        builder.Property(x => x.FechaFin).HasColumnType("date");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
    }
}
