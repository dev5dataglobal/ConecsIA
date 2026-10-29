using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Zona de una sede (RF-05). Nombre único por sede sin distinguir mayúsculas ni espacios extremos.</summary>
public sealed class ZonaConfiguration : EntityConfigurationBase<Zona>, IEntityTypeConfiguration<Zona>
{
    public void Configure(EntityTypeBuilder<Zona> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdZona);
        builder.Property(x => x.Nombre).HasMaxLength(150);
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property<string>("NombreNormalizado").HasColumnName("nombre_normalizado").HasMaxLength(150)
            .HasComputedColumnSql("upper(btrim(nombre))", stored: true);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdZona }).IsUnique();
        // Llave alterna: el punto de acceso referencia (organización, sede, zona) y así no puede quedar en otra sede.
        builder.HasAlternateKey(x => new { x.IdOrganizacion, x.IdSede, x.IdZona });
        builder.HasIndex("IdOrganizacion", "IdSede", "NombreNormalizado").IsUnique().HasDatabaseName("ux_zona_sede_nombre");
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
    }
}
