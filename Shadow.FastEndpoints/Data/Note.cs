using System;
using Orleans;

namespace Shadow.FastEndpoints.Data
{
    [GenerateSerializer]
    public class Note
    {
        [Id(0)]
        public Guid Id { get; set; }

        [Id(1)]
        public string Title { get; set; } = string.Empty;

        [Id(2)]
        public string Content { get; set; } = string.Empty;

        [Id(3)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
