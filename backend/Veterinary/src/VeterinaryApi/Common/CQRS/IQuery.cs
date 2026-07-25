namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Marker interface for all queries in the CQRS pattern.
/// A query represents a read operation that retrieves data without modifying system state.
/// Queries must never produce side effects.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by this query.</typeparam>
public interface IQuery<out TResponse>;
