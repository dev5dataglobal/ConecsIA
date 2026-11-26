using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>
/// Movimiento de vehículo o dispositivo (ADR-030). Todas las referencias son FK compuestas con la organización; el punto
/// debe ser de la sede del movimiento. Una entrada admite a lo sumo una salida (índice único parcial). La tabla es de solo
/// inserción (trigger en la migración).
/// </summary>
public sealed class MovimientoObjetoConfiguration : EntityConfigurationBase<MovimientoObjeto>, IEntityTypeConfiguration<MovimientoObjeto>
{
    public void Configure(EntityTypeBuilder<MovimientoObjeto> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMovimientoObjeto);
        builder.Property(x => x.TipoObjeto).HasMaxLength(20);
        builder.Property(x => x.TipoMovimiento).HasMaxLength(20);
        builder.Property(x => x.Observacion).HasMaxLength(500);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdMovimientoEntrada }).IsUnique().HasFilter("id_movimiento_entrada IS NOT NULL");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVehiculo, x.OcurridoEn });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdDispositivo, x.OcurridoEn });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVisita });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede, x.OcurridoEn });
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehiculo>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdVehiculo }).HasForeignKey(x => new { x.IdOrganizacion, x.IdVehiculo }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Dispositivo>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdDispositivo }).HasForeignKey(x => new { x.IdOrganizacion, x.IdDispositivo }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AutorizacionObjeto>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacionObjeto }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacionObjeto }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Autorizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Visita>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdVisita }).HasForeignKey(x => new { x.IdOrganizacion, x.IdVisita }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersonaResponsable }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PuntoAcceso>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede, x.IdPuntoAcceso }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede, x.IdPuntoAcceso }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MovimientoAcceso>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdMovimiento }).HasForeignKey(x => new { x.IdOrganizacion, x.IdMovimientoAcceso }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MovimientoObjeto>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdMovimientoObjeto }).HasForeignKey(x => new { x.IdOrganizacion, x.IdMovimientoEntrada }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioRegistra).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_movimiento_objeto_objeto",
                "(tipo_objeto = 'Vehiculo' AND id_vehiculo IS NOT NULL AND id_dispositivo IS NULL) OR (tipo_objeto = 'Dispositivo' AND id_dispositivo IS NOT NULL AND id_vehiculo IS NULL)");
            t.HasCheckConstraint("ck_movimiento_objeto_tipo", "tipo_movimiento IN ('Entrada','Salida')");
            t.HasCheckConstraint("ck_movimiento_objeto_salida", "(tipo_movimiento = 'Salida') = (id_movimiento_entrada IS NOT NULL)");
        });
    }
}
