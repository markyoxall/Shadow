using Duende.Bff;

namespace Shadow.BlazorSpa;

public static class WeatherEndpointExtensions
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        app.MapGet("/WeatherForecast", async (IWeatherClient weatherClient) =>
            await weatherClient.GetWeatherForecasts())
            .RequireAuthorization()          // ensures only authenticated users can call
            .AsBffApiEndpoint();             // enforces BFF anti-forgery / cookie
    }
}
