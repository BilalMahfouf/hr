using Xunit;

namespace Application.IntegrationTests.Infrastructure;

[CollectionDefinition("Postgres collection")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
