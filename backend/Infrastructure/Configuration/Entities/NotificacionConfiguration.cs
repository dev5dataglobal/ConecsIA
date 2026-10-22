using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class NotificacionConfiguration : EntityConfigurationBase<Notificacion>, IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdNotificacion);
    }
}
