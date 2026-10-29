using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionConfiguration : EntityConfigurationBase<Autorizacion>, IEntityTypeConfiguration<Autorizacion>
{
    public void Configure(EntityTypeBuilder<Autorizacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAutorizacion);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion }).IsUnique();
        builder.Property(x => x.FechaInicio).HasColumnType("date");
        builder.Property(x => x.FechaFin).HasColumnType("date");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.HoraInicio).HasColumnType("time");
        builder.Property(x => x.HoraFin).HasColumnType("time");
        builder.Property(x => x.MotivoDecision).HasMaxLength(500);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_autorizacion_modalidad", "modalidad IN ('Unica','Diaria','Temporal','Recurrente','Permanente')");
            t.HasCheckConstraint("ck_autorizacion_estado", "estado IN ('Pendiente','Aprobada','Rechazada','Cancelada','Revocada','Vencida','Agotada')");
            // Sin fecha final solo Permanente (siempre) y Recurrente (periodo opcional); Única y Diaria son de un solo día.
            t.HasCheckConstraint("ck_autorizacion_fechas",
                "(modalidad = 'Permanente' AND fecha_fin IS NULL) OR (modalidad = 'Recurrente' AND (fecha_fin IS NULL OR fecha_fin >= fecha_inicio)) " +
                "OR (modalidad IN ('Unica','Diaria') AND fecha_fin = fecha_inicio) OR (modalidad = 'Temporal' AND fecha_fin >= fecha_inicio)");
            // Temporal exige hora de inicio y fin (instantes); en un mismo día el fin debe ser posterior. Recurrente y Permanente usan autorizacion_horario.
            t.HasCheckConstraint("ck_autorizacion_horas",
                "(modalidad = 'Temporal' AND hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND (fecha_fin > fecha_inicio OR hora_fin > hora_inicio)) " +
                "OR (modalidad IN ('Unica','Diaria') AND ((hora_inicio IS NULL AND hora_fin IS NULL) OR (hora_inicio IS NOT NULL AND hora_fin IS NOT NULL AND hora_fin > hora_inicio))) " +
                "OR (modalidad IN ('Recurrente','Permanente') AND hora_inicio IS NULL AND hora_fin IS NULL)");
        });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioDecide).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioCrea).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IdOrganizacion, x.Estado });
        builder.ToTable(t => t.HasCheckConstraint("ck_autorizacion_emisor_alcance", "(tipo_emisor = 'Empresa' AND id_zona_empresarial_emisora IS NULL AND alcance = 'Privado') OR (tipo_emisor = 'ZonaEmpresarial' AND id_zona_empresarial_emisora IS NOT NULL AND alcance = 'Comun')"));
        builder.HasOne<ZonaEmpresarial>().WithMany().HasForeignKey(x => x.IdZonaEmpresarialEmisora).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
    }
}
