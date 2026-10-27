using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>
/// Autorización de ingreso de un vehículo o dispositivo. Todas las referencias usan FK compuestas con la organización
/// para impedir mezclar filas de organizaciones distintas; xmin actúa como token de concurrencia de las decisiones.
/// </summary>
public sealed class AutorizacionObjetoConfiguration : EntityConfigurationBase<AutorizacionObjeto>, IEntityTypeConfiguration<AutorizacionObjeto>
{
    public void Configure(EntityTypeBuilder<AutorizacionObjeto> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAutorizacionObjeto);
        builder.Property(x => x.TipoObjeto).HasMaxLength(20);
        builder.Property(x => x.Estado).HasMaxLength(20);
        builder.Property(x => x.Motivo).HasMaxLength(500);
        builder.Property(x => x.MotivoDecision).HasMaxLength(500);
        builder.Property(x => x.Version).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacionObjeto }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.Estado, x.FechaInicio });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVehiculo });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdDispositivo });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdArea });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion });
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehiculo>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdVehiculo }).HasForeignKey(x => new { x.IdOrganizacion, x.IdVehiculo }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Dispositivo>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdDispositivo }).HasForeignKey(x => new { x.IdOrganizacion, x.IdDispositivo }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Autorizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioSolicita).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioDecide).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_autorizacion_objeto_objeto",
                "(tipo_objeto = 'Vehiculo' AND id_vehiculo IS NOT NULL AND id_dispositivo IS NULL) OR (tipo_objeto = 'Dispositivo' AND id_dispositivo IS NOT NULL AND id_vehiculo IS NULL)");
            t.HasCheckConstraint("CK_autorizacion_objeto_fechas", "fecha_fin >= fecha_inicio");
            t.HasCheckConstraint("CK_autorizacion_objeto_horas", "(hora_inicio IS NULL AND hora_fin IS NULL) OR (hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio)");
            t.HasCheckConstraint("CK_autorizacion_objeto_estado", "estado IN ('Pendiente', 'Aprobada', 'Rechazada', 'Revocada')");
        });
    }
}
