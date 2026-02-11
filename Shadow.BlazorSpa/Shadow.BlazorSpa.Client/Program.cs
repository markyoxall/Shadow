using Duende.Bff.Blazor.Client;
using Fluxor;
using Fluxor.Blazor.Web.ReduxDevTools;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services
    .AddBffBlazorClient() // Provides auth state provider that polls the /bff/user endpoint
    .AddCascadingAuthenticationState();

builder.Services.AddFluxor(options =>
{
    // Scans the client assembly for Features, Reducers, and Effects
    options.ScanAssemblies(typeof(Program).Assembly);

    // Redux DevTools - DISABLED because it breaks initialization when extension is not available
    // VS launches Chrome without extensions, causing store initialization to fail
    // To use Redux DevTools: run app, copy URL, open in regular Chrome with extensions
    // options.UseReduxDevTools();
});

builder.Services.AddSingleton<IWeatherClient>(sp => sp.GetRequiredService<WeatherClient>());

builder.Services.AddLocalApiHttpClient<WeatherClient>();

// Register Notes client that talks to the BFF proxy
// Notes client registered - uses the BFF proxy via Local API
builder.Services.AddScoped<INotesClient, NotesClient>();
builder.Services.AddLocalApiHttpClient<NotesClient>();

var host = builder.Build();

// Initialize Fluxor store before running
var store = host.Services.GetRequiredService<IStore>();
await store.InitializeAsync();

await host.RunAsync();
