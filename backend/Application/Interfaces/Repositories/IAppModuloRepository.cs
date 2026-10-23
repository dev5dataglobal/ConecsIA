using ConecsIA.Application.Interfaces;
using ConecsIA.Domain.Entities;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IAppModuloRepository : IGenericRepository<AppModulo>
{
    Task<List<AppModulo>> ListarAsync(CancellationToken cancellationToken = default);
    Task<AppModulo?> ObtenerPorIdAsync(int idAppModulo, CancellationToken cancellationToken = default);
    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId, CancellationToken cancellationToken = default);
    Task<List<AppModulo>> ObtenerActivosPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
}
