using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class AppModuloRepository(ConecsiaDbContext db) : GenericRepository<AppModulo>(db), IAppModuloRepository
{
    public Task<List<AppModulo>> ListarAsync(CancellationToken cancellationToken = default) =>
        Context.AppModulos.AsNoTracking().OrderBy(x => x.Orden).ThenBy(x => x.Nombre).ToListAsync(cancellationToken);

    public Task<AppModulo?> ObtenerPorIdAsync(int idAppModulo, CancellationToken cancellationToken = default) =>
        Context.AppModulos.SingleOrDefaultAsync(x => x.IdAppModulo == idAppModulo, cancellationToken);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId, CancellationToken cancellationToken = default) =>
        Context.AppModulos.AnyAsync(x => x.Codigo == codigo && (excluirId == null || x.IdAppModulo != excluirId), cancellationToken);

    public Task<List<AppModulo>> ObtenerActivosPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        Context.AppModulos.AsNoTracking().Where(x => x.Activo && ids.Contains(x.IdAppModulo)).ToListAsync(cancellationToken);
}
