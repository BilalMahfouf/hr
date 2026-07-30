using Modules.Shared.Domain.Common;

namespace Modules.Identity.Domain.Users;

public sealed record UserForgetPasswordDomainEvent(
    Guid UserId, string Email, string ClientUri,string Token) : DomainEvent;
