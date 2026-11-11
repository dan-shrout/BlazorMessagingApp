using BlazorMessagingApp.Data;
using BlazorMessagingApp.Models;
using BlazorMessagingApp.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorMessagingApp.Hubs;

public record ConversationSummary(string User, int Unread, DateTime LastAt);
public record Conversation(string User, List<DirectMessage> Messages);

[Authorize(AuthenticationSchemes = HubTicketHandler.SchemeName)]
public class ChatHub(ChatDbContext db, PresenceTracker presence, UserManager<ApplicationUser> users) : Hub
{
    private const int HistoryLimit = 50;
    private const int MaxMessageLength = 2000;

    /// <summary>Joins a room as the authenticated user and returns its recent history (oldest first).</summary>
    public async Task<List<ChatMessage>> JoinRoom(string room)
    {
        room = room.Trim();
        if (room.Length is 0 or > 50)
            throw new HubException("Invalid room name.");

        var user = Context.User?.Identity?.Name ?? throw new HubException("Not authenticated.");

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

    // ---- Private messages ----

    /// <summary>Everyone the caller has exchanged private messages with, newest first.</summary>
    public async Task<List<ConversationSummary>> GetConversations()
    {
        var me = MyId;
        var rows = await db.DirectMessages
            .Where(m => m.FromUserId == me || m.ToUserId == me)
            .GroupBy(m => m.FromUserId == me ? m.ToUserName : m.FromUserName)
            .Select(g => new
            {
                User = g.Key,
                Unread = g.Count(m => m.ToUserId == me && m.ReadAt == null),
                LastAt = g.Max(m => m.SentAt),
            })
            .ToListAsync();

        return rows.OrderByDescending(r => r.LastAt)
            .Select(r => new ConversationSummary(r.User, r.Unread, r.LastAt))
            .ToList();
    }

    /// <summary>Opens a conversation: returns recent history (oldest first) and marks it read.</summary>
    public async Task<Conversation> OpenConversation(string withUser)
    {
        var other = await FindUser(withUser);
        var me = MyId;

        var history = await db.DirectMessages
            .Where(m => (m.FromUserId == me && m.ToUserId == other.Id) || (m.FromUserId == other.Id && m.ToUserId == me))
            .OrderByDescending(m => m.SentAt)
            .Take(HistoryLimit)
            .ToListAsync();
        history.Reverse();

        await MarkRead(other.Id);
        return new Conversation(other.UserName!, history);
    }

    public async Task SendDirectMessage(string toUser, string text)
    {
        var other = await FindUser(toUser);

        text = text.Trim();
        if (text.Length == 0) return;
        if (text.Length > MaxMessageLength) text = text[..MaxMessageLength];

        var message = new DirectMessage
        {
            FromUserId = MyId,
            FromUserName = MyName,
            ToUserId = other.Id,
            ToUserName = other.UserName!,
            Text = text,
        };
        db.DirectMessages.Add(message);
        await db.SaveChangesAsync();

        // Goes to every open connection of both users, so the sender's other tabs stay in sync.
        await Clients.Users([MyId, other.Id]).SendAsync("ReceiveDirectMessage", message);
    }

    public async Task MarkConversationRead(string withUser) => await MarkRead((await FindUser(withUser)).Id);

    private string MyId => Context.UserIdentifier ?? throw new HubException("Not authenticated.");
    private string MyName => Context.User?.Identity?.Name ?? throw new HubException("Not authenticated.");

    private async Task<ApplicationUser> FindUser(string userName)
    {
        var other = await users.FindByNameAsync(userName.Trim()) ?? throw new HubException("No such user.");
        if (other.Id == MyId) throw new HubException("You can't message yourself.");
        return other;
    }

    private Task MarkRead(string fromUserId)
    {
        var me = MyId;
        return db.DirectMessages
            .Where(m => m.ToUserId == me && m.FromUserId == fromUserId && m.ReadAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(m => m.ReadAt, DateTime.UtcNow));
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
