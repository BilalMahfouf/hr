using VeterinaryApi.Common.Results;

namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Defines the contract for handling a command that does not return a typed response value.
/// Implementations contain the business logic for processing the command and
/// are auto-registered as scoped services via Scrutor assembly scanning in <c>Program.cs</c>.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle, must implement <see cref="ICommand"/>.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Processes the specified command and returns a success/failure result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to observe for async cancellation.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating whether the command executed successfully.
    /// On failure, the result contains an <see cref="VeterinaryApi.Common.Errors.Error"/> describing the problem.
    /// </returns>
    public Task<Result> Handle(
        TCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for handling a command that returns a typed response on success.
/// Implementations are auto-registered as scoped services via Scrutor in <c>Program.cs</c>.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle, must implement <see cref="ICommand{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the value returned when the command succeeds.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Processes the specified command and returns a result containing the response value on success.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to observe for async cancellation.</param>
    /// <returns>
    /// A <see cref="Result{TResponse}"/> containing the response value on success,
    /// or an error description on failure.
    /// </returns>
    public Task<Result<TResponse>> Handle(
        TCommand command,
        CancellationToken cancellationToken = default);
}
