using ConecsIA.Modules.Organizaciones.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class ConfiguracionOrganizacion : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> builder)
    {
        builder.ToTable("organizacion");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
