using ConecsIA.Application.Interfaces;

namespace ConecsIA.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IAlertaRepository AlertaRepository { get; }
    IAppModuloRepository AppModuloRepository { get; }
    IAlertaTransicionRepository AlertaTransicionRepository { get; }
    IArchivoRepository ArchivoRepository { get; }
    IAreaRepository AreaRepository { get; }
    IAreaResponsableRepository AreaResponsableRepository { get; }
    IAuditoriaRepository AuditoriaRepository { get; }
    IAutorizacionRepository AutorizacionRepository { get; }
    IAutorizacionDispositivoRepository AutorizacionDispositivoRepository { get; }
    IAutorizacionHorarioRepository AutorizacionHorarioRepository { get; }
    IAutorizacionTransicionRepository AutorizacionTransicionRepository { get; }
    IAutorizacionVehiculoRepository AutorizacionVehiculoRepository { get; }
    IAutorizacionZonaRepository AutorizacionZonaRepository { get; }
    ICiudadRepository CiudadRepository { get; }
    IDispositivoRepository DispositivoRepository { get; }
    IInvitacionEnvioRepository InvitacionEnvioRepository { get; }
    IMembresiaOrganizacionRepository MembresiaOrganizacionRepository { get; }
    IMembresiaPuntoAccesoRepository MembresiaPuntoAccesoRepository { get; }
    IMembresiaRolRepository MembresiaRolRepository { get; }
    IMembresiaSedeRepository MembresiaSedeRepository { get; }
    IModuloRepository ModuloRepository { get; }
    IModuloPermisoRepository ModuloPermisoRepository { get; }
    IMovimientoAccesoRepository MovimientoAccesoRepository { get; }
    IMovimientoDispositivoRepository MovimientoDispositivoRepository { get; }
    IMovimientoVehiculoRepository MovimientoVehiculoRepository { get; }
    INotificacionRepository NotificacionRepository { get; }
    INotificacionDestinatarioRepository NotificacionDestinatarioRepository { get; }
    IOrganizacionRepository OrganizacionRepository { get; }
    IOrganizacionModuloRepository OrganizacionModuloRepository { get; }
    IOutboxRepository OutboxRepository { get; }
    IParametroOrganizacionRepository ParametroOrganizacionRepository { get; }
    IPermanenciaRepository PermanenciaRepository { get; }
    IPermisoRepository PermisoRepository { get; }
    IPersonaRepository PersonaRepository { get; }
    IPersonaTelefonoRepository PersonaTelefonoRepository { get; }
    IPersonaVehiculoRepository PersonaVehiculoRepository { get; }
    IPlanRepository PlanRepository { get; }
    IPlanModuloRepository PlanModuloRepository { get; }
    IPlantillaNotificacionRepository PlantillaNotificacionRepository { get; }
    IPlantillaRolRepository PlantillaRolRepository { get; }
    IPlantillaRolPermisoRepository PlantillaRolPermisoRepository { get; }
    IRestriccionAccesoRepository RestriccionAccesoRepository { get; }
    IRolRepository RolRepository { get; }
    IRolPermisoRepository RolPermisoRepository { get; }
    ISedeRepository SedeRepository { get; }
    ISolicitudIdempotenteRepository SolicitudIdempotenteRepository { get; }
    ITipoDispositivoRepository TipoDispositivoRepository { get; }
    ITipoDocumentoRepository TipoDocumentoRepository { get; }
    ITipoOrganizacionRepository TipoOrganizacionRepository { get; }
    ITipoPersonaRepository TipoPersonaRepository { get; }
    ITokenQrRepository TokenQrRepository { get; }
    IUsuarioRepository UsuarioRepository { get; }
    IVehiculoRepository VehiculoRepository { get; }
    IVisitaRepository VisitaRepository { get; }
    IZonaRepository ZonaRepository { get; }
    IZonaEmpresarialRepository ZonaEmpresarialRepository { get; }
    IZonaEmpresarialOrganizacionRepository ZonaEmpresarialOrganizacionRepository { get; }
    Task<int> SaveAsync();
}
