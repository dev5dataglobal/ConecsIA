using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Identidad;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Modules.Identidad.Infrastructure;

/// <summary>Usuarios miembros de la organización del contexto; activo = usuario y membresía activos.</summary>
internal sealed class ConsultaUsuariosOrganizacion(ConecsIADbContext contexto, IContextoSolicitud solicitud) : IConsultaUsuarios
{
    public async Task<IReadOnlyDictionary<Guid, bool>> ActivosEnOrganizacionAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancelacion)
    {
        var organizacion = solicitud.OrganizacionId;
        var lista = ids.Distinct().ToList();
        // Membresia no tiene filtro global: la organización se fija explícitamente con la del contexto.
        var filas = await contexto.Set<Membresia>().AsNoTracking()
            .Where(m => m.OrganizacionId == organizacion && lista.Contains(m.UsuarioId))
            .Join(contexto.Set<Usuario>().AsNoTracking(), m => m.UsuarioId, u => u.Id, (m, u) => new { u.Id, Activo = m.Activa && u.Activo })
            .ToListAsync(cancelacion);
        return filas.ToDictionary(f => f.Id, f => f.Activo);
    }
}
