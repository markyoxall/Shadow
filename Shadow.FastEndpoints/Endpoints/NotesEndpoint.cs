using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shadow.FastEndpoints.Data;
using Shadow.FastEndpoints.Orleans;

namespace Shadow.FastEndpoints.Endpoints;

public class NotesEndpoint : EndpointWithoutRequest<Note>
{
    private readonly IGrainFactory _grainFactory;

    public NotesEndpoint(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public override void Configure()
    {
        Verbs("GET", "POST");
        Routes("/notes/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var idStr = Route<string>("id");
        if (!Guid.TryParse(idStr, out var id))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Invalid id", ct);
            return;
        }

        var grain = _grainFactory.GetGrain<INoteGrain>(id);

        if (HttpContext.Request.Method == "GET")
        {
            var note = await grain.GetAsync();
            if (note == null)
            {
                HttpContext.Response.StatusCode = 404;
                return;
            }
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(note, cancellationToken: ct);
            return;
        }

        // POST
        var noteReq = await HttpContext.Request.ReadFromJsonAsync<Note>(cancellationToken: ct);
        if (noteReq == null)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Invalid body", ct);
            return;
        }
        await grain.SetAsync(noteReq);
        HttpContext.Response.ContentType = "application/json";
        await HttpContext.Response.WriteAsJsonAsync(noteReq, cancellationToken: ct);
    }
}
