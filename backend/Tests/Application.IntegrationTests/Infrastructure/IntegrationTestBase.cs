using Identity.Abstracions;
using Identity.Application.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using Shared.Abstracions.Emails;
using VeterinaryApi.Common.Abstracions.Payments;
using VeterinaryApi.Features.Subscriptions.Endpoints;
using VeterinaryApi.Infrastructure.Interceptors;
using VeterinaryApi.Infrastructure.OutboxMessages;
using VeterinaryApi.Infrastructure.Persistence;
using VeterinaryApi.Infrastructure.Services.Hashers;
using VeterinaryApi.Infrastructure.Tenants;

namespace Application.IntegrationTests.Infrastructure;

[Collection("Postgres collection")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private string _databaseName = string.Empty;

    protected IntegrationTestBase(PostgresFixture fixture)
    {
        _fixture = fixture;
        CurrentTenant = new TestCurrentTenant();
        HttpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    protected TestCurrentTenant CurrentTenant { get; }
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

    protected void SetCurrentTenant(Guid? userId)
    {
        CurrentTenant.SetUserId(userId);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(CurrentTenant);
        services.AddSingleton<ICurrentTenant>(sp => sp.GetRequiredService<TestCurrentTenant>());
        services.AddSingleton<IHttpContextAccessor>(HttpContextAccessor);

        services.AddScoped<InsertOutboxMessagesInterceptors>();
        services.AddScoped<TenantInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString)
                .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptors>())
                .AddInterceptors(sp.GetRequiredService<TenantInterceptor>());
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIdentityApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

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
