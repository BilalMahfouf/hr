namespace VeterinaryApi.Features.Users;

public static class Shared
{
    public sealed record Response(
            Guid Id,
            string UserName,
            string FullName,
            string Email,
            string Role,
            bool IsActive,
            string? SubscriptionStatus,
            bool? IsSubscriptionExist,
            DateTime CreatedOnUtc
            );

}
