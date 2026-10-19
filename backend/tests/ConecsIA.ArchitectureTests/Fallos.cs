using NetArchTest.Rules;

namespace ConecsIA.ArchitectureTests;

internal static class Fallos
{
    public static string Describir(TestResult resultado) =>
        resultado.IsSuccessful
            ? string.Empty
            : "Tipos que violan la regla: " + string.Join(", ", resultado.FailingTypeNames ?? []);
}
