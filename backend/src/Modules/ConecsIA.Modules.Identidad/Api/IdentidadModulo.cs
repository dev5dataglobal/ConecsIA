using ConecsIA.Modules.Identidad.Application;
using ConecsIA.Modules.Identidad.Infrastructure;
using ConecsIA.SharedKernel.Modulos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConecsIA.Modules.Identidad.Api;

/// <summary>Usuarios, membresías, contexto de organización e inicio de sesión (EP-02, EP-05; HU-006, ADR-006).</summary>
public sealed class IdentidadModulo : IModulo
{
    public string Nombre => "Identidad";

    public void RegistrarServicios(IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddScoped<IRepositorioIdentidad, RepositorioIdentidad>();
        servicios.AddScoped<ResolvedorContexto>();
        servicios.AddScoped<ConsultaUsuariosDesarrollo>();
        servicios.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        servicios.AddScoped<ServicioIngreso>();
        servicios.AddScoped<IRepositorioRoles, RepositorioRoles>();
        servicios.AddScoped<ServicioRoles>();
        servicios.AddScoped<IRepositorioUsuarios, RepositorioUsuarios>();
        servicios.AddScoped<ServicioUsuarios>();
        servicios.AddScoped<ServicioAceptacionInvitaciones>();
        var invitaciones = new OpcionesInvitaciones();
        configuracion.GetSection(OpcionesInvitaciones.Seccion).Bind(invitaciones);
        servicios.AddSingleton(invitaciones);
        servicios.AddScoped<ConecsIA.SharedKernel.Organizaciones.IProvisionOrganizacion, ProvisionOrganizacion>();
    }
}
