using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PersonaVehiculoConfiguration : EntityConfigurationBase<PersonaVehiculo>, IEntityTypeConfiguration<PersonaVehiculo>
{
    public void Configure(EntityTypeBuilder<PersonaVehiculo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdPersona, x.IdVehiculo });
    }
}
