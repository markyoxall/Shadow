using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Shadow.FunkyGibbon;

var builder = FunctionsApplication.CreateBuilder(args);

// Determine environment
var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
              ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
              ?? "Development";

// Load configuration files in the standard order: appsettings.json, appsettings.{Environment}.json, appsettings.local.json (optional)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// If a Functions local.settings.json exists, load its Values into configuration so local runs (and VS publish previews)
// pick up the same keys. This file is ignored by git by default and is intended for local development only.
var localSettingsPath = Path.Combine(AppContext.BaseDirectory, "local.settings.json");
if (!File.Exists(localSettingsPath))
{
    // also check repository root
    var repoLocal = Path.Combine(Directory.GetCurrentDirectory(), "local.settings.json");
    if (File.Exists(repoLocal)) localSettingsPath = repoLocal;
}

if (File.Exists(localSettingsPath))
{
    try
    {
        using var fs = File.OpenRead(localSettingsPath);
        using var doc = JsonDocument.Parse(fs);
        if (doc.RootElement.TryGetProperty("Values", out var values))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in values.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
            builder.Configuration.AddInMemoryCollection(dict);
        }
    }
    catch
    {
        // ignore parse errors for local.settings.json to avoid impacting startup
    }
}

// Using configuration sources (appsettings, optional local overrides, environment variables)

builder.ConfigureFunctionsWebApplication();


builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IFunctionsWorkerMiddleware,
    Shadow.FunkyGibbon.ExceptionHandlingMiddleware>();

// Configure strongly-typed options for the walkthrough function from configuration (prefer section "Walkthrough")
builder.Services.Configure<WalkthroughOptions>(builder.Configuration.GetSection("Walkthrough"));

// Post-configure to support legacy environment variable names used by Azure Functions/local.settings.json
builder.Services.PostConfigure<WalkthroughOptions>(options =>
{
    // Support legacy top-level configuration keys (from appsettings or local.settings.json) as well as environment variables
    if (string.IsNullOrWhiteSpace(options.StorageConnection))
    {
        options.StorageConnection = builder.Configuration["AzureWebJobsStorage"]
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    }

    if (string.IsNullOrWhiteSpace(options.BlobContainer))
    {
        options.BlobContainer = builder.Configuration["BLOB_CONTAINER"]
                              ?? Environment.GetEnvironmentVariable("BLOB_CONTAINER")
                              ?? options.BlobContainer;
    }

    if (string.IsNullOrWhiteSpace(options.SendGridApiKey))
    {
        options.SendGridApiKey = builder.Configuration["SENDGRID_API_KEY"]
                             ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
    }

    if (string.IsNullOrWhiteSpace(options.EmailTo))
    {
        options.EmailTo = builder.Configuration["EMAIL_TO"]
                    ?? builder.Configuration["Walkthrough:EmailTo"]
                    ?? Environment.GetEnvironmentVariable("EMAIL_TO");
    }

    if (string.IsNullOrWhiteSpace(options.EmailFrom))
    {
        options.EmailFrom = builder.Configuration["EMAIL_FROM"]
                        ?? builder.Configuration["Walkthrough:EmailFrom"]
                        ?? Environment.GetEnvironmentVariable("EMAIL_FROM")
                        ?? options.EmailFrom;
    }
});

builder.Build().Run();
