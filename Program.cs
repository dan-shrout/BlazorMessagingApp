using BlazorMessagingApp.Auth;
using BlazorMessagingApp.Components;
using BlazorMessagingApp.Data;
using BlazorMessagingApp.Hubs;
using BlazorMessagingApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSingleton<HubTicketStore>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddIdentityCookies(o => o.ApplicationCookie?.Configure(c => c.LoginPath = "/login"));
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, HubTicketHandler>(HubTicketHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ChatDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, EmailSender>();
builder.Services.AddScoped<AccountEmailService>();

builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Chat") ?? "Data Source=chat.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SchemaUpgrades.Apply(scope.ServiceProvider.GetRequiredService<ChatDbContext>());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Hub requests should get a plain 401, not a re-executed Razor "not found" page.
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/chathub"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<ChatHub>("/chathub");

app.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.Run();
