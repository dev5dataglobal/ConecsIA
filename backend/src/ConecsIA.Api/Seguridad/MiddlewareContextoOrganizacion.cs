using System.Security.Claims;
using ConecsIA.Api.Errores;
using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.Modules.Identidad.Application;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConecsIA.Api.Seguridad;

/// <summary>
/// HU-007 (ADR-005): para cada petición autenticada a un endpoint protegido, deriva el contexto de
/// organización de una membresía activa. La cabecera X-Organizacion-Id solo selecciona entre
/// membresías verificadas; una selección ajena se rechaza con 403 y se audita.
/// </summary>
public sealed class MiddlewareContextoOrganizacion(RequestDelegate siguiente)
{
    public const string CodigoOrganizacionNoPermitida = "ORGANIZACION_NO_PERMITIDA";
    public const string CodigoSinMembresia = "SIN_MEMBRESIA";
    public const string CodigoOrganizacionNoSeleccionada = "ORGANIZACION_NO_SELECCIONADA";

    public async Task InvokeAsync(
        HttpContext http,
        ContextoSolicitud contexto,
        ResolvedorContexto resolvedor,
        IRegistroAuditoria auditoria,
        IUnidadDeTrabajo unidad,
        IProblemDetailsService problemas)
    {
        var endpoint = http.GetEndpoint();
        if (endpoint is null
            || endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
            || http.User.Identity?.IsAuthenticated != true
            || !Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            await siguiente(http);
            return;
        }

        if (endpoint.Metadata.GetMetadata<SinOrganizacionResueltaAttribute>() is not null)
        {
            var permisosSaas = await resolvedor.PermisosSaasDeUsuarioActivoAsync(usuarioId, http.RequestAborted);
            if (permisosSaas is null)
            {
                await EscribirAsync(http, problemas, StatusCodes.Status401Unauthorized, "No autenticado",
                    "La sesión no corresponde a un usuario activo.", codigo: null);
                return;
            }

            contexto.EstablecerUsuario(usuarioId, permisosSaas);
            await siguiente(http);
            return;
        }

        Guid? seleccion = null;
        if (http.Request.Headers.TryGetValue(CabecerasSeguridad.Organizacion, out var valor))
        {
            if (!Guid.TryParse(valor.ToString(), out var organizacion))
            {
                await EscribirAsync(http, problemas, StatusCodes.Status400BadRequest, "Organización inválida",
                    "La organización seleccionada no es un identificador válido.", CodigoOrganizacionNoPermitida);
                return;
            }

            seleccion = organizacion;
        }

        var resultado = await resolvedor.ResolverAsync(usuarioId, seleccion, http.RequestAborted);
        switch (resultado.Estado)
        {
            case EstadoResolucion.Resuelto:
                var m = resultado.Membresia!;
                contexto.Establecer(usuarioId, m.OrganizacionId, resultado.PermisosEfectivos!, m.AlcanceTotalSedes, m.SedesAsignadas, m.PuntosAsignados);
                await siguiente(http);
                return;

            case EstadoResolucion.UsuarioNoValido:
                await EscribirAsync(http, problemas, StatusCodes.Status401Unauthorized, "No autenticado",
                    "La sesión no corresponde a un usuario activo.", codigo: null);
                return;

            case EstadoResolucion.OrganizacionNoPermitida:
                contexto.EstablecerUsuario(usuarioId);
                auditoria.Registrar(new EntradaAuditoria(
                    "contexto.seleccionar_organizacion", "Organizacion", seleccion, ResultadoAuditoria.Denegado,
                    ValoresNuevos: new { OrganizacionSolicitada = seleccion }));
                await unidad.GuardarAsync(http.RequestAborted);
                await EscribirAsync(http, problemas, StatusCodes.Status403Forbidden, "Acceso denegado",
                    "No tienes una membresía activa en la organización seleccionada.", CodigoOrganizacionNoPermitida);
                return;

            case EstadoResolucion.OrganizacionNoSeleccionada:
                await EscribirAsync(http, problemas, StatusCodes.Status403Forbidden, "Organización no seleccionada",
                    "Tienes varias membresías: indica la organización con la cabecera X-Organizacion-Id.", CodigoOrganizacionNoSeleccionada);
                return;

            default:
                await EscribirAsync(http, problemas, StatusCodes.Status403Forbidden, "Acceso denegado",
                    "No tienes una membresía activa en una organización activa.", CodigoSinMembresia);
                return;
        }
    }

    private static async Task EscribirAsync(
        HttpContext http,
        IProblemDetailsService problemas,
        int estado,
        string titulo,
        string detalle,
        string? codigo)
    {
        http.Response.StatusCode = estado;
        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle };
        if (codigo is not null) problema.Extensions[DetallesProblema.ClaveCodigo] = codigo;
        await problemas.WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problema });
    }
}
