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
        // Varias personas comparten tipo: índice no único (solo soporta la FK compuesta).
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdTipoPersona });
        builder.HasIndex(x => new { x.IdOrganizacion, x.IdTipoDocumento, x.NumeroDocumentoNormalizado }).IsUnique();
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(x => x.IdOrganizacion).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TipoDocumento>().WithMany().HasForeignKey(x => x.IdTipoDocumento).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TipoPersona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdTipoPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdTipoPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Archivo>().WithMany().HasForeignKey(x => x.IdArchivoFoto).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.IdUsuarioCrea).OnDelete(DeleteBehavior.Restrict);
    }
}
