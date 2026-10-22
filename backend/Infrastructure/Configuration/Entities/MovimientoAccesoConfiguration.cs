using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MovimientoAccesoConfiguration : EntityConfigurationBase<MovimientoAcceso>, IEntityTypeConfiguration<MovimientoAcceso>
{
    public void Configure(EntityTypeBuilder<MovimientoAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMovimiento);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdZonaPuntoAcceso });
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Zona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdZona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdZonaPuntoAcceso }).OnDelete(DeleteBehavior.Restrict);
    }
}
