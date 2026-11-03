using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PersonaTelefonoConfiguration : EntityConfigurationBase<PersonaTelefono>, IEntityTypeConfiguration<PersonaTelefono>
{
    public void Configure(EntityTypeBuilder<PersonaTelefono> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdTelefono);
        builder.Property(x => x.CodigoPais).HasMaxLength(5).HasDefaultValue("+57");
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        // Sin duplicados activos por persona y un solo principal activo (también ante doble envío).
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona, x.CodigoPais, x.Numero }).IsUnique().HasFilter("activo").HasDatabaseName("UX_persona_telefono_numero_activo");
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona }).IsUnique().HasFilter("principal AND activo").HasDatabaseName("UX_persona_telefono_principal");
    }
}
