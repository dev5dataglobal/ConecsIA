using System.Security.Cryptography;
using System.Text;
using ConecsIA.SharedKernel.Errores;
using ConecsIA.SharedKernel.Tenancy;

namespace ConecsIA.Modules.Identidad.Domain;

public enum EstadoInvitacion
{
    Pendiente,
    Aceptada,
    Revocada,
}

/// <summary>
/// Invitación administrativa (HU-016, ADR-006): token de un solo uso con expiración. Solo se guarda
/// el hash SHA-256; el token en claro se entrega una vez a quien invita (o al canal de correo, D-12).
/// </summary>
public sealed class Invitacion : IEntidadDeOrganizacion
{
    public const string CodigoNoDisponible = "INVITACION_NO_DISPONIBLE";

    private Invitacion()
    {
        HashToken = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid MembresiaId { get; private set; }
    [UnicidadGlobal("Hash de 32 bytes aleatorios: la aceptación anónima lo busca sin conocer la organización.")]
    public string HashToken { get; private set; }
    public EstadoInvitacion Estado { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; }
    public Guid? CreadaPor { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? AceptadaEn { get; private set; }

    /// <summary>Token de concurrencia (xmin): dos aceptaciones simultáneas no pueden ganar ambas.</summary>
    public uint Version { get; private set; }

    public static (Invitacion Invitacion, string Token) Crear(
        Guid organizacionId, Guid usuarioId, Guid membresiaId, Guid? actor, DateTimeOffset ahora, TimeSpan vigencia)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var invitacion = new Invitacion
        {
            Id = Guid.CreateVersion7(ahora),
            OrganizacionId = organizacionId,
            UsuarioId = usuarioId,
            MembresiaId = membresiaId,
            HashToken = Hashear(token),
            Estado = EstadoInvitacion.Pendiente,
            CreadaEn = ahora,
            CreadaPor = actor,
            ExpiraEn = ahora + vigencia,
        };
        return (invitacion, token);
    }

    public static string Hashear(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Vencida, aceptada o revocada: 410 sin distinguir el motivo.</summary>
    public void Aceptar(DateTimeOffset ahora)
    {
        if (Estado != EstadoInvitacion.Pendiente || ahora >= ExpiraEn)
        {
            throw new RecursoNoDisponibleException(CodigoNoDisponible, "La invitación no existe, venció o ya fue usada.");
        }

        Estado = EstadoInvitacion.Aceptada;
        AceptadaEn = ahora;
    }

    public void Revocar()
    {
        if (Estado == EstadoInvitacion.Pendiente) Estado = EstadoInvitacion.Revocada;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
