using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Historial append-only (trigger en la migración) de una autorización de objeto.</summary>
public sealed class AutorizacionObjetoTransicionConfiguration : EntityConfigurationBase<AutorizacionObjetoTransicion>, IEntityTypeConfiguration<AutorizacionObjetoTransicion>
{
    public void Configure(EntityTypeBuilder<AutorizacionObjetoTransicion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTransicion);
        builder.Property(x => x.EstadoAnterior).HasMaxLength(20);
        builder.Property(x => x.EstadoNuevo).HasMaxLength(20);
        builder.Property(x => x.Motivo).HasMaxLength(500);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacionObjeto });
        builder.HasOne<AutorizacionObjeto>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacionObjeto }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacionObjeto }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioActor).OnDelete(DeleteBehavior.Restrict);
    }
}
