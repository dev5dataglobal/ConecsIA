using ConecsIA.Modules.Organizaciones.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class ConfiguracionOrganizacion : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> builder)
    {
        builder.ToTable("organizacion");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Nombre).HasMaxLength(Organizacion.LargoNombre).IsRequired();
        builder.Property(o => o.NombreNormalizado).HasMaxLength(Organizacion.LargoNombre).IsRequired();
        builder.Property(o => o.Tipo).HasMaxLength(50).IsRequired();
        builder.Property(o => o.IdentificacionLegal).HasMaxLength(Organizacion.LargoIdentificacion);
        builder.Property(o => o.ZonaHoraria).HasMaxLength(64);
        builder.Property(o => o.ContactoAdministrativo).HasMaxLength(Organizacion.LargoContacto);
        builder.Property(o => o.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.Version).IsRowVersion();

        // Unicidad global del nombre normalizado (propuesta HU-009).
        builder.HasIndex(o => o.NombreNormalizado).IsUnique();
    }
}
