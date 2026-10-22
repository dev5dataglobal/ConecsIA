using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class MembresiaSedeConfiguration : EntityConfigurationBase<MembresiaSede>, IEntityTypeConfiguration<MembresiaSede>
{
    public void Configure(EntityTypeBuilder<MembresiaSede> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdMembresia, x.IdSede });
    }
}
