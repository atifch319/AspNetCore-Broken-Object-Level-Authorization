namespace BolaDemo.Api.Controllers;

internal static class ApiProblems
{
    public static ActionResult InvalidOrderId(ControllerBase controller) =>
        controller.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid order id.",
            detail: "Order id must be a positive integer.");

    public static ActionResult OrderNotFound(ControllerBase controller) =>
        controller.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Order not found.",
            detail: "No order is available for this request.");

    public static ActionResult InvalidPaging(ControllerBase controller) =>
        controller.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid paging.",
            detail: "Page must be at least 1 and pageSize must be between 1 and 100.");
}
