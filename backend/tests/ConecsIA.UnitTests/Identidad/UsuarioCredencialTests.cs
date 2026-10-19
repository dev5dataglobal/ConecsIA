using ConecsIA.Modules.Identidad.Domain;

namespace ConecsIA.UnitTests.Identidad;

/// <summary>HU-006 (ADR-006): bloqueo por intentos y revocación de sesiones mediante el sello de seguridad.</summary>
public class UsuarioCredencialTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Bloqueo = TimeSpan.FromMinutes(15);

    [Fact]
    public void Se_bloquea_al_alcanzar_el_umbral_y_no_antes()
    {
        var usuario = Usuario.Crear("a@ejemplo.test", "A");

        for (var i = 0; i < 4; i++) Assert.False(usuario.RegistrarIntentoFallido(Ahora, 5, Bloqueo));
        Assert.False(usuario.EstaBloqueado(Ahora));

        Assert.True(usuario.RegistrarIntentoFallido(Ahora, 5, Bloqueo));
        Assert.True(usuario.EstaBloqueado(Ahora.AddMinutes(14)));
        Assert.False(usuario.EstaBloqueado(Ahora.AddMinutes(15)));
    }

    [Fact]
    public void Tras_vencer_el_bloqueo_el_conteo_empieza_de_nuevo()
    {
        var usuario = Usuario.Crear("a@ejemplo.test", "A");
        for (var i = 0; i < 5; i++) usuario.RegistrarIntentoFallido(Ahora, 5, Bloqueo);

        Assert.False(usuario.RegistrarIntentoFallido(Ahora.AddMinutes(16), 5, Bloqueo));
        Assert.False(usuario.EstaBloqueado(Ahora.AddMinutes(16)));
        Assert.Equal(1, usuario.IntentosFallidos);
    }

    [Fact]
    public void El_ingreso_exitoso_reinicia_el_conteo()
    {
        var usuario = Usuario.Crear("a@ejemplo.test", "A");
        for (var i = 0; i < 4; i++) usuario.RegistrarIntentoFallido(Ahora, 5, Bloqueo);

        usuario.RegistrarIngresoExitoso();

        Assert.False(usuario.RegistrarIntentoFallido(Ahora, 5, Bloqueo));
    }

    [Fact]
    public void Inactivar_y_cambiar_contrasena_rotan_el_sello_de_seguridad()
    {
        var usuario = Usuario.Crear("a@ejemplo.test", "A");
        var inicial = usuario.SelloSeguridad;

        usuario.EstablecerContrasena("hash");
        var trasContrasena = usuario.SelloSeguridad;
        usuario.Inactivar();

        Assert.NotEqual(inicial, trasContrasena);
        Assert.NotEqual(trasContrasena, usuario.SelloSeguridad);
        Assert.False(string.IsNullOrEmpty(inicial));
    }
}
