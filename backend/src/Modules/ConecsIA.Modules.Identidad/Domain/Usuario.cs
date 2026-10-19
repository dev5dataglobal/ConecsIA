namespace ConecsIA.Modules.Identidad.Domain;

/// <summary>
/// Usuario autenticable (distinto de Persona). Credencial y bloqueo según ADR-006 (D-03, aprobada
/// 2026-10-19). El sello de seguridad cambia cuando debe invalidarse toda sesión vigente.
/// </summary>
public sealed class Usuario
{
    private Usuario()
    {
        Correo = string.Empty;
        Nombre = string.Empty;
        SelloSeguridad = string.Empty;
    }

    public Guid Id { get; private set; }
    public string Correo { get; private set; }
    public string Nombre { get; private set; }
    public bool Activo { get; private set; }

    /// <summary>Hash de la contraseña (nunca en claro). Nulo mientras el usuario no la haya definido.</summary>
    public string? HashContrasena { get; private set; }
    public string SelloSeguridad { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public static Usuario Crear(string correo, string nombre) => new()
    {
        Id = Guid.NewGuid(),
        Correo = NormalizarCorreo(correo),
        Nombre = nombre.Trim(),
        Activo = true,
        SelloSeguridad = NuevoSello(),
    };

    /// <summary>Solo para semillas sintéticas con identificadores estables.</summary>
    public static Usuario CrearConId(Guid id, string correo, string nombre)
    {
        var usuario = Crear(correo, nombre);
        usuario.Id = id;
        return usuario;
    }

    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    /// <summary>Retirar el acceso invalida las sesiones vigentes (PRD §5.2.8).</summary>
    public void Inactivar()
    {
        Activo = false;
        RevocarSesiones();
    }

    /// <summary>Define o reemplaza la contraseña; invalida las sesiones previas y limpia el bloqueo.</summary>
    public void EstablecerContrasena(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        HashContrasena = hash;
        IntentosFallidos = 0;
        BloqueadoHasta = null;
        RevocarSesiones();
    }

    /// <summary>Regenera el hash de la misma contraseña con parámetros actuales; no revoca sesiones.</summary>
    public void EstablecerHashSinRevocar(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        HashContrasena = hash;
    }

    public void RevocarSesiones() => SelloSeguridad = NuevoSello();

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    /// <summary>Cuenta un intento fallido; al alcanzar el umbral bloquea durante <paramref name="duracion"/>.</summary>
    /// <returns><c>true</c> si este intento provocó el bloqueo.</returns>
    public bool RegistrarIntentoFallido(DateTimeOffset ahora, int umbral, TimeSpan duracion)
    {
        if (BloqueadoHasta is { } hasta && hasta <= ahora)
        {
            // El bloqueo anterior venció: se empieza a contar de nuevo.
            BloqueadoHasta = null;
            IntentosFallidos = 0;
        }

        IntentosFallidos++;
        if (IntentosFallidos < umbral) return false;

        BloqueadoHasta = ahora + duracion;
        IntentosFallidos = 0;
        return true;
    }

    public void RegistrarIngresoExitoso()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }

    private static string NuevoSello() => Guid.NewGuid().ToString("N");
}
