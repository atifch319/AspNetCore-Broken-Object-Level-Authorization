using BolaDemo.Api.Authentication;
using BolaDemo.Api.Authorization;
using BolaDemo.Api.Contracts;
using BolaDemo.Api.Data;

namespace BolaDemo.Api.Controllers;

[ApiController]
[Route("api/secure")]
[Authorize]
[Produces("application/json")]
public sealed class SecureOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationService _authorization;
    private readonly ILogger<SecureOrdersController> _logger;

    public SecureOrdersController(
        AppDbContext db,
        IAuthorizationService authorization,
        ILogger<SecureOrdersController> logger)
    {
        _db = db;
        _authorization = authorization;
        _logger = logger;
    }

    /// <summary>
    /// Returns one order only when the route id and the authenticated user id both match.
    /// A missing row and an order owned by someone else produce the same 404.
    /// That hides whether the other user's order exists. A 403 would confirm that it does.
    /// </summary>
    [HttpGet("orders/{id:int}")]
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

        var userId = User.TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Query strings such as customerUserId are not bound. They cannot select the caller.
        var order = await _db.Orders
            .AsNoTracking()
            .Where(candidate => candidate.Id == id && candidate.CustomerUserId == userId)
            .SelectDto()
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            _logger.LogInformation(
                "No readable order {OrderId} for user {UserId}. The response does not say whether the row exists.",
                id,
                userId);
            return ApiProblems.OrderNotFound(this);
        }

        return Ok(order);
    }

    /// <summary>
    /// Returns the authenticated user's orders. The owner id comes from the principal, never from the client.
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(PagedOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedOrdersResponse>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return ApiProblems.InvalidPaging(this);
        }

        var userId = User.TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var owned = _db.Orders
            .AsNoTracking()
            .Where(candidate => candidate.CustomerUserId == userId)
            .OrderBy(candidate => candidate.Id);

        var totalCount = await owned.CountAsync(cancellationToken);
        var items = await owned
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .SelectDto()
            .ToListAsync(cancellationToken);

        return Ok(new PagedOrdersResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Loads the order, then evaluates <see cref="PolicyNames.CanReadOrder"/> against that order.
    /// The policy is not applied as an attribute because an attribute runs before the resource exists.
    /// Denial is returned as 404 so the response matches the ownership-filtered endpoint and does not
    /// confirm that another user's order exists. Use 403 instead when the caller is already allowed to know.
    /// </summary>
    [HttpGet("orders/{id:int}/policy")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetByPolicy(int id, CancellationToken cancellationToken)
    {
        if (id < 1)
        {
            return ApiProblems.InvalidOrderId(this);
        }

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (order is null)
        {
            return ApiProblems.OrderNotFound(this);
        }

        var authorization = await _authorization.AuthorizeAsync(User, order, PolicyNames.CanReadOrder);
        if (!authorization.Succeeded)
        {
            _logger.LogInformation(
                "Resource policy denied order {OrderId} for user {UserId}.",
                order.Id,
                User.TryGetUserId());
            return ApiProblems.OrderNotFound(this);
        }

        _logger.LogInformation(
            "Resource policy allowed order {OrderId} for user {UserId}.",
            order.Id,
            User.TryGetUserId());

        return Ok(OrderDto.FromEntity(order));
    }
}
