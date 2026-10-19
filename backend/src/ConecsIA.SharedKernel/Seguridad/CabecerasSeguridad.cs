namespace ConecsIA.SharedKernel.Seguridad;

public static class CabecerasSeguridad
{
    /// <summary>
    /// Identidad del esquema de autenticación de desarrollo. Sustituye a HU-006 mientras D-03 está
    /// abierta; está prohibido fuera de Development/Testing.
    /// </summary>
    public const string UsuarioDesarrollo = "X-Usuario-Desarrollo";

    /// <summary>Solo <em>selecciona</em> entre membresías verificadas; nunca otorga acceso (ADR-005).</summary>
    public const string Organizacion = "X-Organizacion-Id";
}
