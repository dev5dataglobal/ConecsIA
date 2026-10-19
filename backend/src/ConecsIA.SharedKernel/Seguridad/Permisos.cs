namespace ConecsIA.SharedKernel.Seguridad;

/// <summary>
/// Catálogo de claves de permiso (ADR-005). El código comprueba claves, nunca nombres de rol.
/// Los roles configurables que agrupan estas claves llegan con HU-017.
/// </summary>
public static class Permisos
{
    public const string SedesConsultar = "sedes.consultar";
    public const string SedesCrear = "sedes.crear";
    public const string SedesModificar = "sedes.modificar";
    public const string SedesInactivar = "sedes.inactivar";

    public static IReadOnlyList<string> Todos { get; } = [SedesConsultar, SedesCrear, SedesModificar, SedesInactivar];
}
