using Microsoft.AspNetCore.Mvc;

namespace Shadow.BlazorSpa.Bff.Controllers;

[Route("bff/notes")]
[ApiController]
public class NotesProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;

    public NotesProxyController(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
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
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
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
