namespace BolaDemo.Api.Authorization;

/// <summary>
/// Marks a resource-based check: the caller may read one specific order.
/// The requirement carries no user id. The handler receives the order.
/// </summary>
public sealed class ReadOrderRequirement : IAuthorizationRequirement;
