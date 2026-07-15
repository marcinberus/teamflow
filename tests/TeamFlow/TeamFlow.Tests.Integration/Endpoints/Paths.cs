namespace TeamFlow.Tests.Integration.Endpoints;

internal class Paths
{
    public const string Users = "/api/v1/users";
    public const string Login = "/api/v1/login";
    public const string Projects = "/api/v1/projects";

    public static string ProjectMembers(Guid projectId) => $"{Projects}/{projectId}/members";

    public static string ProjectTasks(Guid projectId) => $"{Projects}/{projectId}/tasks";

    public static string ProjectMember(Guid projectId, Guid userId) =>
        $"{ProjectMembers(projectId)}/{userId}";
}
