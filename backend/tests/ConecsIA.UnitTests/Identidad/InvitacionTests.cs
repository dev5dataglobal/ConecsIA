using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Errores;

namespace ConecsIA.UnitTests.Identidad;

/// <summary>HU-016: token de invitación de un solo uso, con expiración y guardado como hash.</summary>
public class InvitacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private static (Invitacion, string) Nueva() =>
        Invitacion.Crear(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Ahora, TimeSpan.FromHours(72));

    [Fact]
    public void Se_guarda_solo_el_hash_del_token()
    {
        var (invitacion, token) = Nueva();

        Assert.NotEqual(token, invitacion.HashToken);
        Assert.Equal(Invitacion.Hashear(token), invitacion.HashToken);
        Assert.True(token.Length >= 43, "32 bytes aleatorios en Base64Url.");
    }

    [Fact]
    public void Dos_invitaciones_tienen_tokens_distintos()
    {
        Assert.NotEqual(Nueva().Item2, Nueva().Item2);
    }

    [Fact]
    public void Se_acepta_una_sola_vez()
    {
        var (invitacion, _) = Nueva();
        invitacion.Aceptar(Ahora.AddHours(1));

        Assert.Equal(EstadoInvitacion.Aceptada, invitacion.Estado);
        Assert.Throws<RecursoNoDisponibleException>(() => invitacion.Aceptar(Ahora.AddHours(2)));
    }

    [Fact]
    public void Vencida_no_se_puede_aceptar()
    {
        var (invitacion, _) = Nueva();

        Assert.Throws<RecursoNoDisponibleException>(() => invitacion.Aceptar(Ahora.AddHours(72)));
    }

    [Fact]
    public void Revocada_no_se_puede_aceptar()
    {
        var (invitacion, _) = Nueva();
        invitacion.Revocar();

        Assert.Throws<RecursoNoDisponibleException>(() => invitacion.Aceptar(Ahora));
    }
}
