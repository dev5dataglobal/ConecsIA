using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ZonaEmpresarialOrganizacionConfiguration : EntityConfigurationBase<ZonaEmpresarialOrganizacion>, IEntityTypeConfiguration<ZonaEmpresarialOrganizacion>
{
    public void Configure(EntityTypeBuilder<ZonaEmpresarialOrganizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdZonaEmpresarial });
        builder.HasOne<ZonaEmpresarial>().WithMany().HasForeignKey(x => x.IdZonaEmpresarial).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdZonaEmpresarial, x.EsAdministradora }).IsUnique();
        // Cada zona tiene una sola organización superior (administradora) activa.
        builder.HasIndex(x => x.IdZonaEmpresarial).IsUnique()
            .HasDatabaseName("UX_zona_empresarial_organizacion_administradora")
            .HasFilter("es_administradora AND activa");
        // Una empresa pertenece como máximo a una zona empresarial activa (también bajo concurrencia).
        builder.HasIndex(x => x.IdOrganizacion).IsUnique()
            .HasDatabaseName("UX_zona_empresarial_organizacion_empresa")
            .HasFilter("NOT es_administradora AND activa");
    }
}
