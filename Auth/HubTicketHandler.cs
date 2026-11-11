using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BlazorMessagingApp.Auth;

public class HubTicketHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    HubTicketStore tickets) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "HubTicket";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // SignalR sends the token as a Bearer header on HTTP requests (negotiate) and as a
        // query string parameter on WebSocket / SSE requests.
        string? ticket = Request.Query["access_token"];
        if (string.IsNullOrEmpty(ticket) && Request.Headers.Authorization.ToString() is { } header
            && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            ticket = header["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(ticket))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (tickets.Redeem(ticket) is not var (userId, userName))
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired ticket."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, userName)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
