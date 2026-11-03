using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class SesionRefrescoConfiguration : EntityConfigurationBase<SesionRefresco>, IEntityTypeConfiguration<SesionRefresco>
{
    public void Configure(EntityTypeBuilder<SesionRefresco> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdSesionRefresco);
        builder.Property(x => x.TokenHash).HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.IdUsuario);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
    }
}
