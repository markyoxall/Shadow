using Orleans;
using System.Threading.Tasks;
using Shadow.FastEndpoints.Data;

namespace Shadow.FastEndpoints.Orleans
{
    public interface INoteGrain : IGrainWithGuidKey
    {
        Task<Shadow.FastEndpoints.Data.Note?> GetAsync();
        Task SetAsync(Shadow.FastEndpoints.Data.Note note);
    }
}
