using Application.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;

namespace Application.IntegrationTests.Users;

public sealed class UserForgetPasswordDomainEventHandlerTests
{
    [Fact]
    public async Task Handle_SendsResetEmail()
    {
        var emailService = new TestEmailService();
        var handler = new UserForgetPasswordDomainEventHandler(
            emailService,
            NullLogger<UserForgetPasswordDomainEventHandler>.Instance);

        var userId = Guid.NewGuid();
        var email = "doctor@test.local";
        var token = "token-123";
        var clientUri = "https://client.test/reset";
        var @event = new UserForgetPasswordDomainEvent(userId, email, clientUri, token);

        await handler.Handle(@event, CancellationToken.None);

        Assert.Single(emailService.Sent);
        var request = emailService.Sent[0];
        Assert.Equal(email, request.To);
        Assert.Equal("Reset Password", request.Subject);

        var expectedLink = $"{clientUri}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        Assert.Contains(expectedLink, request.Body);
    }
}
