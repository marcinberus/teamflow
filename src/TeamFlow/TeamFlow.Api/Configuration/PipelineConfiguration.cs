using TeamFlow.Api.Configuration.Services;
using TeamFlow.Api.Middleware;

namespace TeamFlow.Api.Configuration;

public static class PipelineConfiguration
{
    public static WebApplication UsePipelineConfiguration(this WebApplication app)
    {
        app.UseExceptionHandling();

        app.UseSwaggerConfiguration(app.Environment);

        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
