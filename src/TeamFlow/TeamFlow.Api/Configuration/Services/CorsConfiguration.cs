using Microsoft.Net.Http.Headers;

namespace TeamFlow.Api.Configuration.Services;

public static class CorsConfiguration
{
    private static readonly string[] AllowedMethods =
    [
        HttpMethods.Get,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
        HttpMethods.Options
    ];

    private static readonly string[] AllowedHeaders =
    [
        HeaderNames.Accept,
        HeaderNames.Authorization,
        HeaderNames.ContentType,
        HeaderNames.Origin
    ];

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowCredentials()
                    .WithMethods(AllowedMethods)
                    .WithHeaders(AllowedHeaders));
        });

        return services;
    }
}
