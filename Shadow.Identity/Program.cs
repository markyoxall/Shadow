using Serilog;
using Shadow.Identity;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(ctx.Configuration));

    var app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    // ensure database is created/migrated and seed the initial data
    // this is performed at startup so the app can run against a ready DB.
    try
    {
        Log.Information("Ensuring database is created and seed data is applied...");
        SeedData.EnsureSeedData(app);
        Log.Information("Database migration and seeding completed.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred while migrating or seeding the database");
        throw;
    }

    // if the caller explicitly passed "/seed" we exit after seeding (keeps previous behavior)
    if (args.Contains("/seed"))
    {
        Log.Information("Seeding requested via /seed. Exiting.");
        return;
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}