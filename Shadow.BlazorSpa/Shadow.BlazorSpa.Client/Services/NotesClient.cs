using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Shadow.Shared.Models;
using Shadow.Shared.Services;

public class NotesClient : INotesClient
{
    private readonly HttpClient _http;
    private readonly NavigationManager _nav;

    public NotesClient(HttpClient http, NavigationManager nav)
    {
        _http = http;
        _nav = nav;
    }

    private Uri MakeBffUri(string relative)
    {
        // NavigationManager ensures correct base for the BFF host in WASM
        return _nav.ToAbsoluteUri(relative);
    }

    public async Task<Note?> GetAsync(Guid id)
    {
        var uri = MakeBffUri($"/bff/notes/{id}");
        var resp = await _http.GetAsync(uri);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Note>();
    }

    public async Task<Note?> SaveAsync(Guid id, Note note)
    {
        var uri = MakeBffUri($"/bff/notes/{id}");
        // Ensure we include the BFF anti-forgery token for mutating requests
        var csrf = await GetCsrfTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(note)
        };
        if (!string.IsNullOrEmpty(csrf)) req.Headers.Add("X-CSRF", csrf);
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Note>();
    }

    public async Task DeleteAsync(Guid id)
    {
        var uri = MakeBffUri($"/bff/notes/{id}");
        var csrf = await GetCsrfTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Delete, uri);
        if (!string.IsNullOrEmpty(csrf)) req.Headers.Add("X-CSRF", csrf);
        await _http.SendAsync(req);
    }

    private async Task<string?> GetCsrfTokenAsync()
    {
        try
        {
            var uri = MakeBffUri("/bff/csrf");
            var resp = await _http.GetAsync(uri);
            if (!resp.IsSuccessStatusCode) return null;

            // Try parse JSON like { "x-csrf": "..." } or similar
            try
            {
                var dict = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (dict != null && dict.Count > 0)
                {
                    // return first value
                    return dict.Values.FirstOrDefault();
                }
            }
            catch { }

            var str = await resp.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(str)) return str.Trim('\"');
        }
        catch { }

        return null;
    }
}
