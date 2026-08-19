using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Identity.Infrastructure.Persistence;
using Modules.Shared.Infrastructure.Persistence;
using PublicApi.Infrastructure.Persistence;

namespace PublicApi.Common.Extensions;

public static class MigrationExtension
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var sharedDb = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
        sharedDb.Database.Migrate();

        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        identityDb.Database.Migrate();

        var attendanceDb = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
        attendanceDb.Database.Migrate();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();
    }
}
