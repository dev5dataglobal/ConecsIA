namespace ConecsIA.Modules.Identidad.Domain;

/// <summary>
/// Sesión emitida a un usuario (ADR-006, D-03c): permite varias simultáneas y revocar una concreta.
/// La cookie lleva su id; una sesión revocada deja de servir en la siguiente petición aunque alguien
/// conserve una copia de la cookie.
/// </summary>
public sealed class SesionUsuario
{
    private SesionUsuario()
    {
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset IniciadaEn { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? RevocadaEn { get; private set; }

    public static SesionUsuario Iniciar(Guid usuarioId, DateTimeOffset ahora, TimeSpan duracionMaxima) => new()
    {
        Id = Guid.NewGuid(),
        UsuarioId = usuarioId,
        IniciadaEn = ahora,
        ExpiraEn = ahora + duracionMaxima,
    };

    public bool EstaVigente(DateTimeOffset ahora) => RevocadaEn is null && ExpiraEn > ahora;

    /// <returns><c>false</c> si ya estaba revocada (idempotente).</returns>
    public bool Revocar(DateTimeOffset ahora)
    {
        if (RevocadaEn is not null) return false;
        RevocadaEn = ahora;
        return true;
    }
}
