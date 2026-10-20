using System.Text.Json;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Organizaciones.Application;
using ConecsIA.Modules.Organizaciones.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Modules.Organizaciones.Infrastructure;

internal sealed class ConfiguracionVocabulario : IEntityTypeConfiguration<VocabularioOrganizacion>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<VocabularioOrganizacion> builder)
    {
        builder.ToTable("organizacion_vocabulario");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.Property(v => v.Version).IsRowVersion();
        builder.Property(v => v.Terminos)
            .HasColumnType("jsonb")
            .HasConversion(
                t => JsonSerializer.Serialize(t, Json),
                s => JsonSerializer.Deserialize<Dictionary<string, Termino>>(s, Json) ?? new Dictionary<string, Termino>(),
                new ValueComparer<Dictionary<string, Termino>>(
                    (a, b) => JsonSerializer.Serialize(a, Json) == JsonSerializer.Serialize(b, Json),
                    d => JsonSerializer.Serialize(d, Json).GetHashCode(StringComparison.Ordinal),
                    d => new Dictionary<string, Termino>(d)));
        builder.HasOne<Organizacion>().WithMany().HasForeignKey(v => v.OrganizacionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(v => v.OrganizacionId).IsUnique();
    }
}

internal sealed class RepositorioVocabulario(ConecsIADbContext contexto) : IRepositorioVocabulario
{
    public Task<VocabularioOrganizacion?> ObtenerAsync(CancellationToken cancelacion) =>
        contexto.Set<VocabularioOrganizacion>().SingleOrDefaultAsync(cancelacion);

    public void Agregar(VocabularioOrganizacion vocabulario) => contexto.Add(vocabulario);

    public void EsperarVersion(VocabularioOrganizacion vocabulario, uint version) =>
        contexto.Entry(vocabulario).Property(v => v.Version).OriginalValue = version;
}
