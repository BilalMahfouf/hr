namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Marker interface for all commands in the CQRS pattern.
/// A command represents an intent to change the state of the system.
/// Implement <see cref="ICommand"/> for commands that produce no response value,
/// or <see cref="ICommand{TResponse}"/> for commands that return a result.
/// </summary>
public interface IBaseCommand;

/// <summary>
/// Marker interface for commands that do not produce a typed response value.
/// The corresponding handler returns a <c>Result</c> (success/failure) without a payload.
/// </summary>
public interface ICommand : IBaseCommand;

/// <summary>
/// Marker interface for commands that produce a typed response upon successful execution.
/// </summary>
/// <typeparam name="TResponse">The type of the value returned on success.</typeparam>
public interface ICommand<out TResponse> : IBaseCommand;
