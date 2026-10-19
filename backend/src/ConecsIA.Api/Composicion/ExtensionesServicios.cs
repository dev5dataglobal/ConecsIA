using ConecsIA.Api.Errores;
using ConecsIA.Api.Persistencia;
using ConecsIA.Api.Seguridad;
using ConecsIA.Api.Semillas;
using ConecsIA.Api.Validacion;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.Infrastructure.Tiempo;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Api.Composicion;

public static class ExtensionesServicios
{
    public const string CadenaAplicacion = "ConecsIA";
    public const string CadenaMigraciones = "ConecsIAMigraciones";

    public static IServiceCollection AgregarModulos(this IServiceCollection servicios, IConfiguration configuracion)
    {
        foreach (var modulo in CatalogoModulos.Todos)
        {
            modulo.RegistrarServicios(servicios, configuracion);
            servicios.AddValidatorsFromAssembly(modulo.GetType().Assembly, includeInternalTypes: true);
        }

        servicios.AddSingleton(new EnsambladosDelModelo(CatalogoModulos.Todos.Select(m => m.GetType().Assembly)));
        return servicios;
    }

    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        var cadena = configuracion.GetConnectionString(CadenaAplicacion)
                     ?? throw new InvalidOperationException($"Falta la cadena de conexión '{CadenaAplicacion}'.");

        servicios.AddDbContext<ConecsIADbContext>(opciones => OpcionesPersistencia.Configurar(opciones, cadena));
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IConjuntoSemillas, SemillasPrimeraVertical>();
        servicios.AddScoped<IConjuntoSemillas, SemillasPlanoSaas>();
        servicios.AddScoped<IConjuntoSemillas, SemillasContrasenas>();
        servicios.AddScoped<IConjuntoSemillas, SemillasRoles>();
        servicios.AddHealthChecks().AddDbContextCheck<ConecsIADbContext>("postgresql", tags: ["ready"]);

        // ADR-003: en producción las migraciones las aplica el pipeline con un script revisado.
        if (configuracion.GetValue<bool>("Persistencia:AplicarMigracionesAlIniciar"))
        {
            if (entorno.IsProduction())
            {
                throw new InvalidOperationException(
                    "Persistencia:AplicarMigracionesAlIniciar no está permitido en producción (ADR-003).");
            }

            servicios.AddHostedService<AplicadorMigraciones>();
        }

        return servicios;
    }

    public static IServiceCollection AgregarApi(this IServiceCollection servicios)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton<IClock, RelojSistema>();

        servicios
            .AddControllers(opciones =>
            {
                // Seguro por defecto: un controlador nuevo exige autenticación salvo [AllowAnonymous].
                opciones.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
                opciones.Filters.Add<FiltroValidacion>();
            })
            .ConfigureApiBehaviorOptions(opciones =>
            {
                // La validación la resuelve FluentValidation en FiltroValidacion (ADR-002).
                opciones.SuppressModelStateInvalidFilter = true;
            });

        servicios.AddProblemDetails(opciones => opciones.CustomizeProblemDetails = DetallesProblema.Enriquecer);
        servicios.AddExceptionHandler<ManejadorExcepciones>();
        servicios.AddOpenApi("v1");
        return servicios;
    }

    /// <summary>
    /// Autenticación y autorización (ADR-005). Todo controlador exige usuario autenticado por un
    /// filtro global (ver AgregarApi); los públicos se marcan con AllowAnonymous. Las rutas
    /// inexistentes siguen respondiendo 404. Permisos finos y alcance se verifican en los casos de
    /// uso con IContextoSolicitud.
    /// </summary>
    private const string EsquemaPorDefecto = "Compuesto";

    public static IServiceCollection AgregarSeguridad(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        IHostEnvironment entorno)
    {
        AutenticacionDesarrollo.ValidarEntorno(configuracion, entorno);

        servicios.AddScoped<ContextoSolicitud>();
        servicios.AddScoped<IContextoSolicitud>(sp => sp.GetRequiredService<ContextoSolicitud>());
        servicios.AddScoped<IContextoPlanoSaas>(sp => sp.GetRequiredService<ContextoSolicitud>());
        servicios.AddHttpContextAccessor();
        servicios.AddScoped<ISesionUsuario, SesionCookie>();

        // La cookie de sesión (ADR-006) es el esquema real. El de desarrollo (ADR-022) solo atiende
        // peticiones con su cabecera y solo si está habilitado, lo que fuera de Development/Testing
        // detiene el arranque.
        var desarrollo = AutenticacionDesarrollo.EstaHabilitada(configuracion);
        servicios
            .AddAuthentication(EsquemaPorDefecto)
            .AddPolicyScheme(EsquemaPorDefecto, displayName: null, opciones =>
                opciones.ForwardDefaultSelector = http =>
                    desarrollo && http.Request.Headers.ContainsKey(CabecerasSeguridad.UsuarioDesarrollo)
                        ? AutenticacionDesarrollo.Esquema
                        : AutenticacionCookie.Esquema)
            .AddCookie(AutenticacionCookie.Esquema, o => AutenticacionCookie.Configurar(o, entorno))
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, AutenticacionDesarrollo>(
                AutenticacionDesarrollo.Esquema, configureOptions: null);
        servicios.AddAntiforgery(o => MiddlewareCsrf.Configurar(o, entorno));
        servicios.AddAuthorization();
        return servicios;
    }
}
