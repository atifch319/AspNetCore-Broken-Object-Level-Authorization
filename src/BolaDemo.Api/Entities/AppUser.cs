namespace BolaDemo.Api.Entities;

public sealed class AppUser
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
