using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// Client-side implementation used by Blazor WebAssembly. Retrieves forecasts via the local API
/// and notifies the Azure Function. This class must be resolved on the client runtime.
/// </summary>
internal class WeatherClient : IWeatherClient
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherClient> _logger;
    private readonly IWebAssemblyHostEnvironment _env;

    public WeatherClient(HttpClient client, IConfiguration configuration, ILogger<WeatherClient> logger,
        IWebAssemblyHostEnvironment env)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    public string? LastCorrelationId { get; private set; }

    public async Task<WeatherForecast[]> GetWeatherForecasts()
    {
        var forecasts = await _client.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast")
                        ?? throw new JsonException("Failed to deserialize");

        // Notify the Azure Function in background but capture correlation id if available
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyAzureFunctionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task failed to notify Azure Function");
            }
        });

        return forecasts;
    }

    private async Task NotifyAzureFunctionAsync()
    {
            try
            {
                // Prefer a local development URL when running in the WASM Development environment
                var functionUrl = _configuration["AzureFunction:WalkthroughUrl"];
                if (_env != null && _env.IsDevelopment())
                {
                    var local = _configuration["AzureFunction:WalkthroughUrlLocal"];
                    if (!string.IsNullOrWhiteSpace(local)) functionUrl = local;
                }

                if (string.IsNullOrWhiteSpace(functionUrl))
                {
                    _logger.LogWarning("Azure Function URL not configured");
                    return;
                }
                _logger.LogInformation("Notifying Azure Function at {FunctionUrl}", functionUrl);

                using var functionClient = new HttpClient();
                var content = new StringContent($"Weather forecast fetched at {DateTime.UtcNow:O}", System.Text.Encoding.UTF8, "text/plain");
                var response = await functionClient.PostAsync(functionUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Azure Function notified successfully");
                try
                {
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("correlationId", out var cid))
                    {
                        LastCorrelationId = cid.GetString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read correlationId from function response");
                }
            }
            else
            {
                _logger.LogWarning("Azure Function returned status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Azure Function");
        }
    }
}
