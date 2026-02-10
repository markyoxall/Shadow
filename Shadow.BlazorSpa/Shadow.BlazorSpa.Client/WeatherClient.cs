using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// this is an example of a class that would access the data via a web service. This is typically
/// what you'd do in webassembly. 
/// Note that it implements the same interface as the <see cref="ServerWeatherClient"/>
/// when it's rendering on the server. 
/// </summary>
internal class WeatherClient(HttpClient client, IConfiguration configuration, ILogger<WeatherClient> logger) : IWeatherClient
{
    public async Task<WeatherForecast[]> GetWeatherForecasts()
    {
        var forecasts = await client.GetFromJsonAsync<WeatherForecast[]>("WeatherForecast")
                        ?? throw new JsonException("Failed to deserialize");

        // Call Azure Function to log the weather fetch (don't wait for it to complete)
        _ = Task.Run(async () =>
        {
            try
            {
                await NotifyAzureFunctionAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background task failed to notify Azure Function");
            }
        });

        return forecasts;
    }

    private async Task NotifyAzureFunctionAsync()
    {
        try
        {
            var functionUrl = configuration["AzureFunction:WalkthroughUrl"];
            if (string.IsNullOrWhiteSpace(functionUrl))
            {
                logger.LogWarning("Azure Function URL not configured");
                return;
            }

            using var functionClient = new HttpClient();
            var content = new StringContent($"Weather forecast fetched at {DateTime.UtcNow:O}", System.Text.Encoding.UTF8, "text/plain");
            var response = await functionClient.PostAsync(functionUrl, content);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Azure Function notified successfully");
            }
            else
            {
                logger.LogWarning("Azure Function returned status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify Azure Function");
        }
    }
}
