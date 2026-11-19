using BolaDemo.Api.Contracts;

namespace BolaDemo.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("ok"));
}
