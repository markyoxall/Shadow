using System.Net.Http.Json;
using Shadow.BlazorSpa.Client.Shared;

public class NotesClient : INotesClient
{
    private readonly HttpClient _http;

    public NotesClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Note?> GetAsync(Guid id)
    {
        var resp = await _http.GetAsync($"/bff/notes/{id}");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<Note?> SaveAsync(Guid id, Note note)
    {
        var resp = await _http.PostAsJsonAsync($"/bff/notes/{id}", note);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Note>();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _http.DeleteAsync($"/bff/notes/{id}");
    }
}
