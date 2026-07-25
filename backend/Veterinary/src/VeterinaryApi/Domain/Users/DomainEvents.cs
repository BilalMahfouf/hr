using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Domain.Users;


public sealed record UserForgetPasswordDomainEvent(
    Guid UserId, string Email, string ClientUri,string Token) : DomainEvent;