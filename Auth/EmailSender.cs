using System.Net;
using System.Net.Mail;
using BlazorMessagingApp.Models;
using Microsoft.AspNetCore.Identity;

namespace BlazorMessagingApp.Auth;

/// <summary>
/// Sends account emails over SMTP when <c>Smtp:Host</c> is configured. Otherwise the message is
/// written to the application log, which is enough to click the link during development.
/// </summary>
public class EmailSender(IConfiguration config, ILogger<EmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.", confirmationLink);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your password",
            $"Reset your password by <a href='{resetLink}'>clicking here</a>. If you didn't ask for this, ignore this email.", resetLink);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your password", $"Your password reset code is: {resetCode}", resetCode);

    private async Task SendAsync(string to, string subject, string htmlBody, string logValue)
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP not configured. Email to {To} ({Subject}): {Link}", to, subject, logValue);
            return;
        }

        var from = config["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is required.");
        using var client = new SmtpClient(host, config.GetValue("Smtp:Port", 587)) { EnableSsl = config.GetValue("Smtp:EnableSsl", true) };
        if (config["Smtp:User"] is { Length: > 0 } smtpUser)
            client.Credentials = new NetworkCredential(smtpUser, config["Smtp:Password"]);

        using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message);
    }
}
