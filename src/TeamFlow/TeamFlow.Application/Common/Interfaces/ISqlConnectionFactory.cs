using System.Data.Common;

namespace TeamFlow.Application.Common.Interfaces;

public interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
