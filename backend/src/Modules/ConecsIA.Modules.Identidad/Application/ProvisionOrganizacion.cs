using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Organizaciones;
using ConecsIA.SharedKernel.Tiempo;

namespace ConecsIA.Modules.Identidad.Application;

/// <summary>
/// HU-009: al dar de alta una organización copia las plantillas de rol (copias independientes) y
/// vincula al primer administrador con el rol de administración. Si el correo ya pertenece a un
/// usuario, se reutiliza (un usuario puede tener varias membresías, D-03b). El envío de la
/// invitación para definir contraseña es de HU-016/loop_integraciones (D-12).
/// </summary>
public sealed class ProvisionOrganizacion(IRepositorioIdentidad repositorio, IRepositorioRoles roles, IClock reloj)
    : IProvisionOrganizacion
{
    public const string PlantillaAdministrador = "administrador-organizacion";

    public async Task<Guid> ProvisionarAsync(
        Guid organizacionId, string correoAdministrador, string nombreAdministrador, CancellationToken cancelacion)
    {
        Guid? rolAdministrador = null;
        foreach (var plantilla in PlantillasRol.Todas)
        {
            var rol = Rol.Crear(organizacionId, plantilla.Nombre, plantilla.Descripcion, plantilla.Permisos, plantilla.Clave, reloj.UtcNow);
            roles.Agregar(rol);
            if (plantilla.Clave == PlantillaAdministrador) rolAdministrador = rol.Id;
        }

        var usuario = await repositorio.ObtenerPorCorreoParaActualizarAsync(Usuario.NormalizarCorreo(correoAdministrador), cancelacion);
        if (usuario is null)
        {
            usuario = Usuario.Crear(correoAdministrador, nombreAdministrador);
            repositorio.AgregarUsuario(usuario);
        }

        repositorio.AgregarMembresia(Membresia.Crear(
            usuario.Id, organizacionId, permisos: [], alcanceTotalSedes: true, sedesAsignadas: [], roles: [rolAdministrador!.Value]));
        return usuario.Id;
    }
}
