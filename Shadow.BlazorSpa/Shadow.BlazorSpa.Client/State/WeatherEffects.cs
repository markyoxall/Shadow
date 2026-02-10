using Fluxor;
using System.Net.Http.Json;

namespace Shadow.BlazorSpa.Client.State;

public class WeatherEffects
{
    private readonly HttpClient _http;
    public WeatherEffects(HttpClient http) => _http = http;

    [EffectMethod]
    public async Task HandleFetchDataAction(FetchDataAction action, IDispatcher dispatcher)
    {
        // This call automatically includes Duende's BFF cookies/headers
        var data = await _http.GetFromJsonAsync<WeatherForecast[]>("api/weather");
        dispatcher.Dispatch(new FetchDataSuccessAction(data));
    }
}