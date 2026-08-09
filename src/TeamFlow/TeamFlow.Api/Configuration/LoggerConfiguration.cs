using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace TeamFlow.Api.Configuration;

public static class LoggerConfiguration
{
    public static void InitLogger()
    {
        Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }

    public static IServiceCollection AddSerilog(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSerilog((services, loggerConfiguration) =>
        {
            var environment = services.GetRequiredService<IWebHostEnvironment>();

            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();

            if (environment.IsDockerEnvironment())
            {
                loggerConfiguration.WriteTo.GrafanaLoki(
                    configuration["Grafana:LokiUrl"]
                        ?? throw new InvalidOperationException("Grafana:LokiUrl is not configured."),
                    labels:
                    [
                        new LokiLabel { Key = "service_name", Value = "teamflow-api" },
                        new LokiLabel { Key = "environment", Value = environment.EnvironmentName }
                    ],
                    propertiesAsStructuredMetadata: ["RequestId", "UserId"],
                    traceIdMode: LokiFieldDestination.StructuredMetadata,
                    spanIdMode: LokiFieldDestination.StructuredMetadata
                );
            }
        });

        return services;
    }

    public static WebApplication UseSerilogRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = (httpContext, elapsed, exception) =>
            {
                if (exception is not null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= 400)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

                var endpoint = httpContext.GetEndpoint();

                if (endpoint?.DisplayName is not null)
                {
                    diagnosticContext.Set("EndpointName", endpoint.DisplayName);
                }

                var userId = httpContext.User.FindFirst("sub")?.Value;

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    diagnosticContext.Set("UserId", userId);
                }
            };
        });

        return app;
    }
}
