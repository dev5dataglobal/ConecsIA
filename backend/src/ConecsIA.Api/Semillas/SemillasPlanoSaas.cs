using ConecsIA.Infrastructure.Persistencia;
using ConecsIA.Modules.Identidad.Domain;
using ConecsIA.SharedKernel.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace ConecsIA.Api.Semillas;

/// <summary>
/// HU-009: superadministrador SaaS sintético para probar la consola de organizaciones. No tiene
/// membresías: no ve datos de clientes. Su contraseña la define SemillasContrasenas (si está configurada).
/// </summary>
public sealed class SemillasPlanoSaas(ConecsIADbContext contexto) : IConjuntoSemillas
{
    public static readonly Guid Superadministrador = Guid.Parse("0199f2a0-0000-7000-8000-000000000901");

    public async Task SembrarAsync(CancellationToken cancelacion)
    {
        if (await contexto.Set<Usuario>().AnyAsync(u => u.Id == Superadministrador, cancelacion)) return;

        var usuario = Usuario.CrearConId(Superadministrador, "super.saas@ejemplo.test", "Sofía Superadmin (plataforma)");
        usuario.AsignarPermisosSaas(PermisosSaas.Todos);
        contexto.Add(usuario);
        await contexto.SaveChangesAsync(cancelacion);
    }
}
