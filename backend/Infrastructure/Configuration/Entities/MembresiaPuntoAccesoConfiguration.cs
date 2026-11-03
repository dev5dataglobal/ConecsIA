using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MembresiaPuntoAccesoConfiguration : EntityConfigurationBase<MembresiaPuntoAcceso>, IEntityTypeConfiguration<MembresiaPuntoAcceso>
{
    public void Configure(EntityTypeBuilder<MembresiaPuntoAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdMembresia, x.IdPuntoAcceso });
        builder.HasOne<PuntoAcceso>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPuntoAcceso })
            .HasForeignKey(x => new { x.IdOrganizacion, x.IdPuntoAcceso }).OnDelete(DeleteBehavior.Restrict);
    }
}
