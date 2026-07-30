using Modules.Shared.Errors;

namespace Modules.Shared.Domain.Common;

public class DomainException : Exception
{
    public DomainException()
    {
    }

    public Error Error { get; set; } = null!;

    public DomainException(Error error) : base(error.Description)
    {
        Error = error;
    }
}
