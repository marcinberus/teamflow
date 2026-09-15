using MediatR;

namespace TeamFlow.Application.Users.Queries.ListUsers;

public record ListUsersQuery(string? Search = null, int Page = 1, int PageSize = 20)
    : IRequest<ListUsersResult>;
