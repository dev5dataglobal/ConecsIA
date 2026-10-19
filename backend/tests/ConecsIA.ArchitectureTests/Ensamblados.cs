using System.Reflection;
using ConecsIA.Api.Composicion;
using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.SharedKernel.Tiempo;

namespace ConecsIA.ArchitectureTests;

/// <summary>Ensamblados del sistema bajo prueba, obtenidos por tipos ancla.</summary>
internal static class Ensamblados
{
    public const string PrefijoModulos = "ConecsIA.Modules.";

    public static Assembly SharedKernel => typeof(IClock).Assembly;
    public static Assembly Infrastructure => typeof(ConecsIADbContext).Assembly;
    public static Assembly Api => typeof(CatalogoModulos).Assembly;

    /// <summary>Módulos registrados en la composición de la API.</summary>
    public static IReadOnlyList<Assembly> ModulosRegistrados =>
        CatalogoModulos.Todos.Select(m => m.GetType().Assembly).Distinct().ToList();

    /// <summary>
    /// Todos los ensamblados de módulo presentes en la salida de compilación, estén o no registrados.
    /// Detecta módulos que existen en la solución pero que nadie compuso.
    /// </summary>
    public static IReadOnlyList<Assembly> ModulosEnSalida
    {
        get
        {
            var directorio = Path.GetDirectoryName(typeof(Ensamblados).Assembly.Location)!;
            return Directory
                .GetFiles(directorio, PrefijoModulos + "*.dll")
                .Select(ruta => Assembly.Load(AssemblyName.GetAssemblyName(ruta)))
                .ToList();
        }
    }

    public static string NombreModulo(Assembly modulo) =>
        modulo.GetName().Name!;
}
