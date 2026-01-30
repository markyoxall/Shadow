using Duende.Bff.Blazor;
using Shadow.BlazorSpa;
using Shadow.BlazorSpa.Bff.Components;

var builder = WebApplication.CreateBuilder(args);
// Ensure referenced Blazor WebAssembly static assets are available to the host
builder.WebHost.UseStaticWebAssets();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// BFF setup for blazor
builder.Services.AddBff()
    .AddServerSideSessions() // Add in-memory implementation of server side sessions
    .AddBlazorServer();

// Register an abstraction for retrieving weather forecasts that can run on the server. 
// On the client, in WASM, this will be retrieved via an HTTP call to the server.
builder.Services.AddSingleton<IWeatherClient, ServerWeatherClient>();

// Configure the authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
        options.DefaultSignOutScheme = "oidc";
    })
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "__Host-blazor";

        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://localhost:5001";
        options.ClientId = "interactive";
        // Must match the client secret configured in the Identity server (stored hashed there)
        options.ClientSecret = "49C1A7E1-0C79-4A89-A3D6-A37998FB86B0";
        options.ResponseType = "code";
        options.ResponseMode = "query";
        // Ensure the middleware uses the standard callback paths
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        // request the API scope that IdentityServer exposes for this sample
        options.Scope.Add("scope2");
        options.Scope.Add("offline_access");

        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";

        // Ensure the redirect_uri and post_logout_redirect_uri sent to the
        // identity provider exactly match the URIs registered for the client.
        // This avoids mismatches caused by the launcher/IDE composing URLs.
        options.Events ??= new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents();
        options.Events.OnRedirectToIdentityProvider = ctx =>
        {
            // set the absolute redirect_uri the IDP expects
            ctx.ProtocolMessage.RedirectUri = "https://localhost:7035/signin-oidc";
            return System.Threading.Tasks.Task.CompletedTask;
        };
        options.Events.OnRedirectToIdentityProviderForSignOut = ctx =>
        {
            // set the post logout redirect URI used by the IDP
            ctx.ProtocolMessage.PostLogoutRedirectUri = "https://localhost:7035/signout-callback-oidc";
            return System.Threading.Tasks.Task.CompletedTask;
        };
    });

// Make sure authentication state is available to all components. 
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();

// Add the BFF middleware which performs anti forgery protection
app.UseBff();
app.UseAuthorization();
app.UseAntiforgery();

// Add the BFF management endpoints, such as login, logout, etc.
app.MapBffManagementEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Shadow.BlazorSpa.Client._Imports).Assembly);

// Example of local api endpoints. 
app.MapWeatherEndpoints();

app.Run();
