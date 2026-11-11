using BlazorMessagingApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlazorMessagingApp.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ChatMessage>().HasIndex(m => new { m.Room, m.SentAt });
        modelBuilder.Entity<DirectMessage>().HasIndex(m => new { m.ToUserId, m.FromUserId });
        modelBuilder.Entity<DirectMessage>().HasIndex(m => new { m.FromUserId, m.ToUserId });
    }
}
