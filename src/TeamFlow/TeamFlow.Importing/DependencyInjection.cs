using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Importing.Projects;
using TeamFlow.Importing.Projects.Importerts;

namespace TeamFlow.Importing;

public static class DependencyInjection
{
    public static IServiceCollection AddImportingModule(this IServiceCollection services)
    {
        services.AddScoped<IProjectImporter, JsonImporter>();
        services.AddScoped<IProjectImporter, CsvImporter>();
        services.AddScoped<IProjectImportManager, ProjectImportManager>();

        return services;
    }
}
