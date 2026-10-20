using ConecsIA.SharedKernel.Seguridad;

namespace ConecsIA.SharedKernel.Modulos;

public sealed record ModuloOpcional(string Clave, string Nombre);

/// <summary>
/// Catálogo cerrado de módulos opcionales (HU-011, PRD §5.3). El núcleo (estructura, personas,
/// autorizaciones, accesos) siempre está habilitado. Inactivar un módulo nunca borra sus datos.
/// </summary>
public static class ModulosOpcionales
{
    public const string Vehiculos = "vehiculos";
    public const string Equipos = "equipos";
    public const string Correo = "correo";

    public static IReadOnlyList<ModuloOpcional> Todos { get; } =
    [
        new(Vehiculos, "Vehículos"),
        new(Equipos, "Equipos externos"),
        new(Correo, "Notificaciones por correo"),
    ];

    public static bool Existe(string? clave) => Todos.Any(m => m.Clave == clave);
}

/// <summary>
/// Guarda de módulo evaluada en el servidor (HU-011). Se aplica a operaciones <b>nuevas</b>; la
/// consulta histórica no la exige (propuesta PRD §5.3). Se lee en cada petición: efecto inmediato.
/// </summary>
public interface IModulosHabilitados
{
    Task<IReadOnlyList<string>> HabilitadosAsync(CancellationToken cancelacion);

    async Task ExigirAsync(string modulo, CancellationToken cancelacion)
    {
        if (!(await HabilitadosAsync(cancelacion)).Contains(modulo))
        {
            throw new AccesoDenegadoException(CodigoModuloNoHabilitado, "El módulo no está habilitado para tu organización.");
        }
    }

    const string CodigoModuloNoHabilitado = "MODULO_NO_HABILITADO";
}
