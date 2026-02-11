using Microsoft.EntityFrameworkCore;
using FastEndpoints;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Shadow.FastEndpoints.Data;
using Shadow.FastEndpoints.Orleans;

namespace Shadow.FastEndpoints;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration
        var configuration = builder.Configuration;

        // Add services
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        // Database: Postgres connection (use ConnectionStrings:Postgres)
        var pgConn = configuration.GetConnectionString("Postgres") ?? configuration["ConnectionStrings:Postgres"];
        if (string.IsNullOrWhiteSpace(pgConn))
        {
            // Warn at startup - user must set connection string in local secrets or environment
            Console.WriteLine("WARNING: Postgres connection string not configured. Set ConnectionStrings:Postgres in your configuration.");
        }

        // Register DbContextFactory for safe use in background services / grains
        builder.Services.AddDbContextFactory<ApplicationDbContext>(opts =>
        {
            if (!string.IsNullOrWhiteSpace(pgConn)) opts.UseNpgsql(pgConn);
        });

        // FastEndpoints
        builder.Services.AddFastEndpoints();

        // Orleans silo host (local development)
        builder.Host.UseOrleans((ctx, siloBuilder) =>
        {
            siloBuilder
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "ShadowApp";
                });
            // Add any additional Orleans configuration here
        });

        var app = builder.Build();

        // Ensure Postgres database/schema exists for local development
        try
        {
            using var scope = app.Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();
            if (dbFactory != null && !string.IsNullOrWhiteSpace(pgConn))
            {
                using var db = dbFactory.CreateDbContext();
                db.Database.EnsureCreated();
                Console.WriteLine("Postgres database ensured/created.");
            }
            else
            {
                Console.WriteLine("Skipping DB create: no Postgres connection configured or factory missing.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to ensure Postgres DB: {ex.Message}");
        }

        // Configure middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseOpenApi();
            app.UseSwaggerUi3();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        // Map FastEndpoints
        app.UseFastEndpoints();

        app.Run();
    }
}
