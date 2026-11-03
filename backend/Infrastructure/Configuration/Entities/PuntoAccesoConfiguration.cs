using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Punto de acceso de una sede (RF-05, ADR-028). Nombre único por sede; la FK compuesta impide asociarlo a una sede de otra organización.</summary>
public sealed class PuntoAccesoConfiguration : EntityConfigurationBase<PuntoAcceso>, IEntityTypeConfiguration<PuntoAcceso>
{
    public void Configure(EntityTypeBuilder<PuntoAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPuntoAcceso);
        builder.Property(x => x.Nombre).HasMaxLength(150);
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.ModoAcceso).HasMaxLength(20);
        builder.Property<string>("NombreNormalizado").HasColumnName("nombre_normalizado").HasMaxLength(150)
            .HasComputedColumnSql("upper(btrim(nombre))", stored: true);
        builder.HasAlternateKey(x => new { x.IdOrganizacion, x.IdSede, x.IdPuntoAcceso });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPuntoAcceso }).IsUnique();
        builder.HasIndex("IdOrganizacion", "IdSede", "NombreNormalizado").IsUnique().HasDatabaseName("ux_punto_acceso_sede_nombre");
        builder.ToTable(t => t.HasCheckConstraint("ck_punto_acceso_modo", "modo_acceso IN ('Entrada','Salida','Ambos')"));
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede })
            .HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
    }
}
