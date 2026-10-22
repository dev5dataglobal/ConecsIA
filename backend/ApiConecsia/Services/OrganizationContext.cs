using System.Security.Claims;
using ConecsIA.Application.Interfaces;

namespace ApiConecsia.Services;

public sealed class OrganizationContext(IHttpContextAccessor httpContextAccessor)
    : IOrganizationContext
{
    public int? OrganizationId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("organization_id");
            return int.TryParse(value, out var id) && id > 0 ? id : null;
        }
    }
}
