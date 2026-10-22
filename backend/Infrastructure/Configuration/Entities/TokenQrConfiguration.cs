using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class TokenQrConfiguration : EntityConfigurationBase<TokenQr>, IEntityTypeConfiguration<TokenQr>
{
    public void Configure(EntityTypeBuilder<TokenQr> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdToken);
        builder.HasIndex(x => x.HashSha256).IsUnique();
    }
}
