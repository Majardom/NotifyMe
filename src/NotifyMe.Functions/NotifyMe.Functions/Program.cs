using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotifyMe.Functions.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton(sp =>
{
	var endpoint = Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT");
	var key = Environment.GetEnvironmentVariable("COSMOS_DB_KEY");
	var db = Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE");
	var container = Environment.GetEnvironmentVariable("COSMOS_DB_CONTAINER");

	return new CosmosDbService(endpoint, key, db, container);
});

builder.Services.AddLogging();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
