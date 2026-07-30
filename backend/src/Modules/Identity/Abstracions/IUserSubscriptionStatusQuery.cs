namespace Modules.Identity.Abstracions;

public interface IUserSubscriptionStatusQuery
{
    Task<(string? Status, bool Exists)> GetSubscriptionStatusAsync(Guid userId);
}
