using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
// Register exception handling middleware to log unhandled exceptions from functions
builder.Services.AddSingleton<IFunctionsWorkerMiddleware, Shadow.FunkyGibbon.ExceptionHandlingMiddleware>();

builder.Build().Run();
