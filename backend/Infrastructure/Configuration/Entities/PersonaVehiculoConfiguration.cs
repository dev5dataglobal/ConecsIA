using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

/// <summary>Vínculo N:M persona–vehículo: un vehículo de la organización puede asociarse a varias personas.</summary>
public sealed class PersonaVehiculoConfiguration : EntityConfigurationBase<PersonaVehiculo>, IEntityTypeConfiguration<PersonaVehiculo>
{
    public void Configure(EntityTypeBuilder<PersonaVehiculo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdOrganizacion, x.IdPersona, x.IdVehiculo });
        builder.HasOne<Persona>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdPersona }).HasForeignKey(x => new { x.IdOrganizacion, x.IdPersona }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vehiculo>().WithMany().HasPrincipalKey(x => new { x.IdOrganizacion, x.IdVehiculo }).HasForeignKey(x => new { x.IdOrganizacion, x.IdVehiculo }).OnDelete(DeleteBehavior.Restrict);
    }
}
