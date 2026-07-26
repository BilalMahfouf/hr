namespace VeterinaryApi.Common.CQRS;

public interface IBaseCommand;

public interface ICommand : IBaseCommand;

public interface ICommand<out TResponse> : IBaseCommand;
