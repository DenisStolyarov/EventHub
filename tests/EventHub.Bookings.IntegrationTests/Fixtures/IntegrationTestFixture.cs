using EventHub.Bookings.Application.Abstractions.Persistence;
using EventHub.Bookings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EventHub.Bookings.IntegrationTests.Fixtures;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private ServiceProvider _provider = null!;

    public T Create<T>() where T : notnull => _provider.GetRequiredService<T>();

    public BookingsDbContext CreateContext() => Create<BookingsDbContext>();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _provider = BuildServiceProvider();

        await ApplyMigrationsAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _container.DisposeAsync();
    }

    public async Task ResetDataAsync()
    {
        await using NpgsqlConnection connection = new(_container.GetConnectionString());

        await connection.OpenAsync();

        await using NpgsqlCommand cmd = new(
            """
                DO $$
                DECLARE
                    tables text;
                BEGIN
                    SELECT string_agg(quote_ident(schemaname) || '.' || quote_ident(tablename), ', ')
                    INTO tables
                    FROM pg_tables
                    WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory';

                    IF tables IS NOT NULL THEN
                        EXECUTE 'TRUNCATE ' || tables || ' RESTART IDENTITY CASCADE';
                    END IF;
                END $$;
            """,
            connection);

        await cmd.ExecuteNonQueryAsync();
    }

    private ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();

        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(_container.GetConnectionString()),
            ServiceLifetime.Transient);

        services.AddTransient<IUnitOfWork, UnitOfWork>();

        return services.BuildServiceProvider();
    }

    private async Task ApplyMigrationsAsync()
    {
        await using BookingsDbContext ctx = CreateContext();

        await ctx.Database.MigrateAsync();
    }
}
