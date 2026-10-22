using ConecsIA.Application.Interfaces.Repositories;
using ConecsIA.Infrastructure.Data;
using ConecsIA.Infrastructure.Repositories;

namespace ConecsIA.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private IAlertaRepository? _alertaRepository;
    private IAlertaTransicionRepository? _alertaTransicionRepository;
    private IArchivoRepository? _archivoRepository;
    private IAreaRepository? _areaRepository;
    private IAreaResponsableRepository? _areaResponsableRepository;
    private IAuditoriaRepository? _auditoriaRepository;
    private IAutorizacionRepository? _autorizacionRepository;
    private IAutorizacionDispositivoRepository? _autorizacionDispositivoRepository;
    private IAutorizacionHorarioRepository? _autorizacionHorarioRepository;
    private IAutorizacionTransicionRepository? _autorizacionTransicionRepository;
    private IAutorizacionVehiculoRepository? _autorizacionVehiculoRepository;
    private IAutorizacionZonaRepository? _autorizacionZonaRepository;
    private ICiudadRepository? _ciudadRepository;
    private IDispositivoRepository? _dispositivoRepository;
    private IInvitacionEnvioRepository? _invitacionEnvioRepository;
    private IMembresiaOrganizacionRepository? _membresiaOrganizacionRepository;
    private IMembresiaPuntoAccesoRepository? _membresiaPuntoAccesoRepository;
    private IMembresiaRolRepository? _membresiaRolRepository;
    private IMembresiaSedeRepository? _membresiaSedeRepository;
    private IModuloRepository? _moduloRepository;
    private IMovimientoAccesoRepository? _movimientoAccesoRepository;
    private IMovimientoDispositivoRepository? _movimientoDispositivoRepository;
    private IMovimientoVehiculoRepository? _movimientoVehiculoRepository;
    private INotificacionRepository? _notificacionRepository;
    private INotificacionDestinatarioRepository? _notificacionDestinatarioRepository;
    private IOrganizacionRepository? _organizacionRepository;
    private IOrganizacionModuloRepository? _organizacionModuloRepository;
    private IOutboxRepository? _outboxRepository;
    private IParametroOrganizacionRepository? _parametroOrganizacionRepository;
    private IPermanenciaRepository? _permanenciaRepository;
    private IPermisoRepository? _permisoRepository;
    private IPersonaRepository? _personaRepository;
    private IPersonaTelefonoRepository? _personaTelefonoRepository;
    private IPersonaVehiculoRepository? _personaVehiculoRepository;
    private IPlanRepository? _planRepository;
    private IPlanModuloRepository? _planModuloRepository;
    private IPlantillaNotificacionRepository? _plantillaNotificacionRepository;
    private IPlantillaRolRepository? _plantillaRolRepository;
    private IPlantillaRolPermisoRepository? _plantillaRolPermisoRepository;
    private IRestriccionAccesoRepository? _restriccionAccesoRepository;
    private IRolRepository? _rolRepository;
    private IRolPermisoRepository? _rolPermisoRepository;
    private ISedeRepository? _sedeRepository;
    private ISolicitudIdempotenteRepository? _solicitudIdempotenteRepository;
    private ITipoDispositivoRepository? _tipoDispositivoRepository;
    private ITipoDocumentoRepository? _tipoDocumentoRepository;
    private ITipoOrganizacionRepository? _tipoOrganizacionRepository;
    private ITipoPersonaRepository? _tipoPersonaRepository;
    private ITokenQrRepository? _tokenQrRepository;
    private IUsuarioRepository? _usuarioRepository;
    private IVehiculoRepository? _vehiculoRepository;
    private IVisitaRepository? _visitaRepository;
    private IZonaRepository? _zonaRepository;

    private readonly ConecsiaDbContext _context;

    public UnitOfWork(ConecsiaDbContext context)
    {
        _context = context;
    }

    public IAlertaRepository AlertaRepository
    {
        get
        {
            if (_alertaRepository == null)
            {
                _alertaRepository = new AlertaRepository(_context);
            }

            return _alertaRepository;
        }
    }

    public IAlertaTransicionRepository AlertaTransicionRepository
    {
        get
        {
            if (_alertaTransicionRepository == null)
            {
                _alertaTransicionRepository = new AlertaTransicionRepository(_context);
            }

            return _alertaTransicionRepository;
        }
    }

    public IArchivoRepository ArchivoRepository
    {
        get
        {
            if (_archivoRepository == null)
            {
                _archivoRepository = new ArchivoRepository(_context);
            }

            return _archivoRepository;
        }
    }

    public IAreaRepository AreaRepository
    {
        get
        {
            if (_areaRepository == null)
            {
                _areaRepository = new AreaRepository(_context);
            }

            return _areaRepository;
        }
    }

    public IAreaResponsableRepository AreaResponsableRepository
    {
        get
        {
            if (_areaResponsableRepository == null)
            {
                _areaResponsableRepository = new AreaResponsableRepository(_context);
            }

            return _areaResponsableRepository;
        }
    }

    public IAuditoriaRepository AuditoriaRepository
    {
        get
        {
            if (_auditoriaRepository == null)
            {
                _auditoriaRepository = new AuditoriaRepository(_context);
            }

            return _auditoriaRepository;
        }
    }

    public IAutorizacionRepository AutorizacionRepository
    {
        get
        {
            if (_autorizacionRepository == null)
            {
                _autorizacionRepository = new AutorizacionRepository(_context);
            }

            return _autorizacionRepository;
        }
    }

    public IAutorizacionDispositivoRepository AutorizacionDispositivoRepository
    {
        get
        {
            if (_autorizacionDispositivoRepository == null)
            {
                _autorizacionDispositivoRepository = new AutorizacionDispositivoRepository(_context);
            }

            return _autorizacionDispositivoRepository;
        }
    }

    public IAutorizacionHorarioRepository AutorizacionHorarioRepository
    {
        get
        {
            if (_autorizacionHorarioRepository == null)
            {
                _autorizacionHorarioRepository = new AutorizacionHorarioRepository(_context);
            }

            return _autorizacionHorarioRepository;
        }
    }

    public IAutorizacionTransicionRepository AutorizacionTransicionRepository
    {
        get
        {
            if (_autorizacionTransicionRepository == null)
            {
                _autorizacionTransicionRepository = new AutorizacionTransicionRepository(_context);
            }

            return _autorizacionTransicionRepository;
        }
    }

    public IAutorizacionVehiculoRepository AutorizacionVehiculoRepository
    {
        get
        {
            if (_autorizacionVehiculoRepository == null)
            {
                _autorizacionVehiculoRepository = new AutorizacionVehiculoRepository(_context);
            }

            return _autorizacionVehiculoRepository;
        }
    }

    public IAutorizacionZonaRepository AutorizacionZonaRepository
    {
        get
        {
            if (_autorizacionZonaRepository == null)
            {
                _autorizacionZonaRepository = new AutorizacionZonaRepository(_context);
            }

            return _autorizacionZonaRepository;
        }
    }

    public ICiudadRepository CiudadRepository
    {
        get
        {
            if (_ciudadRepository == null)
            {
                _ciudadRepository = new CiudadRepository(_context);
            }

            return _ciudadRepository;
        }
    }

    public IDispositivoRepository DispositivoRepository
    {
        get
        {
            if (_dispositivoRepository == null)
            {
                _dispositivoRepository = new DispositivoRepository(_context);
            }

            return _dispositivoRepository;
        }
    }

    public IInvitacionEnvioRepository InvitacionEnvioRepository
    {
        get
        {
            if (_invitacionEnvioRepository == null)
            {
                _invitacionEnvioRepository = new InvitacionEnvioRepository(_context);
            }

            return _invitacionEnvioRepository;
        }
    }

    public IMembresiaOrganizacionRepository MembresiaOrganizacionRepository
    {
        get
        {
            if (_membresiaOrganizacionRepository == null)
            {
                _membresiaOrganizacionRepository = new MembresiaOrganizacionRepository(_context);
            }

            return _membresiaOrganizacionRepository;
        }
    }

    public IMembresiaPuntoAccesoRepository MembresiaPuntoAccesoRepository
    {
        get
        {
            if (_membresiaPuntoAccesoRepository == null)
            {
                _membresiaPuntoAccesoRepository = new MembresiaPuntoAccesoRepository(_context);
            }

            return _membresiaPuntoAccesoRepository;
        }
    }

    public IMembresiaRolRepository MembresiaRolRepository
    {
        get
        {
            if (_membresiaRolRepository == null)
            {
                _membresiaRolRepository = new MembresiaRolRepository(_context);
            }

            return _membresiaRolRepository;
        }
    }

    public IMembresiaSedeRepository MembresiaSedeRepository
    {
        get
        {
            if (_membresiaSedeRepository == null)
            {
                _membresiaSedeRepository = new MembresiaSedeRepository(_context);
            }

            return _membresiaSedeRepository;
        }
    }

    public IModuloRepository ModuloRepository
    {
        get
        {
            if (_moduloRepository == null)
            {
                _moduloRepository = new ModuloRepository(_context);
            }

            return _moduloRepository;
        }
    }

    public IMovimientoAccesoRepository MovimientoAccesoRepository
    {
        get
        {
            if (_movimientoAccesoRepository == null)
            {
                _movimientoAccesoRepository = new MovimientoAccesoRepository(_context);
            }

            return _movimientoAccesoRepository;
        }
    }

    public IMovimientoDispositivoRepository MovimientoDispositivoRepository
    {
        get
        {
            if (_movimientoDispositivoRepository == null)
            {
                _movimientoDispositivoRepository = new MovimientoDispositivoRepository(_context);
            }

            return _movimientoDispositivoRepository;
        }
    }

    public IMovimientoVehiculoRepository MovimientoVehiculoRepository
    {
        get
        {
            if (_movimientoVehiculoRepository == null)
            {
                _movimientoVehiculoRepository = new MovimientoVehiculoRepository(_context);
            }

            return _movimientoVehiculoRepository;
        }
    }

    public INotificacionRepository NotificacionRepository
    {
        get
        {
            if (_notificacionRepository == null)
            {
                _notificacionRepository = new NotificacionRepository(_context);
            }

            return _notificacionRepository;
        }
    }

    public INotificacionDestinatarioRepository NotificacionDestinatarioRepository
    {
        get
        {
            if (_notificacionDestinatarioRepository == null)
            {
                _notificacionDestinatarioRepository = new NotificacionDestinatarioRepository(_context);
            }

            return _notificacionDestinatarioRepository;
        }
    }

    public IOrganizacionRepository OrganizacionRepository
    {
        get
        {
            if (_organizacionRepository == null)
            {
                _organizacionRepository = new OrganizacionRepository(_context);
            }

            return _organizacionRepository;
        }
    }

    public IOrganizacionModuloRepository OrganizacionModuloRepository
    {
        get
        {
            if (_organizacionModuloRepository == null)
            {
                _organizacionModuloRepository = new OrganizacionModuloRepository(_context);
            }

            return _organizacionModuloRepository;
        }
    }

    public IOutboxRepository OutboxRepository
    {
        get
        {
            if (_outboxRepository == null)
            {
                _outboxRepository = new OutboxRepository(_context);
            }

            return _outboxRepository;
        }
    }

    public IParametroOrganizacionRepository ParametroOrganizacionRepository
    {
        get
        {
            if (_parametroOrganizacionRepository == null)
            {
                _parametroOrganizacionRepository = new ParametroOrganizacionRepository(_context);
            }

            return _parametroOrganizacionRepository;
        }
    }

    public IPermanenciaRepository PermanenciaRepository
    {
        get
        {
            if (_permanenciaRepository == null)
            {
                _permanenciaRepository = new PermanenciaRepository(_context);
            }

            return _permanenciaRepository;
        }
    }

    public IPermisoRepository PermisoRepository
    {
        get
        {
            if (_permisoRepository == null)
            {
                _permisoRepository = new PermisoRepository(_context);
            }

            return _permisoRepository;
        }
    }

    public IPersonaRepository PersonaRepository
    {
        get
        {
            if (_personaRepository == null)
            {
                _personaRepository = new PersonaRepository(_context);
            }

            return _personaRepository;
        }
    }

    public IPersonaTelefonoRepository PersonaTelefonoRepository
    {
        get
        {
            if (_personaTelefonoRepository == null)
            {
                _personaTelefonoRepository = new PersonaTelefonoRepository(_context);
            }

            return _personaTelefonoRepository;
        }
    }

    public IPersonaVehiculoRepository PersonaVehiculoRepository
    {
        get
        {
            if (_personaVehiculoRepository == null)
            {
                _personaVehiculoRepository = new PersonaVehiculoRepository(_context);
            }

            return _personaVehiculoRepository;
        }
    }

    public IPlanRepository PlanRepository
    {
        get
        {
            if (_planRepository == null)
            {
                _planRepository = new PlanRepository(_context);
            }

            return _planRepository;
        }
    }

    public IPlanModuloRepository PlanModuloRepository
    {
        get
        {
            if (_planModuloRepository == null)
            {
                _planModuloRepository = new PlanModuloRepository(_context);
            }

            return _planModuloRepository;
        }
    }

    public IPlantillaNotificacionRepository PlantillaNotificacionRepository
    {
        get
        {
            if (_plantillaNotificacionRepository == null)
            {
                _plantillaNotificacionRepository = new PlantillaNotificacionRepository(_context);
            }

            return _plantillaNotificacionRepository;
        }
    }

    public IPlantillaRolRepository PlantillaRolRepository
    {
        get
        {
            if (_plantillaRolRepository == null)
            {
                _plantillaRolRepository = new PlantillaRolRepository(_context);
            }

            return _plantillaRolRepository;
        }
    }

    public IPlantillaRolPermisoRepository PlantillaRolPermisoRepository
    {
        get
        {
            if (_plantillaRolPermisoRepository == null)
            {
                _plantillaRolPermisoRepository = new PlantillaRolPermisoRepository(_context);
            }

            return _plantillaRolPermisoRepository;
        }
    }

    public IRestriccionAccesoRepository RestriccionAccesoRepository
    {
        get
        {
            if (_restriccionAccesoRepository == null)
            {
                _restriccionAccesoRepository = new RestriccionAccesoRepository(_context);
            }

            return _restriccionAccesoRepository;
        }
    }

    public IRolRepository RolRepository
    {
        get
        {
            if (_rolRepository == null)
            {
                _rolRepository = new RolRepository(_context);
            }

            return _rolRepository;
        }
    }

    public IRolPermisoRepository RolPermisoRepository
    {
        get
        {
            if (_rolPermisoRepository == null)
            {
                _rolPermisoRepository = new RolPermisoRepository(_context);
            }

            return _rolPermisoRepository;
        }
    }

    public ISedeRepository SedeRepository
    {
        get
        {
            if (_sedeRepository == null)
            {
                _sedeRepository = new SedeRepository(_context);
            }

            return _sedeRepository;
        }
    }

    public ISolicitudIdempotenteRepository SolicitudIdempotenteRepository
    {
        get
        {
            if (_solicitudIdempotenteRepository == null)
            {
                _solicitudIdempotenteRepository = new SolicitudIdempotenteRepository(_context);
            }

            return _solicitudIdempotenteRepository;
        }
    }

    public ITipoDispositivoRepository TipoDispositivoRepository
    {
        get
        {
            if (_tipoDispositivoRepository == null)
            {
                _tipoDispositivoRepository = new TipoDispositivoRepository(_context);
            }

            return _tipoDispositivoRepository;
        }
    }

    public ITipoDocumentoRepository TipoDocumentoRepository
    {
        get
        {
            if (_tipoDocumentoRepository == null)
            {
                _tipoDocumentoRepository = new TipoDocumentoRepository(_context);
            }

            return _tipoDocumentoRepository;
        }
    }

    public ITipoOrganizacionRepository TipoOrganizacionRepository
    {
        get
        {
            if (_tipoOrganizacionRepository == null)
            {
                _tipoOrganizacionRepository = new TipoOrganizacionRepository(_context);
            }

            return _tipoOrganizacionRepository;
        }
    }

    public ITipoPersonaRepository TipoPersonaRepository
    {
        get
        {
            if (_tipoPersonaRepository == null)
            {
                _tipoPersonaRepository = new TipoPersonaRepository(_context);
            }

            return _tipoPersonaRepository;
        }
    }

    public ITokenQrRepository TokenQrRepository
    {
        get
        {
            if (_tokenQrRepository == null)
            {
                _tokenQrRepository = new TokenQrRepository(_context);
            }

            return _tokenQrRepository;
        }
    }

    public IUsuarioRepository UsuarioRepository
    {
        get
        {
            if (_usuarioRepository == null)
            {
                _usuarioRepository = new UsuarioRepository(_context);
            }

            return _usuarioRepository;
        }
    }

    public IVehiculoRepository VehiculoRepository
    {
        get
        {
            if (_vehiculoRepository == null)
            {
                _vehiculoRepository = new VehiculoRepository(_context);
            }

            return _vehiculoRepository;
        }
    }

    public IVisitaRepository VisitaRepository
    {
        get
        {
            if (_visitaRepository == null)
            {
                _visitaRepository = new VisitaRepository(_context);
            }

            return _visitaRepository;
        }
    }

    public IZonaRepository ZonaRepository
    {
        get
        {
            if (_zonaRepository == null)
            {
                _zonaRepository = new ZonaRepository(_context);
            }

            return _zonaRepository;
        }
    }

    public async Task<int> SaveAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
