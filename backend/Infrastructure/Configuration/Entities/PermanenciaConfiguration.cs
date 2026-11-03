using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>
/// Permanencia: intervalo entrada–salida de una visita. Índices únicos que blindan la concurrencia:
/// una permanencia por visita y, como máximo, una abierta por persona y sede (Propuesta PRD §6.8, D-07).
/// </summary>
public sealed class PermanenciaConfiguration : EntityConfigurationBase<Permanencia>, IEntityTypeConfiguration<Permanencia>
{
    public void Configure(EntityTypeBuilder<Permanencia> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPermanencia);
        builder.Property(x => x.Estado).HasMaxLength(20);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPermanencia }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVisita }).IsUnique().HasDatabaseName("ux_permanencia_visita");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona, x.IdSede }).IsUnique().HasFilter("estado = 'Abierta'").HasDatabaseName("ux_permanencia_abierta_persona_sede");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede, x.Estado });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_permanencia_estado", "estado IN ('Abierta','Cerrada')");
            t.HasCheckConstraint("ck_permanencia_cierre", "(estado = 'Abierta' AND cerrada_en IS NULL AND id_movimiento_salida IS NULL) OR (estado = 'Cerrada' AND cerrada_en IS NOT NULL AND id_movimiento_salida IS NOT NULL AND cerrada_en >= abierta_en AND duracion_segundos >= 0)");
        });
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Visita>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdVisita }).HasForeignKey(x => new { x.IdOrganizacion, x.IdVisita }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MovimientoAcceso>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdMovimiento }).HasForeignKey(x => new { x.IdOrganizacion, x.IdMovimientoEntrada }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MovimientoAcceso>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdMovimiento }).HasForeignKey(x => new { x.IdOrganizacion, x.IdMovimientoSalida }).OnDelete(DeleteBehavior.Restrict);
    }
}
