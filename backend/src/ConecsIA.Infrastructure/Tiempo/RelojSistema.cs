using ConecsIA.SharedKernel.Tiempo;

namespace ConecsIA.Infrastructure.Tiempo;

public sealed class RelojSistema(TimeProvider proveedor) : IClock
{
    public DateTimeOffset UtcNow => proveedor.GetUtcNow();
}
