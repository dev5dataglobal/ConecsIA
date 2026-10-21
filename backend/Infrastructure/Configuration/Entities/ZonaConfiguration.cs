using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ZonaConfiguration : EntityConfigurationBase<Zona>, IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdZona);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdZona }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede }).IsUnique();
        builder.ToTable("zona", "public", table => table.HasCheckConstraint("ck_zona_modo_acceso", "es_punto_acceso OR modo_acceso IS NULL OR modo_acceso IN ('Entrada','Salida','Ambos')"));
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
    }
}
