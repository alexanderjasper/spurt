using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Spurt.Data;
using Spurt.Domain.Games;

namespace Spurt.Tests.Integration;

/// <summary>
///     Factory for creating isolated test environments
/// </summary>
public class TestDbContextFixture : IDisposable
{
    private readonly SqliteConnection _masterConnection;
    public AppDbContext DbContext { get; }

    public readonly IServiceProvider ServiceProvider;

    public TestDbContextFixture()
    {
        // Create a master connection that stays open during the fixture lifetime
        // to keep the in-memory database alive
        _masterConnection = new SqliteConnection("DataSource=:memory:");
        _masterConnection.Open();

        // Configure DbContext with SQLite
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_masterConnection)
            .EnableSensitiveDataLogging()
            .Options;

        // Create and set up the context
        DbContext = new AppDbContext(options);
        DbContext.Database.EnsureCreated();

        // Create a service collection and register services
        var services = new ServiceCollection();

        // Register DbContext factory to create fresh instances for each test
        services.AddSingleton(options);
        services.AddScoped<AppDbContext>();

        // Register data layer commands and queries
        services.Scan(scan => scan
            .FromAssemblyOf<AppDbContext>()
            .AddClasses(classes => classes.Where(type =>
                    type.Namespace != null &&
                    (
                        type.Namespace.Contains("Commands") ||
                        type.Namespace.Contains("Queries")
                    )
                )
            )
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        // Register a mock notification service
        services.AddSingleton<IGameHubNotificationService>(sp =>
            Substitute.For<IGameHubNotificationService>());

        // Register domain services
        services.Scan(scan => scan
            .FromAssemblyOf<Game>()
            .AddClasses(classes => classes.Where(type =>
                    type.Namespace != null &&
                    (
                        type.Namespace.Contains("Commands") ||
                        type.Namespace.Contains("Queries")
                    )
                )
            )
            .AsSelf()
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );

        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    ///     Creates a fresh DbContext instance with a clean tracking context
    /// </summary>
    public AppDbContext CreateNewDbContext()
    {
        return new AppDbContext(ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
    }

    /// <summary>
    ///     Creates a completely new test environment with its own isolated database
    /// </summary>
    public TestEnvironment CreateTestEnvironment()
    {
        return new TestEnvironment(ServiceProvider);
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _masterConnection.Close();
        _masterConnection.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Represents an isolated test environment with its own database
    /// </summary>
    public class TestEnvironment : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IServiceScope _scope;

        public IServiceProvider ServiceProvider { get; }
        public AppDbContext DbContext { get; }

        public TestEnvironment(IServiceProvider rootServiceProvider)
        {
            // Create a new connection for this test
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Create a new service collection and register services for this test
            var services = new ServiceCollection();

            // Register DbContext with our new connection
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .EnableSensitiveDataLogging()
                .Options;

            services.AddSingleton(options);
            services.AddScoped<AppDbContext>();

            // Register data layer commands and queries
            services.Scan(scan => scan
                .FromAssemblyOf<AppDbContext>()
                .AddClasses(classes => classes.Where(type =>
                        type.Namespace != null &&
                        (
                            type.Namespace.Contains("Commands") ||
                            type.Namespace.Contains("Queries")
                        )
                    )
                )
                .AsImplementedInterfaces()
                .WithTransientLifetime()
            );

            // Register a mock notification service
            services.AddSingleton<IGameHubNotificationService>(sp =>
                Substitute.For<IGameHubNotificationService>());

            // Register domain services
            services.Scan(scan => scan
                .FromAssemblyOf<Game>()
                .AddClasses(classes => classes.Where(type =>
                        type.Namespace != null &&
                        (
                            type.Namespace.Contains("Commands") ||
                            type.Namespace.Contains("Queries")
                        )
                    )
                )
                .AsSelf()
                .AsImplementedInterfaces()
                .WithTransientLifetime()
            );

            // Build the service provider
            _scope = services.BuildServiceProvider().CreateScope();
            ServiceProvider = _scope.ServiceProvider;

            // Create a new DbContext and ensure the database is created
            DbContext = new AppDbContext(options);
            DbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _scope.Dispose();
            _connection.Close();
            _connection.Dispose();
        }
    }
}