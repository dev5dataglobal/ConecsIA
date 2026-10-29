using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionTransicionConfiguration : EntityConfigurationBase<AutorizacionTransicion>, IEntityTypeConfiguration<AutorizacionTransicion>
{
    public void Configure(EntityTypeBuilder<AutorizacionTransicion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTransicion);
        builder.HasOne<Autorizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion });
    }
}
