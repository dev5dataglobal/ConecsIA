using ConecsIA.SharedKernel.Persistencia;

namespace ConecsIA.Infrastructure.Persistencia;

public sealed class UnidadDeTrabajo(ConecsIADbContext contexto) : IUnidadDeTrabajo
{
    public Task GuardarAsync(CancellationToken cancelacion) => contexto.SaveChangesAsync(cancelacion);
}
