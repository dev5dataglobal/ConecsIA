using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Plantillas de categorías por tipo de organización (plano SaaS, sin id_organizacion).</summary>
public sealed class PlantillaCategoriaAccesoConfiguration : EntityConfigurationBase<PlantillaCategoriaAcceso>, IEntityTypeConfiguration<PlantillaCategoriaAcceso>
{
    public void Configure(EntityTypeBuilder<PlantillaCategoriaAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPlantillaCategoriaAcceso);
        builder.Property(x => x.Codigo).HasMaxLength(40);
        builder.Property(x => x.Nombre).HasMaxLength(100);
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.FlujoAprobacion).HasMaxLength(20);
        builder.Property(x => x.ModalidadesPermitidas).HasColumnType("text[]");
        builder.HasIndex(x => new { x.IdTipoOrganizacion, x.Codigo }).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("ck_plantilla_categoria_acceso_flujo", "flujo_aprobacion IN ('Automatico','Anfitrion','Seguridad')"));
        builder.HasOne<TipoOrganizacion>().WithMany().HasForeignKey(x => x.IdTipoOrganizacion).OnDelete(DeleteBehavior.Restrict);
    }
}
