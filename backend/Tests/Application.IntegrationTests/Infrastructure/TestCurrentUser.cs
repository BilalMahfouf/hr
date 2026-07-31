using Modules.Shared.Abstracions;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; private set; }

    public void SetUserId(Guid? userId)
    {
        UserId = userId;
    }
}
