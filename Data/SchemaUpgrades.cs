using Microsoft.EntityFrameworkCore;

namespace BlazorMessagingApp.Data;

/// <summary>
/// EnsureCreated never alters an existing database, so tables added after the first run are
/// created here. Replace with EF Core migrations if the schema keeps growing.
/// </summary>
public static class SchemaUpgrades
{
    public static void Apply(ChatDbContext db)
    {
        db.Database.EnsureCreated();

        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DirectMessages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DirectMessages" PRIMARY KEY AUTOINCREMENT,
                "FromUserId" TEXT NOT NULL,
                "FromUserName" TEXT NOT NULL,
                "ToUserId" TEXT NOT NULL,
                "ToUserName" TEXT NOT NULL,
                "Text" TEXT NOT NULL,
                "SentAt" TEXT NOT NULL,
                "ReadAt" TEXT NULL
            );
            """);
        db.Database.ExecuteSqlRaw("""CREATE INDEX IF NOT EXISTS "IX_DirectMessages_ToUserId_FromUserId" ON "DirectMessages" ("ToUserId", "FromUserId");""");
        db.Database.ExecuteSqlRaw("""CREATE INDEX IF NOT EXISTS "IX_DirectMessages_FromUserId_ToUserId" ON "DirectMessages" ("FromUserId", "ToUserId");""");
    }
}
