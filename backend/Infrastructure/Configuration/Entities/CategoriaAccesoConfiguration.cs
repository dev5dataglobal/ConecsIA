using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Categoría de acceso por organización (ADR-029). Código único por organización; sin borrado, se inactiva.</summary>
public sealed class CategoriaAccesoConfiguration : EntityConfigurationBase<CategoriaAcceso>, IEntityTypeConfiguration<CategoriaAcceso>
{
    public void Configure(EntityTypeBuilder<CategoriaAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdCategoriaAcceso);
        builder.Property(x => x.Codigo).HasMaxLength(40);
        builder.Property(x => x.Nombre).HasMaxLength(100);
        builder.Property(x => x.Descripcion).HasMaxLength(500);
        builder.Property(x => x.FlujoAprobacion).HasMaxLength(20);
        builder.Property(x => x.ModalidadesPermitidas).HasColumnType("text[]");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdCategoriaAcceso }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.Codigo }).IsUnique();
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_categoria_acceso_flujo", "flujo_aprobacion IN ('Automatico','Anfitrion','Seguridad')");
            t.HasCheckConstraint("ck_categoria_acceso_modalidades",
                "cardinality(modalidades_permitidas) > 0 AND modalidades_permitidas <@ ARRAY['Unica','Diaria','Temporal','Recurrente','Permanente']::text[]");
        });
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
    }
}
