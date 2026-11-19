using BolaDemo.Api.Authentication;
using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;

namespace BolaDemo.Api.Controllers;

/// <summary>
/// INTENTIONALLY VULNERABLE. Demo only. Do not deploy this controller.
/// </summary>
[ApiController]
[Route("api/insecure/orders")]
[Authorize]
[Produces("application/json")]
public sealed class InsecureOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<InsecureOrdersController> _logger;

    public InsecureOrdersController(AppDbContext db, ILogger<InsecureOrdersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns an order by route id after authentication and without an ownership check.
    /// [Authorize] proves the caller is signed in. It does not compare the route id with that caller.
    /// A route id is an object reference, not permission to read the object.
    /// Changing the id must not grant access to another user's order. This action shows the bug where it does.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken cancellationToken)
    {
        if (id < 1)
        {
            return ApiProblems.InvalidOrderId(this);
        }

        // The authenticated user id is available and deliberately unused.
        // That is the vulnerability: the object id is trusted on its own.
        var userId = User.TryGetUserId();
        _logger.LogWarning(
            "Intentionally vulnerable order read by user {UserId} for order {OrderId}. No ownership check is performed.",
            userId,
            id);

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
