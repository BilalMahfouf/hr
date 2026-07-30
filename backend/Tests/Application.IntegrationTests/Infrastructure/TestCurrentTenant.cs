using Shared.Abstracions;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestCurrentTenant : ICurrentTenant
{
    public Guid? UserId { get; private set; }

    public void SetUserId(Guid? userId)
    {
        UserId = userId;
    }
}
