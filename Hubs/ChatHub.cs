using BlazorMessagingApp.Data;
using BlazorMessagingApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorMessagingApp.Hubs;

public class ChatHub(ChatDbContext db, PresenceTracker presence) : Hub
{
    private const int HistoryLimit = 50;
    private const int MaxMessageLength = 2000;

    /// <summary>Joins a room and returns its recent history (oldest first).</summary>
    public async Task<List<ChatMessage>> JoinRoom(string room, string user)
    {
        room = room.Trim();
        user = user.Trim();
        if (room.Length is 0 or > 50 || user.Length is 0 or > 30)
            throw new HubException("Invalid room or user name.");

        await LeaveCurrentRoom();

        presence.Join(Context.ConnectionId, user, room);
        await Groups.AddToGroupAsync(Context.ConnectionId, room);

        await Clients.Group(room).SendAsync("UserJoined", user);
        await Clients.Group(room).SendAsync("PresenceChanged", presence.UsersIn(room));

        var history = await db.Messages
            .Where(m => m.Room == room)
            .OrderByDescending(m => m.SentAt)
            .Take(HistoryLimit)
            .ToListAsync();
        history.Reverse();
        return history;
    }

    public async Task SendMessage(string text)
    {
        var (user, room) = presence.Get(Context.ConnectionId)
            ?? throw new HubException("Join a room first.");

        text = text.Trim();
        if (text.Length is 0) return;
        if (text.Length > MaxMessageLength) text = text[..MaxMessageLength];

        var message = new ChatMessage { Room = room, User = user, Text = text, SentAt = DateTime.UtcNow };
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        await Clients.Group(room).SendAsync("ReceiveMessage", message);
    }

    public async Task Typing()
    {
        if (presence.Get(Context.ConnectionId) is var (user, room))
            await Clients.OthersInGroup(room).SendAsync("UserTyping", user);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentRoom();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task LeaveCurrentRoom()
    {
        var info = presence.Get(Context.ConnectionId);
        if (info is null) return;

        presence.Leave(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, info.Value.Room);
        await Clients.Group(info.Value.Room).SendAsync("UserLeft", info.Value.User);
        await Clients.Group(info.Value.Room).SendAsync("PresenceChanged", presence.UsersIn(info.Value.Room));
    }
}
