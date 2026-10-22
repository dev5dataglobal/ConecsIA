using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class MovimientoDispositivoRepository(ConecsiaDbContext db) : GenericRepository<MovimientoDispositivo>(db), IMovimientoDispositivoRepository
{
}
