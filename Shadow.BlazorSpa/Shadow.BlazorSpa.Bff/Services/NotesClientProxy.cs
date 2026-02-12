using System.Net.Http.Json;
using Shadow.Shared.Models;
using Shadow.Shared.Services;

namespace Shadow.BlazorSpa.Bff.Services;

public class NotesClientProxy : INotesClient
{
    private readonly IHttpClientFactory _httpFactory;

    public NotesClientProxy(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<Note?> GetAsync(Guid id)
    {
        var client = _httpFactory.CreateClient("FastEndpoints");
        var resp = await client.GetAsync($"/notes/{id}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<Note?> SaveAsync(Guid id, Note note)
    {
        var client = _httpFactory.CreateClient("FastEndpoints");
        var resp = await client.PostAsJsonAsync($"/notes/{id}", note);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Note>();
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = _httpFactory.CreateClient("FastEndpoints");
        await client.DeleteAsync($"/notes/{id}");
    }
}
