using Modules.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Modules.Identity.Abstracions;

public interface IIdentityApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserSession> UserSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
