using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Horario de ingreso por defecto de la organización (id_sede nulo) o de una sede (ADR-029).</summary>
public sealed class HorarioBaseConfiguration : EntityConfigurationBase<HorarioBase>, IEntityTypeConfiguration<HorarioBase>
{
    public void Configure(EntityTypeBuilder<HorarioBase> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdHorarioBase);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdSede, x.DiaSemana });
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_horario_base_dia", "dia_semana BETWEEN 1 AND 7");
            // Sin franjas nocturnas hasta D-06.
            t.HasCheckConstraint("ck_horario_base_horas", "hora_fin > hora_inicio");
        });
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sede>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdSede }).HasForeignKey(x => new { x.IdOrganizacion, x.IdSede }).OnDelete(DeleteBehavior.Restrict);
    }
}
