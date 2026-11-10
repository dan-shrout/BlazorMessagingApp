using System.Collections.Concurrent;

namespace BlazorMessagingApp.Hubs;

/// <summary>Tracks which users are in which rooms, keyed by SignalR connection id.</summary>
public class PresenceTracker
{
    private readonly ConcurrentDictionary<string, (string User, string Room)> _connections = new();

    public void Join(string connectionId, string user, string room) =>
        _connections[connectionId] = (user, room);

    /// <summary>Removes the connection and returns its previous room, if any.</summary>
    public string? Leave(string connectionId) =>
        _connections.TryRemove(connectionId, out var entry) ? entry.Room : null;

    public (string User, string Room)? Get(string connectionId) =>
        _connections.TryGetValue(connectionId, out var entry) ? entry : null;

    public string[] UsersIn(string room) =>
        _connections.Values
            .Where(c => c.Room == room)
            .Select(c => c.User)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToArray();
}
