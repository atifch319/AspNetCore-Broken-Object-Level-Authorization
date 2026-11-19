using BolaDemo.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace BolaDemo.Api.Authentication;

/// <summary>
/// Development and Testing identity for the three seeded demo users.
/// The header carries an email. It is accepted only when that email is on the demo allow-list
/// and a matching user exists. Arbitrary user ids are rejected.
/// This handler is not production authentication.
/// </summary>
public sealed class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppDbContext _db;

    public DemoAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(DemoAuthenticationDefaults.HeaderName, out var values))
        {
            return AuthenticateResult.NoResult();
        }

        if (values.Count != 1)
        {
            return AuthenticateResult.Fail("Demo identity is not allowed.");
        }

        var normalized = values.ToString().Trim().ToLowerInvariant();
        if (!DemoIdentities.IsAllowedEmail(normalized))
        {
            return AuthenticateResult.Fail("Demo identity is not allowed.");
        }

        var user = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Email == normalized, Context.RequestAborted);

        if (user is null)
        {
            return AuthenticateResult.Fail("Demo identity is not allowed.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
