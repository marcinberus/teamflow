using System.Text.Json;
using Asp.Versioning;
using TeamFlow.Api.Configuration.Services;
using TeamFlow.Application;
using TeamFlow.Infrastructure;

namespace TeamFlow.Api.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddServiceConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);

        services.AddHttpContextAccessor();

        services.AddControllers();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddSwaggerConfiguration();

        services.AddAuthenticationConfiguration(configuration);
        services.AddAuthorization();

        services.AddRateLimiterConfiguration(configuration);

        services.AddCorsConfiguration(configuration);

        return services;
    }
}
