using Carter;

namespace VeterinaryApi.Common.Endpoints
{
    /// <summary>
    /// Marker interface for Carter endpoint registration modules.
    /// All endpoint classes that implement route registration must implement this interface,
    /// which extends <see cref="ICarterModule"/> for automatic discovery by the Carter framework.
    /// </summary>
    public interface IEndpoint : ICarterModule
    {
    }
}
