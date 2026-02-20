using System;
using System.Threading.Tasks;
using Shadow.Shared.Models;

namespace Shadow.Shared.Services
{
    public interface INotesClient
    {
        Task<Note?> GetAsync(Guid id);
        Task<Note?> SaveAsync(Guid id, Note note);
        Task DeleteAsync(Guid id);
    }
}
