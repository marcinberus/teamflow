using Microsoft.AspNetCore.RateLimiting;

namespace TeamFlow.Api.Configuration.Services;

public static class RateLimiterConfiguration
{
    public static IServiceCollection AddRateLimiterConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowMinutes = configuration.GetValue("RateLimiting:WindowMinutes", 1);

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("FixedWindowRateLimiter", limiterOptions =>
            {
                limiterOptions.PermitLimit = permitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(windowMinutes);
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
