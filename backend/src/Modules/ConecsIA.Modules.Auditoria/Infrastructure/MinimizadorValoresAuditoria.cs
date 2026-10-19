using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ConecsIA.Modules.Auditoria.Infrastructure;

/// <summary>
/// HU-008 (ADR-014, ADR-018): serializa los valores anterior/nuevo y enmascara cualquier propiedad
/// con nombre de secreto (contraseñas, hashes, tokens, sellos, claves), aunque esté anidada. Es una
/// red de seguridad: los casos de uso igual deben pasar solo datos no sensibles.
/// </summary>
public static partial class MinimizadorValoresAuditoria
{
    public const string Mascara = "***";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string? Serializar(object? valores)
    {
        if (valores is null) return null;

        var nodo = JsonSerializer.SerializeToNode(valores, valores.GetType(), Json);
        Enmascarar(nodo);
        return nodo?.ToJsonString(Json);
    }

    private static void Enmascarar(JsonNode? nodo)
    {
        switch (nodo)
        {
            case JsonObject objeto:
                foreach (var (nombre, valor) in objeto.ToList())
                {
                    if (NombreSensible().IsMatch(nombre) && valor is not (JsonObject or JsonArray))
                        objeto[nombre] = Mascara;
                    else
                        Enmascarar(valor);
                }
                break;
            case JsonArray lista:
                foreach (var elemento in lista) Enmascarar(elemento);
                break;
        }
    }

    [GeneratedRegex("contrase[ñn]a|password|pwd|hash|token|secret|sello|api[_-]?key|cookie|authorization",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NombreSensible();
}
