using Microsoft.AspNetCore.Mvc;

namespace Shadow.BlazorSpa.Bff.Controllers;

[Route("bff/notes")]
[ApiController]
public class NotesProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<NotesProxyController> _logger;

    public NotesProxyController(IHttpClientFactory httpFactory, ILogger<NotesProxyController> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var client = _httpFactory.CreateClient("FastEndpoints");
        var resp = await client.GetAsync($"/notes/{id}");
        var content = await resp.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)resp.StatusCode
        };
    }

    [HttpPost("{id:guid}")]
    public async Task<IActionResult> Post(Guid id)
    {
        var client = _httpFactory.CreateClient("FastEndpoints");
        // Log incoming headers to inspect CSRF / antiforgery headers from the client
        try
        {
            var headers = Request.Headers.Select(h => $"{h.Key}:{string.Join(';', h.Value)}");
            _logger.LogInformation("Incoming POST /bff/notes/{Id} headers: {Headers}", id, string.Join(" | ", headers));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read request headers for /bff/notes/{Id}", id);
        }

        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        _logger.LogDebug("Incoming POST /bff/notes/{Id} body: {Body}", id, body);
        var resp = await client.PostAsync($"/notes/{id}", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        var content = await resp.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)resp.StatusCode
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = _httpFactory.CreateClient("FastEndpoints");
        var resp = await client.DeleteAsync($"/notes/{id}");
        return StatusCode((int)resp.StatusCode);
    }
}
