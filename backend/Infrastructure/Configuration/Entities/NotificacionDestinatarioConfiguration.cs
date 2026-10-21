using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class NotificacionDestinatarioConfiguration : EntityConfigurationBase<NotificacionDestinatario>, IEntityTypeConfiguration<NotificacionDestinatario>
{
    public void Configure(EntityTypeBuilder<NotificacionDestinatario> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdDestinatario);
    }
}
