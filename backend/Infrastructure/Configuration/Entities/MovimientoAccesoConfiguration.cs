using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Movimiento de entrada o salida confirmado. Solo inserción: una corrección es un movimiento nuevo (D-08).</summary>
public sealed class MovimientoAccesoConfiguration : EntityConfigurationBase<MovimientoAcceso>, IEntityTypeConfiguration<MovimientoAcceso>
{
    public void Configure(EntityTypeBuilder<MovimientoAcceso> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdMovimiento);
        builder.Property(x => x.TipoMovimiento).HasMaxLength(20);
        builder.Property(x => x.Resultado).HasMaxLength(20);
        builder.Property(x => x.Medio).HasMaxLength(20);
        builder.Property(x => x.Observacion).HasMaxLength(500);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPuntoAcceso });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVisita });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede, x.OcurridoEn });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_movimiento_acceso_tipo", "tipo_movimiento IN ('Entrada','Salida')");
            t.HasCheckConstraint("ck_movimiento_acceso_resultado", "resultado IN ('Confirmado','Denegado')");
        });
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        // zona_historica: nombre legible de la zona de acceso retirada (ADR-028); solo lo tienen los movimientos anteriores a esa migración.
        builder.Property(x => x.ZonaHistorica).HasMaxLength(150);
        // FK con la sede del movimiento: el punto debe pertenecer a la organización y sede del movimiento.
        builder.HasOne<PuntoAcceso>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede, x.IdPuntoAcceso }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede, x.IdPuntoAcceso }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Visita>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdVisita }).HasForeignKey(x => new { x.IdOrganizacion, x.IdVisita }).OnDelete(DeleteBehavior.Restrict);
        // ADR-029: el movimiento (incluidos los intentos denegados) se vincula al registro de acceso.
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion });
        builder.HasOne<Autorizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioRegistra).OnDelete(DeleteBehavior.Restrict);
    }
}
