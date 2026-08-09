namespace TeamFlow.Api.Configuration;

public static class Environments
{
    public const string Docker = "Docker";

    public static bool IsDockerEnvironment(this IWebHostEnvironment env)
    {
        return env.IsEnvironment(Docker);
    }
}
