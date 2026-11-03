using System.Text.RegularExpressions;
using ConecsIA.Domain.Entities;
using ConecsIA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiConecsia.Services;

/// <summary>
/// Reglas del módulo de Personas (normalización y formatos). Identificación de una persona:
/// (organización, tipo de documento, número normalizado), garantizada por índice único.
/// Normalización vigente (Propuesta, D-09): recortar espacios externos y pasar a mayúsculas;
/// no se quitan ceros iniciales ni otros caracteres.
/// </summary>
public static class ReglasPersona
{
    /// <summary>Tipos de persona iniciales de cada organización (editables e inactivables después).</summary>
    public static readonly string[] TiposPersonaIniciales = ["Empleado", "Visitante", "Contratista", "Proveedor", "Residente", "Cliente"];

    public static readonly string[] TiposTelefono = ["Móvil", "Fijo", "Trabajo", "Emergencia", "Otro"];
    public static readonly string[] TiposVehiculo = ["Automóvil", "Camioneta", "Motocicleta", "Camión", "Bus", "Otro"];
    public static readonly string[] RelacionesVehiculo = ["Propietario", "Responsable", "Conductor"];

    /// <summary>Patrón por defecto si el tipo de documento no define uno.</summary>
    private const string PatronGenerico = "^[A-Z0-9-]{3,20}$";
    private static readonly TimeSpan Limite = TimeSpan.FromMilliseconds(200);

    public static string NormalizarDocumento(string numero) => numero.Trim().ToUpperInvariant();

    public static string? ValidarDocumento(TipoDocumento tipo, string normalizado)
    {
        try
        {
            return Regex.IsMatch(normalizado, tipo.PatronValidacion ?? PatronGenerico, RegexOptions.None, Limite)
                ? null
                : $"El número no tiene un formato válido para {tipo.Nombre}.";
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            return "El tipo de documento tiene un patrón de validación no válido.";
        }
    }

    public static bool PatronValido(string? patron)
    {
        if (string.IsNullOrWhiteSpace(patron)) return true;
        try { _ = new Regex(patron, RegexOptions.None, Limite); return true; }
        catch (ArgumentException) { return false; }
    }

    /// <summary>Teléfono: solo dígitos (se aceptan espacios, guiones, puntos y paréntesis como separadores).</summary>
    public static string NormalizarTelefono(string numero) => Regex.Replace(numero, @"[\s\-().]", "");

    public static string? ValidarTelefono(string codigoPais, string numero, string tipo)
    {
        if (!TiposTelefono.Contains(tipo)) return "El tipo de teléfono no es válido.";
        if (!Regex.IsMatch(numero, @"^[0-9]+$")) return "El teléfono solo admite números.";
        if (codigoPais == "+57")
        {
            // Plan de numeración colombiano: móviles 3XXXXXXXXX; fijos 60 + indicativo + número (10 dígitos).
            if (tipo == "Móvil" && !Regex.IsMatch(numero, @"^3[0-9]{9}$")) return "El móvil colombiano debe tener 10 dígitos y empezar por 3.";
            if (!Regex.IsMatch(numero, @"^(3[0-9]{9}|60[0-9]{8})$")) return "El teléfono colombiano debe tener 10 dígitos (móvil 3… o fijo 60…).";
            return null;
        }
        // E.164: máximo 15 dígitos incluido el indicativo.
        return numero.Length >= 4 && numero.Length + codigoPais.Length - 1 <= 15 ? null : "El teléfono no tiene una longitud válida.";
    }

    /// <summary>Placa: mayúsculas sin espacios ni guiones. Formato provisional hasta D-17.</summary>
    public static string NormalizarPlaca(string placa) => Regex.Replace(placa, @"[\s\-]", "").ToUpperInvariant();

    public static string? ValidarVehiculo(string placaNormalizada, string tipo, string relacion)
    {
        if (!TiposVehiculo.Contains(tipo)) return "El tipo de vehículo no es válido.";
        if (!RelacionesVehiculo.Contains(relacion)) return "La relación con el vehículo no es válida.";
        return Regex.IsMatch(placaNormalizada, "^(?=.*[A-Z])(?=.*[0-9])[A-Z0-9]{4,8}$")
            ? null
            : "La placa debe tener entre 4 y 8 letras y números (por ejemplo ABC123 o ABC12D).";
    }

    /// <summary>Agrega los tipos de persona iniciales que falten (la persiste quien llama).</summary>
    public static async Task SembrarTiposPersonaAsync(ConecsiaDbContext db, int idOrganizacion, CancellationToken ct)
    {
        var existentes = await db.TiposPersona.IgnoreQueryFilters().Where(x => x.IdOrganizacion == idOrganizacion).Select(x => x.Nombre).ToListAsync(ct);
        foreach (var nombre in TiposPersonaIniciales.Where(n => !existentes.Contains(n, StringComparer.OrdinalIgnoreCase)))
            db.TiposPersona.Add(new TipoPersona { IdOrganizacion = idOrganizacion, Nombre = nombre, Activo = true });
    }
}
