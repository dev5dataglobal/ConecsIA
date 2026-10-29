using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>
/// Visita: episodio operativo (≠ persona, autorización y permanencia). Referencias con FK compuestas por organización;
/// el estado se restringe con CHECK y la columna version se incrementa en cada transición.
/// </summary>
public sealed class VisitaConfiguration : EntityConfigurationBase<Visita>, IEntityTypeConfiguration<Visita>
{
    public void Configure(EntityTypeBuilder<Visita> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdVisita);
        builder.Property(x => x.Estado).HasMaxLength(20);
        builder.Property(x => x.Motivo).HasMaxLength(500);
        builder.Property(x => x.MotivoCierre).HasMaxLength(500);
        builder.Property(x => x.PoliticaAplicada).HasMaxLength(100);
        builder.Property(x => x.MotivoExcepcion).HasMaxLength(500);
        builder.Property(x => x.FechaEsperada).HasColumnType("date");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdVisita }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede, x.Estado });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdArea });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdAutorizacion });
        builder.HasIndex(x => new { x.IdOrganizacion, x.CreadoEn });
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_visita_estado", "estado IN ('Pendiente','Autorizada','Ingreso','Finalizada','Rechazada','Cancelada','Vencida')");
            t.HasCheckConstraint("ck_visita_horario_esperado", "hora_esperada_inicio IS NULL OR hora_esperada_fin IS NULL OR hora_esperada_fin > hora_esperada_inicio");
            // Una excepción al tipo de permiso siempre lleva motivo (D-39).
            t.HasCheckConstraint("ck_visita_excepcion_motivo", "NOT excepcion_tipo_acceso OR motivo_excepcion IS NOT NULL");
        });
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Area>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdArea }).HasForeignKey(x => new { x.IdOrganizacion, x.IdArea }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Autorizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).HasForeignKey(x => new { x.IdOrganizacion, x.IdAutorizacion }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MembresiaOrganizacion>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdMembresia }).HasForeignKey(x => new { x.IdOrganizacion, x.IdMembresiaResponsable }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioRegistra).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioActualiza).OnDelete(DeleteBehavior.Restrict);
    }
}
