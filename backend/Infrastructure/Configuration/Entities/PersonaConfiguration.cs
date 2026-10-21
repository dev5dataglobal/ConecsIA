using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PersonaConfiguration : EntityConfigurationBase<Persona>, IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPersona);
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdPersona }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdTipoPersona }).IsUnique();
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdTipoDocumento, x.NumeroDocumentoNormalizado }).IsUnique();
        builder.HasOne<TipoDocumento>().WithMany().HasForeignKey(x => x.IdTipoDocumento).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TipoPersona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdTipoPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdTipoPersona }).OnDelete(DeleteBehavior.Restrict);
    }
}
