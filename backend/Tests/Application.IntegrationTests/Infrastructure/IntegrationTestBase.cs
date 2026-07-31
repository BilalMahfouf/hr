using Modules.Identity.Abstracions;
using Modules.Identity.Application.Users;
using Modules.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Modules.Shared.Abstracions;
using Modules.Shared.Infrastructure.Outbox;
using PublicApi.Common.Abstracions;
using Modules.Shared.Abstracions.Emails;
using PublicApi.Common.Abstracions.Payments;
using PublicApi.Features.Subscriptions.Endpoints;
using PublicApi.Infrastructure.Interceptors;
using PublicApi.Infrastructure.Persistence;
using PublicApi.Infrastructure.Services.Hashers;

namespace Application.IntegrationTests.Infrastructure;

[Collection("Postgres collection")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private string _databaseName = string.Empty;

    protected IntegrationTestBase(PostgresFixture fixture)
    {
        _fixture = fixture;
        CurrentUser = new TestCurrentUser();
        HttpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    protected TestCurrentUser CurrentUser { get; }
    protected HttpContextAccessor HttpContextAccessor { get; }
    protected TestEmailService EmailService { get; private set; } = new();
    protected IServiceProvider RootProvider { get; private set; } = null!;
    protected string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _databaseName = $"test_{Guid.NewGuid():N}";
        ConnectionString = await _fixture.CreateDatabaseAsync(_databaseName);
        RootProvider = BuildServiceProvider();

        await using var scope = RootProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (RootProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (RootProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (!string.IsNullOrWhiteSpace(_databaseName))
        {
            await _fixture.DropDatabaseAsync(_databaseName);
        }
    }

    protected IServiceScope CreateScope() => RootProvider.CreateScope();

    protected void ResetHttpContext()
    {
        HttpContextAccessor.HttpContext = new DefaultHttpContext();
    }

    protected void SetCurrentUser(Guid? userId)
    {
        CurrentUser.SetUserId(userId);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(CurrentUser);
        services.AddSingleton<ICurrentUser>(sp => sp.GetRequiredService<TestCurrentUser>());
        services.AddSingleton<IHttpContextAccessor>(HttpContextAccessor);

        services.AddScoped<InsertOutboxMessagesInterceptors>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>());
        });
        services.AddScoped<IIdentityApplicationDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IJwtProvider, TestJwtProvider>();

        EmailService = new TestEmailService();
        services.AddSingleton<IEmailService>(EmailService);
        services.AddSingleton<IUserSubscriptionStatusQuery, TestSubscriptionStatusQuery>();

        services.AddSingleton<FluentValidation.IValidator<ChangeEmail.ChangeEmailCommand>>(
            new ChangeEmail.Validator());
        services.AddSingleton<FluentValidation.IValidator<CreateSubscirption.Command>>(
            new CreateSubscirption.Validator());

        ConfigureServices(services);

        return services.BuildServiceProvider();
    }
}
