using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Shadow.FastEndpoints.Data;
using Shadow.FastEndpoints.Orleans;

namespace Shadow.FastEndpoints.Endpoints;

public class NotesEndpoint : EndpointWithoutRequest
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
            await SendErrorsAsync("Invalid id", 400, ct);
            return;
        }

        var grain = _grainFactory.GetGrain<INoteGrain>(id);

        if (HttpContext.Request.Method == "GET")
        {
            var note = await grain.GetAsync();
            if (note == null) return await SendNotFoundAsync(ct);
            await SendAsync(note, cancellation: ct);
            return;
        }

        // POST
        var noteReq = await HttpContext.Request.ReadFromJsonAsync<Note>(cancellationToken: ct);
        if (noteReq == null) return await SendErrorsAsync("Invalid body", 400, ct);
        await grain.SetAsync(noteReq);
        await SendAsync(noteReq, cancellation: ct);
    }
}
