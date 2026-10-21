using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PlanModuloConfiguration : EntityConfigurationBase<PlanModulo>, IEntityTypeConfiguration<PlanModulo>
{
    public void Configure(EntityTypeBuilder<PlanModulo> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => new { x.IdPlan, x.IdModulo });
    }
}
