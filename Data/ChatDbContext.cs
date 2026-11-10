using BlazorMessagingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorMessagingApp.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMessage>().HasIndex(m => new { m.Room, m.SentAt });
    }
}
