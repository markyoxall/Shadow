using Shadow.BlazorSpa.Client.Shared;

public interface INotesClient
{
    Task<Note?> GetAsync(Guid id);
    Task<Note?> SaveAsync(Guid id, Note note);
    Task DeleteAsync(Guid id);
}
