using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using VeterinaryApi.Infrastructure.Persistence;

namespace VeterinaryApi.Common.Extensions;

/// <summary>Application builder extension methods for EF Core database migration.</summary>
public static class MigrationExtension
{
    /// <summary>
    /// Applies any pending EF Core migrations to the database on application startup.
    /// Creates a scoped <see cref="ApplicationDbContext"/> to run <c>Database.Migrate()</c>.
    /// </summary>
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();


    }

}
