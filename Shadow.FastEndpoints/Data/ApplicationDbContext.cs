using Microsoft.EntityFrameworkCore;

namespace Shadow.FastEndpoints.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Note> Notes { get; set; } = null!;
    }
}
