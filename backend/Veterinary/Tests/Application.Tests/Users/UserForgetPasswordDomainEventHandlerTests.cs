using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeterinaryApi.Common.Abstracions.Emails;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using Xunit;

namespace Application.Tests.Users;

public class UserForgetPasswordDomainEventHandlerTests
{
    private readonly Mock<IEmailService> _mockEmailService;

    public UserForgetPasswordDomainEventHandlerTests()
    {
        _mockEmailService = new Mock<IEmailService>();
    }

    private static string BuildExpectedLink(string email, string token, string clientUri)
    {
        return $"{clientUri}?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
    }

    [Fact]
    public async Task Handle_WhenEmailServiceSucceeds_ShouldSendResetEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var token = "reset-token";
        var clientUri = "https://example.com/reset";

        var domainEvent = new UserForgetPasswordDomainEvent(userId, email, clientUri, token);

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var handler = new UserForgetPasswordDomainEventHandler(
            _mockEmailService.Object,
            NullLogger<UserForgetPasswordDomainEventHandler>.Instance);

        var expectedLink = BuildExpectedLink(email, token, clientUri);

        // Act
        await handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _mockEmailService.Verify(es => es.SendEmailAsync(
            It.Is<SendEmailRequest>(r =>
                r.To == email &&
                r.Subject == "Reset Password" &&
                r.Body.Contains(expectedLink)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailServiceThrows_ShouldNotThrow()
    {
        // Arrange
        var domainEvent = new UserForgetPasswordDomainEvent(
            Guid.NewGuid(),
            "user@example.com",
            "https://example.com/reset",
            "reset-token");

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("mail failed"));

        var handler = new UserForgetPasswordDomainEventHandler(
            _mockEmailService.Object,
            NullLogger<UserForgetPasswordDomainEventHandler>.Instance);

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(domainEvent, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}
