using Microsoft.EntityFrameworkCore;
using FastEndpoints;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Shadow.FastEndpoints.Data;
using Shadow.FastEndpoints.Orleans;
using Shadow.FastEndpoints.Services;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

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

        // Distributed cache: prefer Redis (ConnectionStrings:Redis) with memory fallback
        var redisConn = configuration.GetConnectionString("Redis") ?? configuration["ConnectionStrings:Redis"];
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            builder.Services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConn;
            });
            // Register ConnectionMultiplexer for advanced Redis operations (locks)
            builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn));

            Console.WriteLine("Using Redis distributed cache.");
        }
        else
        {
            builder.Services.AddDistributedMemoryCache();
            Console.WriteLine("Redis not configured; using in-memory distributed cache as fallback.");
        }

        // Register cache service wrapper
        builder.Services.AddSingleton<ICacheService, CacheService>();

        // HttpClient with Polly resilience policies (example for external calls)
        builder.Services.AddHttpClient("ResilientClient")
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }))
            .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

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
            // FastEndpoints.Swagger integrates with FastEndpoints - Map OpenAPI UI endpoint
            app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html"));
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        // Map FastEndpoints
        app.UseFastEndpoints();

        app.Run();
    }
}
