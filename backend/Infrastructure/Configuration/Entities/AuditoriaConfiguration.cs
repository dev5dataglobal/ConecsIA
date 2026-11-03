using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class AuditoriaConfiguration : EntityConfigurationBase<Auditoria>, IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdAuditoria);
        // ADR-029: dispositivo/navegador del actor (User-Agent truncado) y consultas de historial por objeto y fecha.
        builder.Property(x => x.AgenteUsuario).HasMaxLength(300);
        builder.HasIndex(x => new { x.IdOrganizacion, x.Entidad, x.IdRegistro });
        builder.HasIndex(x => new { x.IdOrganizacion, x.OcurridoEn });
    }
}
