using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class UsuarioConfiguration : EntityConfigurationBase<Usuario>, IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdUsuario);
        builder.HasIndex(x => x.CorreoNormalizado).IsUnique();
    }
}
