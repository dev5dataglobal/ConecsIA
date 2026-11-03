using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AppModuloConfiguration : EntityConfigurationBase<AppModulo>, IEntityTypeConfiguration<AppModulo>
{
    public void Configure(EntityTypeBuilder<AppModulo> builder)
    {
        ConfigureDefaults(builder);
        builder.ToTable("app_modulos", "public");
        builder.HasKey(x => x.IdAppModulo);
        builder.Property(x => x.Codigo).HasMaxLength(60);
        builder.Property(x => x.Nombre).HasMaxLength(120);
        builder.Property(x => x.Ruta).HasMaxLength(200);
        builder.Property(x => x.Icono).HasMaxLength(60);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.ToTable(t => t.HasCheckConstraint("ck_app_modulos_codigo", "codigo ~ '^[a-z][a-z0-9_]*$'"));
    }
}
