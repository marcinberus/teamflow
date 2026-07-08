namespace TeamFlow.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
