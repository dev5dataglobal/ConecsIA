using System.Reflection;
using System.Reflection.Emit;

namespace ConecsIA.ArchitectureTests;

/// <summary>
/// ADR-008 (propuesto) y HU-001: el código de negocio obtiene la hora de IClock para que las
/// reglas de vigencia sean comprobables. Se prohíbe leer DateTime.Now/UtcNow y DateTimeOffset.Now/UtcNow.
/// </summary>
public class TiempoTests
{
    private static readonly MethodInfo[] RelojesProhibidos =
    [
        typeof(DateTime).GetProperty(nameof(DateTime.Now))!.GetMethod!,
        typeof(DateTime).GetProperty(nameof(DateTime.UtcNow))!.GetMethod!,
        typeof(DateTime).GetProperty(nameof(DateTime.Today))!.GetMethod!,
        typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.Now))!.GetMethod!,
        typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.UtcNow))!.GetMethod!,
    ];

    [Fact]
    public void Los_modulos_no_leen_el_reloj_del_sistema_directamente()
    {
        var infractores = Ensamblados.ModulosEnSalida
            .SelectMany(a => a.GetTypes())
            .SelectMany(MetodosQueLeenReloj)
            .ToList();

        Assert.True(infractores.Count == 0, $"Usan el reloj del sistema: {string.Join(", ", infractores)}");
    }

    private static IEnumerable<string> MetodosQueLeenReloj(Type tipo)
    {
        const BindingFlags todos = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
                                   | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        foreach (var metodo in tipo.GetMethods(todos).Cast<MethodBase>().Concat(tipo.GetConstructors(todos)))
        {
            var il = metodo.GetMethodBody()?.GetILAsByteArray();
            if (il is null) continue;

            for (var i = 0; i < il.Length - 4; i++)
            {
                if (il[i] != OpCodes.Call.Value && il[i] != OpCodes.Callvirt.Value) continue;

                var token = BitConverter.ToInt32(il, i + 1);
                MethodBase? llamado;
                try
                {
                    llamado = tipo.Module.ResolveMethod(token);
                }
                catch (ArgumentException)
                {
                    continue; // el byte no era el inicio de una instrucción call
                }

                if (llamado is not null && RelojesProhibidos.Contains(llamado))
                {
                    yield return $"{tipo.FullName}.{metodo.Name}";
                    break;
                }
            }
        }
    }
}
