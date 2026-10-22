using ConecsIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public sealed class PlanConfiguration : EntityConfigurationBase<Plan>, IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        ConfigureDefaults(builder);
        builder.HasKey(x => x.IdPlan);
    }
}
