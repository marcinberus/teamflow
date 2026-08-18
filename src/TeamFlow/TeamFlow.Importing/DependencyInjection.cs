using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Importing.Projects;
using TeamFlow.Importing.Projects.Models;
using TeamFlow.Importing.TaskItems;
using TeamFlow.Importing.TaskItems.Models;
using ProjectsCsvImporter = TeamFlow.Importing.Projects.Importerts.CsvImporter;
using ProjectsJsonImporter = TeamFlow.Importing.Projects.Importerts.JsonSourceGenerationImporter;
using TaskItemsCsvImporter = TeamFlow.Importing.TaskItems.Importers.CsvImporter;

namespace TeamFlow.Importing;

public static class DependencyInjection
{
    public static IServiceCollection AddImportingModule(this IServiceCollection services)
    {
        services.AddScoped<IProjectImporter, ProjectsJsonImporter>();
        services.AddScoped<IProjectImporter, ProjectsCsvImporter>();
        services.AddScoped<IImportManager<ProjectLine>, ProjectImportManager>();

        services.AddScoped<ITaskItemImporter, TaskItemsCsvImporter>();
        services.AddScoped<IImportManager<TaskItemLine>, TaskItemImportManager>();

        return services;
    }
}
