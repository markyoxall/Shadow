using Orleans;
using System.Threading.Tasks;
using Shadow.FastEndpoints.Data;
using Shadow.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Shadow.FastEndpoints.Orleans
{
    public class NoteGrain : Grain, INoteGrain
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public NoteGrain(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Shadow.FastEndpoints.Data.Note?> GetAsync()
        {
            using var db = _dbFactory.CreateDbContext();
            var id = this.GetPrimaryKey();
            return await db.Notes.FindAsync(id);
        }

        public async Task SetAsync(Shadow.FastEndpoints.Data.Note note)
        {
            using var db = _dbFactory.CreateDbContext();
            note.Id = this.GetPrimaryKey();
            note.CreatedAt = DateTime.UtcNow;
            var existing = await db.Notes.FindAsync(note.Id);
            if (existing == null)
            {
                db.Notes.Add(note);
            }
            else
            {
                existing.Title = note.Title;
                existing.Content = note.Content;
                existing.CreatedAt = note.CreatedAt;
            }
            await db.SaveChangesAsync();
        }
    }
}
