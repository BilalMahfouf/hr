using VeterinaryApi.Common.Results;

namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Defines the contract for handling a query that returns a typed result.
/// Implementations contain read-only data retrieval logic and must not modify state.
/// Query handlers are auto-registered as scoped services via Scrutor in <c>Program.cs</c>.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle, must implement <see cref="IQuery{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of data returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Executes the specified query and returns the result.
    /// Implementations should use <c>.AsNoTracking()</c> on EF Core queries for performance.
    /// </summary>
    /// <param name="query">The query parameters and filters.</param>
    /// <param name="cancellationToken">Token to observe for async cancellation.</param>
    /// <returns>
    /// A <see cref="Result{TResponse}"/> containing the queried data on success,
    /// or a <see cref="VeterinaryApi.Common.Errors.Error"/> (e.g., <c>NotFound</c>) on failure.
    /// </returns>
    public Task<Result<TResponse>> Handle(
        TQuery query,
        CancellationToken cancellationToken = default);
}
