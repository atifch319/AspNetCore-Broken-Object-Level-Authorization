using BolaDemo.Api.Authorization;
using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;

namespace BolaDemo.Api.Controllers;

/// <summary>
/// Admin-only route. The role attribute is a coarse gate and runs before the order is loaded.
/// It is not a substitute for <see cref="ReadOrderAuthorizationHandler"/>, which is the reusable
/// owner-or-admin rule used by the policy endpoint. This route has no customer caller, so the
/// role gate is the whole authorization decision: an admin may read any order.
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = AppRoles.Admin)]
[Produces("application/json")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminOrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (id < 1)
        {
            return ApiProblems.InvalidOrderId(this);
        }

        var order = await _db.Orders
            .AsNoTracking()
            .Where(candidate => candidate.Id == id)
            .SelectDto()
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return ApiProblems.OrderNotFound(this);
        }

        return Ok(order);
    }
}
