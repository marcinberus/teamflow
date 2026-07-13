using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;

namespace TeamFlow.Application.Projects.Queries.GetProject;

public record GetProjectQuery(Guid ProjectId) : IRequest<Result<ProjectDetailsDto>>;
