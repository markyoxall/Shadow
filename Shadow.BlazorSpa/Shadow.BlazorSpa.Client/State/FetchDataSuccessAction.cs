namespace Shadow.BlazorSpa.Client.State;

public record FetchDataSuccessAction(IEnumerable<WeatherForecast> Data);
