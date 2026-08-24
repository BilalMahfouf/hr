using Testcontainers.MsSql;

namespace Application.IntegrationTests.Infrastructure;

public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public MsSqlFixture()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("yourStrong(!)Password")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
