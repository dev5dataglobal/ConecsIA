using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.RegularExpressions;

namespace ConecsIA.Infrastructure.Configuration.Entities;

public abstract class EntityConfigurationBase<TEntity> where TEntity : class
{
    protected static void ConfigureDefaults(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(ToSnakeCase(typeof(TEntity).Name), "public");
        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.Name));
            if (property.ClrType == typeof(string))
                property.SetMaxLength(2000);
        }
    }

    private static string ToSnakeCase(string value) =>
        Regex.Replace(value, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
}
