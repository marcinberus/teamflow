using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Projects.DTOs;

namespace TeamFlow.Application.Projects.Queries.GetProjectStatistics;

public record GetProjectStatisticsQuery(Guid ProjectId) : IRequest<Result<ProjectStatisticsDto>>;
