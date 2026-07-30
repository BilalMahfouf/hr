using Modules.Shared.Results;

namespace Modules.Shared.CQRS;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public Task<Result<TResponse>> Handle(
        TQuery query,
        CancellationToken cancellationToken = default);
}
