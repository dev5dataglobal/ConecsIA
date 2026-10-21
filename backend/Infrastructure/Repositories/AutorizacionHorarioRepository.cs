using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;

namespace ConecsIA.Infrastructure.Repositories;

public sealed class AutorizacionHorarioRepository(ConecsiaDbContext db) : GenericRepository<AutorizacionHorario>(db), IAutorizacionHorarioRepository
{
}
