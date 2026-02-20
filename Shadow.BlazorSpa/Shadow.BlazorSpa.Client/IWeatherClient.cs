
/// <summary>
/// Abstraction of the class that retrieves weather data on the client or the server
/// </summary>
public interface IWeatherClient
{
    Task<WeatherForecast[]> GetWeatherForecasts();
    string? LastCorrelationId { get; }
}
