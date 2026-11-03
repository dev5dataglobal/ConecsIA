using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class RecuperacionContrasenaConfiguration : EntityConfigurationBase<RecuperacionContrasena>, IEntityTypeConfiguration<RecuperacionContrasena>
{
    public void Configure(EntityTypeBuilder<RecuperacionContrasena> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdRecuperacionContrasena);
        builder.Property(x => x.TokenHash).HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.IdUsuario);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Restrict);
    }
}
