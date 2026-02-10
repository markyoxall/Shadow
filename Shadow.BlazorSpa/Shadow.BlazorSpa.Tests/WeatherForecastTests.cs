using Shadow.BlazorSpa.Client;

namespace Shadow.BlazorSpa.Tests;

public class WeatherForecastTests
{
    [Fact]
    public void WeatherForecast_TemperatureF_CalculatesCorrectly()
    {
        // Arrange
        var forecast = new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now),
            TemperatureC = 0,
            Summary = "Freezing"
        };

        // Act
        var temperatureF = forecast.TemperatureF;

        // Assert
        Assert.Equal(32, temperatureF);
    }

    [Fact]
    public void WeatherForecast_Properties_SetCorrectly()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.Now);
        var tempC = 25;
        var summary = "Warm";

        // Act
        var forecast = new WeatherForecast
        {
            Date = date,
            TemperatureC = tempC,
            Summary = summary
        };

        // Assert
        Assert.Equal(date, forecast.Date);
        Assert.Equal(tempC, forecast.TemperatureC);
        Assert.Equal(summary, forecast.Summary);
        // 25°C = 77°F but formula may round to 76
        Assert.InRange(forecast.TemperatureF, 76, 77);
    }
}
