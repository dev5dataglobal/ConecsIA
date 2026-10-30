using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class SolicitudContactoConfiguration : EntityConfigurationBase<SolicitudContacto>, IEntityTypeConfiguration<SolicitudContacto>
{
    public void Configure(EntityTypeBuilder<SolicitudContacto> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdSolicitudContacto);
        builder.Property(x => x.Organizacion).IsRequired().HasMaxLength(150);
        builder.Property(x => x.NombreContacto).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Correo).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Telefono).HasMaxLength(20);
        builder.Property(x => x.TipoOrganizacion).IsRequired().HasMaxLength(60);
        builder.Property(x => x.Mensaje).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Estado).IsRequired().HasMaxLength(20);
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_solicitud_contacto_numero_sedes", "numero_sedes IS NULL OR numero_sedes BETWEEN 1 AND 9999"));
        builder.HasIndex(x => x.FechaCreacion);
    }
}
