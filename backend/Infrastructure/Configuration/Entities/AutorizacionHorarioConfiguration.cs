using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AutorizacionHorarioConfiguration : EntityConfigurationBase<AutorizacionHorario>, IEntityTypeConfiguration<AutorizacionHorario>
{
    public void Configure(EntityTypeBuilder<AutorizacionHorario> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAutorizacionHorario);
        builder.ToTable("autorizacion_horario", "public", table => table.HasCheckConstraint("ck_autorizacion_horario_dia", "dia_semana BETWEEN 1 AND 7"));
    }
}
