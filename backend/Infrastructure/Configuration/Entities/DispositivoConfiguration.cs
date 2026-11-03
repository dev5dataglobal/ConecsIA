using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>
/// Dispositivo de una persona (1:N). FK compuestas con la organización: persona y tipo deben ser de la misma
/// organización que el dispositivo. Identificador único por organización (provisional, D-17).
/// </summary>
public sealed class DispositivoConfiguration : EntityConfigurationBase<Dispositivo>, IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdDispositivo);
        builder.Property(x => x.CodigoIdentificador).HasMaxLength(60);
        builder.Property(x => x.Serial).HasMaxLength(60);
        builder.Property(x => x.SerialNormalizado).HasMaxLength(60);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdDispositivo }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.CodigoIdentificador }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersonaResponsable });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdTipoDispositivo });
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersonaResponsable }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TipoDispositivo>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdTipoDispositivo }).HasForeignKey(x => new { x.IdOrganizacion, x.IdTipoDispositivo }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioCrea).OnDelete(DeleteBehavior.Restrict);
    }
}
