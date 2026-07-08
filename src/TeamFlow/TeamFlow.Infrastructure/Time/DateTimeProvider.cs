using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
