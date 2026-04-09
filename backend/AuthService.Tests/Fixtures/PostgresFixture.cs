using Testcontainers.PostgreSql;

namespace AuthService.Tests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Db { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Db = new PostgreSqlBuilder("postgres:15")
            .WithDatabase("authdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await Db.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
    }
}
