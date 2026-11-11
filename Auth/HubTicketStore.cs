using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace BlazorMessagingApp.Auth;

/// <summary>
/// Short-lived in-memory tickets that let the server-side Blazor circuit authenticate its
/// SignalR connection as the signed-in user. The browser's auth cookie isn't available to that
/// connection, and tickets never leave the server process.
/// </summary>
public class HubTicketStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    private readonly ConcurrentDictionary<string, (string UserId, string UserName, DateTime Expires)> _tickets = new();

    public string Issue(string userId, string userName)
    {
        var now = DateTime.UtcNow;
        foreach (var (key, value) in _tickets)
            if (value.Expires < now) _tickets.TryRemove(key, out _);

        var ticket = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _tickets[ticket] = (userId, userName, now + Lifetime);
        return ticket;
    }

    // Not single-use: SignalR sends the token on both the negotiate and transport requests.
    public (string UserId, string UserName)? Redeem(string ticket) =>
        _tickets.TryGetValue(ticket, out var t) && t.Expires >= DateTime.UtcNow ? (t.UserId, t.UserName) : null;
}
