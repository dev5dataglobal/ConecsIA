using ConecsIA.Infrastructure.Seguridad;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Auditoria;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Persistencia;
using ConecsIA.SharedKernel.Seguridad;
using ConecsIA.SharedKernel.Tiempo;
using FluentValidation;

namespace ConecsIA.Modules.Identidad.Application;

public sealed record SolicitudIngreso(string? Correo, string? Contrasena);

public sealed record SesionDto(Guid Id, DateTimeOffset IniciadaEn, DateTimeOffset ExpiraEn, bool Actual);

public interface IHasherContrasenas
{
    string Hashear(string contrasena);

    /// <returns>Si coincide y, en ese caso, si el hash debe regenerarse con parámetros actuales.</returns>
    (bool Valida, bool RequiereRehash) Verificar(string hash, string contrasena);
}

/// <summary>
/// HU-006: inicio y cierre de sesión (ADR-006, opción A). Respuesta genérica ante cualquier fallo de
/// credenciales; bloqueo temporal tras <see cref="PoliticaAutenticacion.IntentosAntesDeBloquear"/> fallos.
/// La auditoría nunca recibe la contraseña ni el correo intentado.
/// </summary>
public sealed class ServicioIngreso(
    IRepositorioIdentidad repositorio,
    IHasherContrasenas hasher,
    ISesionUsuario sesion,
    ContextoSolicitud contexto,
    IRegistroAuditoria auditoria,
    IUnidadDeTrabajo unidad,
    IClock reloj)
{
    public const string AccionIngreso = "sesion.iniciar";
    public const string AccionBloqueo = "sesion.bloquear";
    public const string AccionCierre = "sesion.cerrar";
    public const string AccionRevocar = "sesion.revocar";

    // Se verifica contra este hash cuando el usuario no existe, para igualar el tiempo de respuesta.
    private static string? hashSenuelo;

    public async Task IngresarAsync(SolicitudIngreso solicitud, CancellationToken cancelacion)
    {
        var ahora = reloj.UtcNow;
        var usuario = await repositorio.ObtenerPorCorreoParaActualizarAsync(
            Usuario.NormalizarCorreo(solicitud.Correo!), cancelacion);

        if (usuario is null)
        {
            hashSenuelo ??= hasher.Hashear(Guid.NewGuid().ToString("N"));
            hasher.Verificar(hashSenuelo, solicitud.Contrasena!);
            await FallarAsync(null, cancelacion);
        }

        if (usuario!.EstaBloqueado(ahora))
        {
            Auditar(AccionIngreso, usuario.Id, ResultadoAuditoria.Denegado, "bloqueado");
            await unidad.GuardarAsync(cancelacion);
            throw new CuentaBloqueadaException();
        }

        var (valida, requiereRehash) = usuario.HashContrasena is null
            ? (false, false)
            : hasher.Verificar(usuario.HashContrasena, solicitud.Contrasena!);

        if (!valida || !usuario.Activo)
        {
            if (usuario.RegistrarIntentoFallido(
                    ahora, PoliticaAutenticacion.IntentosAntesDeBloquear, PoliticaAutenticacion.DuracionBloqueo))
            {
                Auditar(AccionBloqueo, usuario.Id, ResultadoAuditoria.Denegado, "umbral_alcanzado");
            }

            await FallarAsync(usuario.Id, cancelacion);
        }

        usuario.RegistrarIngresoExitoso();
        if (requiereRehash) usuario.EstablecerHashSinRevocar(hasher.Hashear(solicitud.Contrasena!));

        var registro = SesionUsuario.Iniciar(usuario.Id, ahora, PoliticaAutenticacion.DuracionMaximaSesion);
        repositorio.AgregarSesion(registro);
        contexto.EstablecerUsuario(usuario.Id);
        Auditar(AccionIngreso, usuario.Id, ResultadoAuditoria.Exitoso, motivo: null);
        await unidad.GuardarAsync(cancelacion);
        await sesion.IniciarAsync(usuario.Id, registro.Id, usuario.SelloSeguridad);
    }

    /// <summary>Cierra la sesión actual: la revoca en el servidor y retira la cookie.</summary>
    public async Task CerrarAsync(Guid? usuarioId, Guid? sesionId, CancellationToken cancelacion)
    {
        if (usuarioId is { } id)
        {
            contexto.EstablecerUsuario(id);
            if (sesionId is { } sid
                && await repositorio.ObtenerSesionParaActualizarAsync(sid, cancelacion) is { } registro
                && registro.UsuarioId == id)
            {
                registro.Revocar(reloj.UtcNow);
            }

            Auditar(AccionCierre, id, ResultadoAuditoria.Exitoso, motivo: null);
            await unidad.GuardarAsync(cancelacion);
        }

        await sesion.CerrarAsync();
    }

    public async Task<IReadOnlyList<SesionDto>> ListarSesionesAsync(Guid usuarioId, Guid? sesionActual, CancellationToken cancelacion) =>
        (await repositorio.SesionesVigentesAsync(usuarioId, reloj.UtcNow, cancelacion))
            .Select(s => new SesionDto(s.Id, s.IniciadaEn, s.ExpiraEn, s.Id == sesionActual))
            .ToList();

    /// <summary>Revoca una de las sesiones propias. Una sesión ajena o inexistente responde 404.</summary>
    public async Task RevocarSesionAsync(Guid usuarioId, Guid sesionId, CancellationToken cancelacion)
    {
        var registro = await repositorio.ObtenerSesionParaActualizarAsync(sesionId, cancelacion);
        if (registro is null || registro.UsuarioId != usuarioId) throw new RecursoNoEncontradoException("la sesión");

        contexto.EstablecerUsuario(usuarioId);
        if (registro.Revocar(reloj.UtcNow))
        {
            auditoria.Registrar(new EntradaAuditoria(AccionRevocar, nameof(SesionUsuario), sesionId, ResultadoAuditoria.Exitoso));
            await unidad.GuardarAsync(cancelacion);
        }
    }

    private async Task FallarAsync(Guid? usuarioId, CancellationToken cancelacion)
    {
        Auditar(AccionIngreso, usuarioId, ResultadoAuditoria.Denegado, "credenciales_invalidas");
        await unidad.GuardarAsync(cancelacion);
        throw new CredencialesInvalidasException();
    }

    private void Auditar(string accion, Guid? usuarioId, ResultadoAuditoria resultado, string? motivo) =>
        auditoria.Registrar(new EntradaAuditoria(
            accion, nameof(Usuario), usuarioId, resultado,
            ValoresNuevos: motivo is null ? null : new { Motivo = motivo }));
}

internal sealed class ValidadorIngreso : AbstractValidator<SolicitudIngreso>
{
    public ValidadorIngreso()
    {
        RuleFor(s => s.Correo).NotEmpty().MaximumLength(254);
        RuleFor(s => s.Contrasena).NotEmpty().MaximumLength(512);
    }
}
