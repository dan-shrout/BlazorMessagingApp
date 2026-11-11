# BlazorMessagingApp

A real-time chat app built with Blazor (interactive server rendering), SignalR, ASP.NET Core Identity and SQLite.

## Features

- **Chat rooms**: join any room by name, see recent history, who's online, and who's typing.
- **Private messages**: click a user in the online list, or type a username, to start a conversation. Conversations are saved, show unread counts, and reach all of a user's open tabs.
- **Accounts**: register with a username, email and password. Login requires a confirmed email.
- **Email confirmation and password reset**: via emailed links. Links are written to the server log unless SMTP is configured.
- **Persistence**: users, room messages and private messages are stored in a local SQLite database (`chat.db`).
- **Reconnects**: the client reconnects automatically and rejoins its room.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run it

```bash
dotnet run --launch-profile http
```

Then open <http://localhost:5181>. (The `https` profile serves on <https://localhost:7122>.)

The database file `chat.db` is created in the project folder on first run. To start fresh, stop the app and delete `chat.db*`.

### First-time walkthrough

1. Go to `/register` and create an account.
2. Without SMTP configured, the confirmation email is written to the terminal running the app. Look for a line like:
   ```
   warn: ...EmailSender[0]
         SMTP not configured. Email to you@example.com (Confirm your email): http://localhost:5181/confirm-email?userId=...
   ```
   Open that link to confirm the account.
3. Log in, pick a room and start chatting.
4. To try private messages, open a second browser (or a private window), register and confirm a second user, join the same room, then click the other user's name.

Password reset works the same way: `/forgot-password`, then open the link from the log.

## Configuration

Settings live in [appsettings.json](appsettings.json) and can be overridden with environment variables (use `__` for nesting).

| Setting | Purpose | Default |
| --- | --- | --- |
| `ConnectionStrings:Chat` | SQLite connection string | `Data Source=chat.db` |
| `Smtp:Host` | SMTP server. If empty, emails are logged instead of sent | empty |
| `Smtp:Port` | SMTP port | `587` |
| `Smtp:EnableSsl` | Use TLS | `true` |
| `Smtp:User`, `Smtp:Password` | SMTP credentials (optional) | empty |
| `Smtp:From` | Sender address (required when `Smtp:Host` is set) | empty |

Example:

```bash
Smtp__Host=smtp.example.com Smtp__From=chat@example.com Smtp__User=me Smtp__Password=secret dotnet run --launch-profile http
```

Keep SMTP passwords out of source control; use environment variables or [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets).

## Project layout

| Path | What it does |
| --- | --- |
| `Program.cs` | Service registration, authentication, middleware, hub mapping |
| `Hubs/ChatHub.cs` | SignalR hub: rooms, messages, typing, private messages |
| `Hubs/PresenceTracker.cs` | In-memory tracking of who is in which room |
| `Auth/HubTicketStore.cs`, `Auth/HubTicketHandler.cs` | Short-lived tickets that authenticate the SignalR connection (see below) |
| `Auth/EmailSender.cs`, `Auth/AccountEmailService.cs` | Sends or logs confirmation and reset emails |
| `Data/ChatDbContext.cs`, `Data/SchemaUpgrades.cs` | EF Core context and database setup |
| `Models/` | `ApplicationUser`, `ChatMessage`, `DirectMessage` |
| `Components/Pages/Chat.razor` | The chat UI (home page) |
| `Components/Pages/` (`Login`, `Register`, `ConfirmEmail`, `ResendConfirmation`, `ForgotPassword`, `ResetPassword`) | Account pages |

## How hub authentication works

The chat page opens its SignalR connection from the server, so the browser's auth cookie isn't sent with it. Instead, the page asks `HubTicketStore` for a random ticket tied to the signed-in user, and the hub validates it through the `HubTicket` authentication scheme. Tickets are kept in memory, last 2 minutes, and a fresh one is issued on every (re)connect.

## Limitations

- **Single server only**: presence and hub tickets are held in memory. Scaling out would need a SignalR backplane (such as Redis) and shared storage for both.
- **Live private messages need a joined room**: the hub connection starts when you join a room. Unread counts are loaded at that point.
- **No schema migrations**: the database is created with `EnsureCreated`, and new tables are added in `Data/SchemaUpgrades.cs`. Move to EF Core migrations if the schema keeps changing.
- **No revalidation of open sessions**: a signed-in session stays signed in until it ends, even if the account is later changed or removed.
- **Basic SMTP**: uses `System.Net.Mail.SmtpClient`. Consider MailKit or an email API for production.
