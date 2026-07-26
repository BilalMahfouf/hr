using VeterinaryApi.Common.Abstracions.Emails;
using VeterinaryApi.Common.Results;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestEmailService : IEmailService
{
    public List<SendEmailRequest> Sent { get; } = new();

    public Task<Result> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        Sent.Add(request);
        return Task.FromResult(Result.Success);
    }
}
