using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace BolaDemo.Api.Authentication;

/// <summary>
/// Fail-closed scheme used outside Development and Testing.
/// The demo header is ignored so it cannot be mistaken for a production login.
/// </summary>
public sealed class RejectAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public RejectAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());
}
