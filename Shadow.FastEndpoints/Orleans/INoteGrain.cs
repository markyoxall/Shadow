using Orleans;
using System.Threading.Tasks;
using Shadow.FastEndpoints.Data;

namespace Shadow.FastEndpoints.Orleans
{
    public interface INoteGrain : IGrainWithGuidKey
    {
        Task<Note?> GetAsync();
        Task SetAsync(Note note);
    }
}
