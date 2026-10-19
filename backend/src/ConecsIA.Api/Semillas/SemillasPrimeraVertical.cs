using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Estructura.Domain;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.Modules.Organizaciones.Domain;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Api.Semillas;

/// <summary>
/// Dos organizaciones sintéticas para probar a mano HU-013 con aislamiento y alcance (CA-01, CA-02).
/// Identificadores fijos: ejecutar dos veces no duplica nada. Mientras no exista HU-009 (alta de
/// organizaciones) ni HU-016/017 (usuarios y roles), esta es la única forma de tenerlos.
/// </summary>
public sealed class SemillasPrimeraVertical(ConecsIADbContext contexto, IClock reloj) : IConjuntoSemillas
{
    public static readonly Guid OrgPinos = Guid.Parse("0199f2a0-0000-7000-8000-000000000001");
    public static readonly Guid OrgAndes = Guid.Parse("0199f2a0-0000-7000-8000-000000000002");
    public static readonly Guid AdminPinos = Guid.Parse("0199f2a0-0000-7000-8000-000000000101");
    public static readonly Guid OperadorPinos = Guid.Parse("0199f2a0-0000-7000-8000-000000000102");
    public static readonly Guid AdminAndes = Guid.Parse("0199f2a0-0000-7000-8000-000000000103");

    public async Task SembrarAsync(CancellationToken cancelacion)
    {
        if (await contexto.Set<Organizacion>().AnyAsync(o => o.Id == OrgPinos, cancelacion)) return;

        var ahora = reloj.UtcNow;
        var pinos = Organizacion.CrearConId(OrgPinos, "Conjunto Residencial Los Pinos (sintético)", ahora);
        var andes = Organizacion.CrearConId(OrgAndes, "Corporativo Andes (sintético)", ahora);
        var porteria = Sede.Crear(OrgPinos, "Portería Norte", "Calle 1 # 2-3", "Bogotá", "Colombia", "America/Bogota", ahora);
        var club = Sede.Crear(OrgPinos, "Club House", "Calle 1 # 2-5", "Bogotá", "Colombia", "America/Bogota", ahora);
        var torre = Sede.Crear(OrgAndes, "Torre Lima", "Av. Larco 100", "Lima", "Perú", "America/Lima", ahora);

        string[] administrador = [Permisos.SedesConsultar, Permisos.SedesCrear, Permisos.SedesModificar, Permisos.SedesInactivar];

        contexto.AddRange(pinos, andes, porteria, club, torre);
        contexto.AddRange(
            Usuario.CrearConId(AdminPinos, "admin.pinos@ejemplo.test", "Ana Admin (Los Pinos)"),
            Usuario.CrearConId(OperadorPinos, "operador.pinos@ejemplo.test", "Óscar Operador (Portería Norte)"),
            Usuario.CrearConId(AdminAndes, "admin.andes@ejemplo.test", "Andrés Admin (Andes)"));
        contexto.AddRange(
            Membresia.Crear(AdminPinos, OrgPinos, administrador, alcanceTotalSedes: true, sedesAsignadas: []),
            Membresia.Crear(OperadorPinos, OrgPinos, [Permisos.SedesConsultar], alcanceTotalSedes: false, sedesAsignadas: [porteria.Id]),
            Membresia.Crear(AdminAndes, OrgAndes, administrador, alcanceTotalSedes: true, sedesAsignadas: []));
        await contexto.SaveChangesAsync(cancelacion);
    }
}
