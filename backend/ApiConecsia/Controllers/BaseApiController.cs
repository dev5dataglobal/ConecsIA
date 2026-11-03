using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiConecsia.Controllers;

[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
}
