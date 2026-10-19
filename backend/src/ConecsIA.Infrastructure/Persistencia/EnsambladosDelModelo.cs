using System.Reflection;

namespace ConecsIA.Infrastructure.Persistencia;

/// <summary>Ensamblados de módulo que aportan configuraciones de entidades al modelo.</summary>
public sealed class EnsambladosDelModelo(IEnumerable<Assembly> ensamblados)
{
    public IReadOnlyList<Assembly> Todos { get; } = ensamblados.Distinct().ToList();
}
