namespace ApiConecsia.Helpers;

/// <summary>Regla única de contraseñas (cambio desde el perfil y restablecimiento por correo).</summary>
public static class PoliticaContrasena
{
    public const string Mensaje = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número.";

    public static bool Cumple(string value) =>
        value.Length >= 8 && value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit);
}
