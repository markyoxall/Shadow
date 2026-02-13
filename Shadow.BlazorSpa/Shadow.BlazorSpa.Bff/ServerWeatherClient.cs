using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shadow.BlazorSpa;

/// <summary>
/// Server-side implementation that returns synthetic forecasts. It does not call the Azure Function.
/// </summary>
internal class ServerWeatherClient : IWeatherClient
{
    // Server implementation does not set a correlation id.
    public string? LastCorrelationId { get; } = null;

    public Task<WeatherForecast[]> GetWeatherForecasts()
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var results = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();

        return Task.FromResult(results);
    }

    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
}
