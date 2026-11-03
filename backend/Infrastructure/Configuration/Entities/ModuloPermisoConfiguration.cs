using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class ModuloPermisoConfiguration : EntityConfigurationBase<ModuloPermiso>, IEntityTypeConfiguration<ModuloPermiso>
{
    public void Configure(EntityTypeBuilder<ModuloPermiso> builder)
    {
        ConfigureDefaults(builder);
        builder.ToTable("modulos_permisos", "public");
        builder.HasKey(x => x.IdModuloPermiso);
        // Un mismo módulo no puede asignarse dos veces al mismo rol.
        builder.HasIndex(x => new { x.IdRol, x.IdAppModulo }).IsUnique();
        // Consulta de módulos permitidos por organización y rol.
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdRol });
        builder.HasIndex(x => x.IdAppModulo);
        // FK compuesta: el rol debe pertenecer a la misma organización de la asignación.
        builder.HasOne<Rol>().WithMany()
            .HasForeignKey(x => new { x.IdOrganizacion, x.IdRol })
            .HasPrincipalKey(x => new { x.IdOrganizacion, x.IdRol })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppModulo>().WithMany().HasForeignKey(x => x.IdAppModulo).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioAsigna).OnDelete(DeleteBehavior.SetNull);
    }
}
