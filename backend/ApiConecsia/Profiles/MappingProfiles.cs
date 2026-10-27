using AutoMapper;
using ConecsIA.Application.DTOs;
using ConecsIA.Domain.Entities;

namespace ApiConecsia.Profiles;

public sealed class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<Zona, ZonaDto>().ReverseMap();
        CreateMap<Visita, VisitaDto>().ReverseMap();
        CreateMap<Vehiculo, VehiculoDto>().ReverseMap();
        CreateMap<Usuario, UsuarioDto>().ReverseMap();
        CreateMap<TokenQr, TokenQrDto>().ReverseMap();
        CreateMap<TipoPersona, TipoPersonaDto>().ReverseMap();
        CreateMap<TipoOrganizacion, TipoOrganizacionDto>().ReverseMap();
        CreateMap<TipoDocumento, TipoDocumentoDto>().ReverseMap();
        CreateMap<SolicitudIdempotente, SolicitudIdempotenteDto>().ReverseMap();
        CreateMap<Sede, SedeDto>().ReverseMap();
        CreateMap<RolPermiso, RolPermisoDto>().ReverseMap();
        CreateMap<Rol, RolDto>().ReverseMap();
        CreateMap<RestriccionAcceso, RestriccionAccesoDto>().ReverseMap();
        CreateMap<PlantillaRolPermiso, PlantillaRolPermisoDto>().ReverseMap();
        CreateMap<PlantillaRol, PlantillaRolDto>().ReverseMap();
        CreateMap<PlantillaNotificacion, PlantillaNotificacionDto>().ReverseMap();
        CreateMap<PlanModulo, PlanModuloDto>().ReverseMap();
        CreateMap<Plan, PlanDto>().ReverseMap();
        CreateMap<PersonaVehiculo, PersonaVehiculoDto>().ReverseMap();
        CreateMap<PersonaTelefono, PersonaTelefonoDto>().ReverseMap();
        CreateMap<Persona, PersonaDto>().ReverseMap();
        CreateMap<Permiso, PermisoDto>().ReverseMap();
        CreateMap<Permanencia, PermanenciaDto>().ReverseMap();
        CreateMap<ParametroOrganizacion, ParametroOrganizacionDto>().ReverseMap();
        CreateMap<Outbox, OutboxDto>().ReverseMap();
        CreateMap<OrganizacionModulo, OrganizacionModuloDto>().ReverseMap();
        CreateMap<Organizacion, OrganizacionDto>().ReverseMap();
        CreateMap<Notificacion, NotificacionDto>().ReverseMap();
        CreateMap<NotificacionDestinatario, NotificacionDestinatarioDto>().ReverseMap();
        CreateMap<MovimientoVehiculo, MovimientoVehiculoDto>().ReverseMap();
        CreateMap<MovimientoDispositivo, MovimientoDispositivoDto>().ReverseMap();
        CreateMap<MovimientoAcceso, MovimientoAccesoDto>().ReverseMap();
        CreateMap<Modulo, ModuloDto>().ReverseMap();
        CreateMap<MembresiaSede, MembresiaSedeDto>().ReverseMap();
        CreateMap<MembresiaRol, MembresiaRolDto>().ReverseMap();
        CreateMap<MembresiaPuntoAcceso, MembresiaPuntoAccesoDto>().ReverseMap();
        CreateMap<MembresiaOrganizacion, MembresiaOrganizacionDto>().ReverseMap();
        CreateMap<InvitacionEnvio, InvitacionEnvioDto>().ReverseMap();
        CreateMap<Ciudad, CiudadDto>().ReverseMap();
        CreateMap<AutorizacionZona, AutorizacionZonaDto>().ReverseMap();
        CreateMap<AutorizacionVehiculo, AutorizacionVehiculoDto>().ReverseMap();
        CreateMap<AutorizacionTransicion, AutorizacionTransicionDto>().ReverseMap();
        CreateMap<AutorizacionHorario, AutorizacionHorarioDto>().ReverseMap();
        CreateMap<Autorizacion, AutorizacionDto>().ReverseMap();
        CreateMap<AutorizacionDispositivo, AutorizacionDispositivoDto>().ReverseMap();
        CreateMap<Auditoria, AuditoriaDto>().ReverseMap();
        CreateMap<AreaResponsable, AreaResponsableDto>().ReverseMap();
        CreateMap<Area, AreaDto>().ReverseMap();
        CreateMap<Archivo, ArchivoDto>().ReverseMap();
        CreateMap<AlertaTransicion, AlertaTransicionDto>().ReverseMap();
        CreateMap<Alerta, AlertaDto>().ReverseMap();
    }
}
