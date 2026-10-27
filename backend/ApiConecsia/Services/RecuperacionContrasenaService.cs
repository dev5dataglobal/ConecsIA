using System.Net;
using System.Text.Json;
using ApiConecsia.Helpers;
using ConecsIA.Application.Interfaces;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiConecsia.Services;

public enum ResultadoRecuperacion { Ok, CorreoNoConfigurado, TokenInvalido, ContrasenaInvalida }

/// <summary>
/// Restablecimiento de contraseña por correo. La solicitud responde igual exista o no la cuenta;
/// el token es de un solo uso, vence y solo se guarda su hash. Restablecer cambia el sello de seguridad,
/// lo que invalida los tokens de acceso y las sesiones renovables abiertas.
/// </summary>
public sealed class RecuperacionContrasenaService(
    ConecsiaDbContext db,
    IPasswordHasher<Usuario> passwordHasher,
    IEnviadorCorreo correo,
    IOptions<SesionOptions> opciones,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RecuperacionContrasenaService> logger)
{
    private readonly SesionOptions _opciones = opciones.Value;

    public async Task<ResultadoRecuperacion> SolicitarAsync(string correoIngresado, CancellationToken ct)
    {
        // Se comprueba antes de buscar la cuenta: la respuesta no depende de que el correo exista.
        if (!correo.Configurado || string.IsNullOrWhiteSpace(_opciones.UrlFrontend))
        {
            logger.LogWarning("Solicitud de recuperación de contraseña rechazada: el envío de correo o Sesion:UrlFrontend no están configurados.");
            return ResultadoRecuperacion.CorreoNoConfigurado;
        }

        var normalizado = correoIngresado.Trim().ToUpperInvariant();
        var usuario = await db.Usuarios.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.CorreoNormalizado == normalizado && x.Activo, ct);
        if (usuario is null) return ResultadoRecuperacion.Ok;

        var ahora = DateTime.UtcNow;
        // Un enlace nuevo anula los pendientes: solo el último sirve.
        await db.RecuperacionesContrasena
            .Where(x => x.IdUsuario == usuario.IdUsuario && x.UsadoEn == null && x.ExpiraEn > ahora)
            .ExecuteUpdateAsync(x => x.SetProperty(r => r.UsadoEn, ahora), ct);

        var token = TokensSeguros.Generar();
        db.RecuperacionesContrasena.Add(new RecuperacionContrasena
        {
            IdUsuario = usuario.IdUsuario,
            TokenHash = TokensSeguros.Hash(token),
            CreadoEn = ahora,
            ExpiraEn = ahora.AddMinutes(_opciones.MinutosRecuperacion),
        });
        Auditar(usuario.IdUsuario, "SolicitarRecuperacionContrasena", "Exitoso");
        await db.SaveChangesAsync(ct);

        var enlace = $"{_opciones.UrlFrontend.TrimEnd('/')}/restablecer-contrasena#token={Uri.EscapeDataString(token)}";
        try
        {
            await correo.EnviarAsync(
                usuario.Correo,
                "Restablece tu contraseña de ConecsIA",
                $"Recibimos una solicitud para restablecer tu contraseña.\n\nAbre este enlace (vence en {_opciones.MinutosRecuperacion} minutos y sirve una sola vez):\n{enlace}\n\nSi no fuiste tú, ignora este mensaje: tu contraseña no cambiará.",
                $"<p>Recibimos una solicitud para restablecer tu contraseña.</p><p><a href=\"{WebUtility.HtmlEncode(enlace)}\">Restablecer contraseña</a></p><p>El enlace vence en {_opciones.MinutosRecuperacion} minutos y sirve una sola vez. Si no fuiste tú, ignora este mensaje: tu contraseña no cambiará.</p>",
                ct);
        }
        catch (Exception ex)
        {
            // No se revela al cliente (no debe saber si la cuenta existe); queda en el registro del servidor.
            logger.LogError(ex, "No se pudo enviar el correo de recuperación al usuario {IdUsuario}.", usuario.IdUsuario);
        }
        return ResultadoRecuperacion.Ok;
    }

    public async Task<ResultadoRecuperacion> RestablecerAsync(string token, string nuevaContrasena, CancellationToken ct)
    {
        if (!PoliticaContrasena.Cumple(nuevaContrasena)) return ResultadoRecuperacion.ContrasenaInvalida;

        var ahora = DateTime.UtcNow;
        var hash = TokensSeguros.Hash(token.Trim());
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        // Consumo condicional: dos envíos simultáneos del mismo enlace no pueden usarlo dos veces.
        var consumidos = await db.RecuperacionesContrasena
            .Where(x => x.TokenHash == hash && x.UsadoEn == null && x.ExpiraEn > ahora)
            .ExecuteUpdateAsync(x => x.SetProperty(r => r.UsadoEn, ahora), ct);
        if (consumidos == 0) return ResultadoRecuperacion.TokenInvalido;

        var idUsuario = await db.RecuperacionesContrasena.Where(x => x.TokenHash == hash).Select(x => x.IdUsuario).SingleAsync(ct);
        var usuario = await db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.IdUsuario == idUsuario && x.Activo, ct);
        if (usuario is null) return ResultadoRecuperacion.TokenInvalido;

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, nuevaContrasena);
        usuario.SelloSeguridad = Guid.NewGuid().ToString("N");
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await db.SesionesRefresco
            .Where(x => x.IdUsuario == usuario.IdUsuario && x.RevocadoEn == null)
            .ExecuteUpdateAsync(x => x.SetProperty(s => s.RevocadoEn, ahora), ct);
        Auditar(usuario.IdUsuario, "RestablecerContrasena", "Exitoso");
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return ResultadoRecuperacion.Ok;
    }

    private void Auditar(int idUsuario, string accion, string resultado)
    {
        var http = httpContextAccessor.HttpContext;
        db.Auditorias.Add(new Auditoria
        {
            TipoActor = "Usuario",
            IdUsuario = idUsuario,
            Accion = accion,
            Entidad = "Usuario",
            IdRegistro = idUsuario.ToString(),
            Resultado = resultado,
            ValorNuevo = JsonSerializer.Serialize(new { canal = "correo" }),
            DireccionIp = http?.Connection.RemoteIpAddress?.ToString(),
            IdCorrelacion = http?.TraceIdentifier,
            OcurridoEn = DateTime.UtcNow,
        });
    }
}
