using ConecsIA.Modules.Auditoria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Auditoria.Infrastructure;

internal sealed class ConfiguracionEventoAuditoria : IEntityTypeConfiguration<EventoAuditoria>
{
    public void Configure(EntityTypeBuilder<EventoAuditoria> builder)
    {
        builder.ToTable("auditoria_evento");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Accion).HasMaxLength(100).IsRequired();
        builder.Property(e => e.TipoObjeto).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Resultado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.ValoresAnteriores).HasColumnType("jsonb");
        builder.Property(e => e.ValoresNuevos).HasColumnType("jsonb");
        builder.Property(e => e.TraceId).HasMaxLength(64);
        builder.HasIndex(e => new { e.OrganizacionId, e.Instante });
        builder.HasIndex(e => new { e.TipoObjeto, e.ObjetoId });
    }
}
