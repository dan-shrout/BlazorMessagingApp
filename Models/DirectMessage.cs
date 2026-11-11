namespace BlazorMessagingApp.Models;

public class DirectMessage
{
    public int Id { get; set; }
    public string FromUserId { get; set; } = "";
    public string FromUserName { get; set; } = "";
    public string ToUserId { get; set; } = "";
    public string ToUserName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
