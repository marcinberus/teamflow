using Serilog;
using TeamFlow.Api.Configuration;

TeamFlow.Api.Configuration.LoggerConfiguration.InitLogger();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddServiceConfiguration(builder.Configuration);

    var app = builder.Build();

    app.UsePipelineConfiguration();

    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}


public partial class Program { }

