using System.Text;
using BlazorMessagingApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace BlazorMessagingApp.Auth;

/// <summary>Builds the confirmation and reset links and sends them. Also decodes the tokens.</summary>
public class AccountEmailService(
    UserManager<ApplicationUser> userManager,
    IEmailSender<ApplicationUser> emailSender,
    NavigationManager navigation)
{
    public async Task SendConfirmationAsync(ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendConfirmationLinkAsync(user, user.Email!, BuildLink("confirm-email", user, token));
    }

    public async Task SendPasswordResetAsync(ApplicationUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailSender.SendPasswordResetLinkAsync(user, user.Email!, BuildLink("reset-password", user, token));
    }

    public static string? DecodeToken(string? code)
    {
        try { return code is null ? null : Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (FormatException) { return null; }
    }

    private string BuildLink(string path, ApplicationUser user, string token) =>
        navigation.GetUriWithQueryParameters(
            navigation.ToAbsoluteUri(path).AbsoluteUri,
            new Dictionary<string, object?>
            {
                ["userId"] = user.Id,
                ["code"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)),
            });
}
